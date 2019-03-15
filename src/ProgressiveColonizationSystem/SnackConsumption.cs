﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ProgressiveColonizationSystem
{
    public class SnackConsumption
        : VesselModule
    {
        public const double DrillCapacityMultiplierForAutomaticMiningQualification = 5.0;

        [KSPField(isPersistant = true)]
        public double LastUpdateTime;

        /// <summary>
        ///   This is the vessel ID of the rover or lander that is used to automatically supply
        ///   the base.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string supplierMinerCraftId = "";

        /// <summary>
        ///   The vessel ID of the rover that last pushed the minimum quantity of resources to the station.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string lastMinerToDepositCraftId = "";

        protected IResourceBroker _resBroker;
        public IResourceBroker ResBroker
        {
            get { return _resBroker ?? (_resBroker = new ResourceBroker()); }
        }

        protected ResourceConverter _resConverter;
        public ResourceConverter ResConverter
        {
            get { return _resConverter ?? (_resConverter = new ResourceConverter()); }
        }

        /// <summary>
        ///   This is called on each physics frame for the active vessel by reflection-magic from KSP.
        /// </summary>
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || vessel == null || !vessel.loaded || LifeSupportScenario.Instance == null)
            {
                return;
            }

            // Compute this early, since it sets the last update time member
            double deltaTime = GetDeltaTime();
            if (deltaTime < 0)
            {
                // This is the vessel launch - nothing to do but set the update time.
                return;
            }

            if (vessel.isEVA)
            {
                // TODO: What to do here?  Should Kerbals on EVA ever go hungry?
                return;
            }

            List<ProtoCrewMember> crew = vessel.GetVesselCrew();
            if (crew.Count == 0)
            {
                // Nobody on board
                return;
            }

            if (this.IsAtHome)
            {
                // While actually on Kerbal, the Kerbals will order take-out rather than consuming
                // what's in the ship.
                foreach (var crewman in crew)
                {
                    LifeSupportScenario.Instance?.KerbalHasReachedHomeworld(crewman);
                }
            }
            else
            {
                this.ProduceAndConsume(crew, deltaTime);
            }
        }

        private bool IsMiningLanderPresent
        {
            get
            {
                if (string.IsNullOrEmpty(this.supplierMinerCraftId))
                {
                    return false;
                }
                else
                {
                    Guid vesselId = new Guid(this.supplierMinerCraftId);
                    Vessel vessel = FlightGlobals.Vessels.FirstOrDefault(v => v.id == vesselId);
                    return vessel != null && vessel.loaded
                        && (vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.SPLASHED)
                        && this.vessel.GetVesselCrew().Any(c => c.trait == KerbalRoster.pilotTrait);
                }
            }
        }

        /// <summary>
        ///   This gets a string that summarizes the state of the miner for the <see cref="LifeSupportStatusMonitor"/>
        /// </summary>
        public string GetMinerStatusMessage()
        {
            if (string.IsNullOrEmpty(this.supplierMinerCraftId))
            {
                return null;
            }

            Guid vesselId = new Guid(this.supplierMinerCraftId);
            Vessel minerVessel = FlightGlobals.Vessels.FirstOrDefault(v => v.id == vesselId);
            if (minerVessel == null)
            {
                return null;
            }

            if (!minerVessel.loaded || (minerVessel.situation != Vessel.Situations.LANDED && minerVessel.situation != Vessel.Situations.SPLASHED))
            {
                return $"{minerVessel.vesselName} is set up to automatically fetch Crush-Ins for this base, but it's not present.";
            }

            if (!this.vessel.GetVesselCrew().Any(c => c.trait == KerbalRoster.pilotTrait))
            {
                return $"{minerVessel.vesselName} is set up to automatically fetch Crush-Ins for this base, but there's no pilot here to drive it.";
            }

            return $"{minerVessel.vesselName} is automatically fetching Crush-Ins for this base.";
        }

        internal void MiningMissionFinished(Vessel sourceVessel, double amountSent)
        {
            if (sourceVessel.GetCrewCapacity() < 2)
            {
                ScreenMessages.PostScreenMessage("This vessel doesn't qualify to be an automatic miner -- it doesn't have a crew capacity of 2 or more", 15.0f);
                return;
            }

            double totalCapacityAtBase = this.vessel.FindPartModulesImplementing<ITieredProducer>()
                .Where(p => p.Input == ColonizationResearchScenario.CrushInsResource)
                .Sum(p => p.ProductionRate);
            double minimumQualifyingAmount = totalCapacityAtBase * DrillCapacityMultiplierForAutomaticMiningQualification;
            if (amountSent < minimumQualifyingAmount)
            {
                ScreenMessages.PostScreenMessage($"This vessel doesn't qualify towards becoming an automatic miner -- less than {minimumQualifyingAmount} was transferred.", 15.0f);
                return;
            }

            string sourceVesselId = sourceVessel.id.ToString();
            if (this.lastMinerToDepositCraftId == sourceVesselId)
            {
                if (this.supplierMinerCraftId != sourceVesselId)
                {
                    PopupMessageWithKerbal.ShowPopup("We Got it From here!",
                        $"Looks like the {sourceVessel.vesselName} is a fine vessel for grabbing resources.  "
                        + "If you don't mind, leaving her parked here, Kerbals at the base will automatically "
                        + "drive it out and gather resources in the future.",
                        "Because you delivered two loads of Crush-Ins to your base with this craft, you qualify "
                        + "for automatic mining.  That means that if you leave your base alone, even for a long "
                        + "time, when you return they'll still be full of Crush-Ins.  This depends on you leaving "
                        + "the ship parked in physics-range of the base (2.2km) and the base having a Pilot on "
                        + "board to do the driving.",
                        "Thanks!");
                    this.supplierMinerCraftId = sourceVesselId;
                }
            }
            else
            {
                ScreenMessages.PostScreenMessage($"{this.vessel.vesselName} has given a signed bill of lading to {sourceVessel.vesselName} -- one more delivery and it'll be certified for automatic use!", 15.0f);
                this.lastMinerToDepositCraftId = sourceVesselId;
            }
        }

        /// <summary>
        ///   Calculates snacks consumption aboard the vessel.
        /// </summary>
        /// <param name="crew">The crew</param>
        /// <param name="deltaTime">The amount of time (in seconds) since the last calculation was done</param>
        /// <returns>The amount of <paramref name="deltaTime"/> in which food was supplied.</returns>
        private double ProduceAndConsume(List<ProtoCrewMember> crew, double deltaTime)
        {
            var snackProducers = this.vessel.FindPartModulesImplementing<ITieredProducer>();
			this.ResourceQuantities(out var availableResources, out var availableStorage);
            var crewPart = vessel.parts.FirstOrDefault(p => p.CrewCapacity > 0);
            double remainingTime = deltaTime;

            while (remainingTime > ResourceUtilities.FLOAT_TOLERANCE)
            {
                TieredProduction.CalculateResourceUtilization(
                    crew.Count,
                    deltaTime,
                    snackProducers,
                    ColonizationResearchScenario.Instance,
                    availableResources,
					availableStorage,
                    out double elapsedTime,
                    out List<TieredResource> breakthroughCategories,
                    out Dictionary<string,double> resourceConsumptionPerSecond,
                    out Dictionary<string,double> resourceProductionPerSecond);

                if (elapsedTime == 0)
                {
                    break;
                }

                if (resourceConsumptionPerSecond != null || resourceProductionPerSecond != null)
                {
                    ConversionRecipe consumptionRecipe = new ConversionRecipe();
                    if (resourceConsumptionPerSecond != null)
                    {
                        // ISSUE 2019/2: This isn't really ideal, since finding nearby lodes is not a cheap operation
                        //   and it gets done twice.  But perhaps it's all mute because the whole resource chain calculation
                        //   is expensive as well and perhaps there's a way to compute it less than once a frame.
                        if (ResourceLodeScenario.Instance.TryFindResourceLodeInRange(vessel, out var resourceLode)
                         && resourceConsumptionPerSecond.TryGetValue(ColonizationResearchScenario.LodeResource.TieredName(resourceLode.Tier), out double lodeConsumptionPerSecond))
                        {
                            ResourceLodeScenario.Instance.TryConsume(resourceLode, lodeConsumptionPerSecond * elapsedTime, out _);
                        }
                        else
                        {
                            consumptionRecipe.Inputs.AddRange(resourceConsumptionPerSecond
                                .Where(pair => !resourceIsAutosupplied(pair.Key))
                                .Select(pair => new ResourceRatio()
                                {
                                    ResourceName = pair.Key,
                                    Ratio = pair.Value,
                                    DumpExcess = false,
                                    FlowMode = ResourceFlowMode.ALL_VESSEL
                                }));
                        }
                    }
                    if (resourceProductionPerSecond != null)
                    {
                        consumptionRecipe.Outputs.AddRange(
                            resourceProductionPerSecond.Select(pair => new ResourceRatio()
                            {
                                ResourceName = pair.Key,
                                Ratio = pair.Value,
                                DumpExcess = true,
                                FlowMode = ResourceFlowMode.ALL_VESSEL
                            }));
                    }
                    Debug.Assert(elapsedTime > 0);
                    var consumptionResult = this.ResConverter.ProcessRecipe(elapsedTime, consumptionRecipe, crewPart, null, 1f);
                    Debug.Assert(Math.Abs(consumptionResult.TimeFactor - elapsedTime) < ResourceUtilities.FLOAT_TOLERANCE,
                        "ProgressiveColonizationSystem.SnackConsumption.CalculateSnackFlow is busted - it somehow got the consumption recipe wrong.");
                }

                foreach (TieredResource resource in breakthroughCategories)
                {
                    TechTier newTier = ColonizationResearchScenario.Instance.GetMaxUnlockedTier(resource, this.vessel.lastBody.name);
                    string title = $"{resource.ResearchCategory.DisplayName} has progressed to {newTier.DisplayName()}!";
                    string message = resource.ResearchCategory.BreakthroughMessage(newTier);
                    string boringMessage = resource.ResearchCategory.BoringBreakthroughMessage(newTier);
                    PopupMessageWithKerbal.ShowPopup(title, message, boringMessage, "That's Just Swell");
                }

                remainingTime -= elapsedTime;
            }

            if (remainingTime != deltaTime)
            {
                double lastMealTime = Planetarium.GetUniversalTime() - remainingTime;
                // Somebody got something to eat - record that.
                foreach (var crewMember in crew)
                {
                    LifeSupportScenario.Instance.KerbalHadASnack(crewMember, lastMealTime);
                }
            }

            if (remainingTime > ResourceUtilities.FLOAT_TOLERANCE)
            {
                // We ran out of food
                // TODO: Maybe we ought to have a single message for the whole crew?
                foreach (var crewMember in crew)
                {
                    LifeSupportScenario.Instance.KerbalMissedAMeal(crewMember);
                }
                return deltaTime - remainingTime;
            }
            else
            {
                return 0;
            }
        }

        private bool resourceIsAutosupplied(string tieredResourceName)
        {
            if (this.IsMiningLanderPresent)
            {
                ColonizationResearchScenario.Instance.TryParseTieredResourceName(tieredResourceName, out var resource, out _);
                return resource == ColonizationResearchScenario.CrushInsResource;
            }
            else
            {
                return false;
            }
        }

        internal void ResourceQuantities(out Dictionary<string, double> availableResources, out Dictionary<string, double> availableStorage)
            => ResourceQuantities(this.vessel, 100 * ResourceUtilities.FLOAT_TOLERANCE, out availableResources, out availableStorage);

        internal static void ResourceQuantities(Vessel vessel, double minimumAmount, out Dictionary<string, double> availableResources, out Dictionary<string, double> availableStorage)
        {
            availableResources = new Dictionary<string, double>();
            availableStorage = new Dictionary<string, double>();
            foreach (var part in vessel.parts)
            {
                foreach (var resource in part.Resources)
                {
                    if (resource.resourceName == "ElectricCharge")
                    {
                        continue;
                    }

                    // Be careful that we treat nearly-zero as zero, as otherwise we can get into an infinite
                    // loop when the resource calculator decides the amount is too minute to rate more than 0 time.
                    if (resource.flowState && resource.amount > minimumAmount)
                    {
                        availableResources.TryGetValue(resource.resourceName, out double amount);
                        availableResources[resource.resourceName] = amount + resource.amount;
                    }
                    if (resource.flowState && resource.maxAmount - resource.amount > minimumAmount)
                    {
                        availableStorage.TryGetValue(resource.resourceName, out double amount);
                        availableStorage[resource.resourceName] = amount + resource.maxAmount - resource.amount;
                    }
                }
            }

            // Add a magic container that has whatever stuff the planet has
            if (ResourceLodeScenario.Instance.TryFindResourceLodeInRange(vessel, out var resourceLode))
            {
                availableResources.Add(ColonizationResearchScenario.LodeResource.TieredName(resourceLode.Tier), resourceLode.Quantity);
            }
        }

        public bool IsAtHome => vessel.mainBody == FlightGlobals.GetHomeBody() && vessel.altitude < 10000;

        private double GetDeltaTime()
        {
            if (Time.timeSinceLevelLoad < 1.0f || !FlightGlobals.ready)
            {
                return -1;
            }

            if (Math.Abs(LastUpdateTime) < ResourceUtilities.FLOAT_TOLERANCE)
            {
                // Just started running
                LastUpdateTime = Planetarium.GetUniversalTime();
                return -1;
            }

            double maxDeltaTime = ResourceUtilities.GetMaxDeltaTime();
            double deltaTime = Math.Min(Planetarium.GetUniversalTime() - LastUpdateTime, maxDeltaTime);

            LastUpdateTime += deltaTime;
            return deltaTime;
        }
    }
}
