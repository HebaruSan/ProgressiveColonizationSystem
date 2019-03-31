﻿using KSP.UI.Screens;
using ProgressiveColonizationSystem.ProductionChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ProgressiveColonizationSystem
{
    /// <summary>
    ///   This class maintains a toolbar button and a GUI display that allows the user to see
    ///   into the life support status of the active vessel.
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT)]
    public class LifeSupportStatusMonitor
        : PksTabbedDialog
    {
        const string ProductionTab = "Production";
        const string SnacksTab = "Snacks";
        const string ProgressionTab = "Progression";
        const string TransferTab = "Transfer";
        const string CrewTab = "Crew";

        // If simulating + or - crewman, this becomes positive or negative.
        private int crewDelta = 0;
        // CrewDelta gets reset when lastActiveVessel no longer equals the current vessel.
        private Vessel lastActiveVessel;
        private string consumptionAndProductionInformation;
        private bool showingWhatIfButtons;
        private bool showingResourceTransfer;

        private IntervesselResourceTransfer resourceTransfer = new IntervesselResourceTransfer();

        public LifeSupportStatusMonitor()
            : base(new string[] { SnacksTab, ProductionTab, ProgressionTab, TransferTab, CrewTab })
        {
        }

        protected override DialogGUIBase DrawTab(string tab)
        {
            switch(tab)
            {
                default:
                case SnacksTab:
                    return DrawSnacksTab();
                case ProductionTab:
                    return DrawProductionTab();
                case ProgressionTab:
                    return DrawProgressionTab();
                case TransferTab:
                    return DrawTransferTab();
                case CrewTab:
                    return DrawCrewTab();
            }
        }

        protected override bool IsRelevant => FlightGlobals.ActiveVessel.GetCrewCount() > 0 && !FlightGlobals.ActiveVessel.isEVA;
        protected override ApplicationLauncher.AppScenes VisibleInScenes { get; } = ApplicationLauncher.AppScenes.FLIGHT;

        private DialogGUIBase DrawSnacksTab()
        {
            var body = new DialogGUILabel(() => this.consumptionAndProductionInformation);
            var whatif = new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                            new DialogGUILabel("What if we"),
                            new DialogGUIButton("Add", () => { ++crewDelta; }, () => true, false),
                            new DialogGUILabel("/"),
                            new DialogGUIButton("Remove", () => { --crewDelta; }, () => FlightGlobals.ActiveVessel.GetCrewCount() + this.crewDelta > 1, false),
                            new DialogGUILabel("a kerbal?"));
            return new DialogGUIVerticalLayout(body, whatif);
        }

        private DialogGUIBase DrawProductionTab()
        {
            return new DialogGUILabel(() => this.consumptionAndProductionInformation);
        }

        private DialogGUIBase DrawProgressionTab()
        {
            return new DialogGUILabel(() => this.consumptionAndProductionInformation);
        }

        private DialogGUIBase DrawTransferTab()
        {
            return new DialogGUIVerticalLayout(
                new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                    new DialogGUILabel("Target: "),
                    new DialogGUILabel(resourceTransfer.TargetVessel?.GetDisplayName())),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                    new DialogGUIButton("Start", resourceTransfer.StartTransfer, () => resourceTransfer.TargetVessel != null && !resourceTransfer.IsTransferUnderway, dismissOnSelect: false),
                    new DialogGUISlider(() => (float)resourceTransfer.TransferPercent, 0, 1, false, 100, 20, null)));
        }

        private DialogGUIBase DrawCrewTab()
        {
            return new DialogGUILabel("TODO");
        }

        protected override MultiOptionDialog DrawDialog(Rect rect)
        {
            // FYI, if you want to override a style, here'd be a way to do it:
            // var myStyle = new UIStyle(UISkinManager.defaultSkin.label) { wordWrap = false};
            //
            // Too bad wordWrap doesn't get paid attention to.
            return new MultiOptionDialog(
                        "LifeSupportMonitor",  // <- no idea what this does.
                        "",
                        "Colony Status",
                        HighLogic.UISkin,
                        rect,
                        DrawTabbedDialog());
        }

        protected override void OnFixedUpdate()
        {
            resourceTransfer.OnFixedUpdate();

            if (this.lastActiveVessel != FlightGlobals.ActiveVessel)
            {
                this.crewDelta = 0;
            }

            this.lastActiveVessel = FlightGlobals.ActiveVessel;
            var activeSnackConsumption = FlightGlobals.ActiveVessel?.GetComponent<SnackConsumption>();
            if (activeSnackConsumption == null)
            {
                // Shouldn't happen, but just in case...
                return;
            }

            int crewCount = FlightGlobals.ActiveVessel.GetCrewCount();
            if ((crewCount > 0) != this.showingWhatIfButtons)
            {
                this.showingWhatIfButtons = (crewCount > 0);
                this.Redraw();
            }

            if ((resourceTransfer.TargetVessel != null) != this.showingResourceTransfer)
            {
                this.showingResourceTransfer = (resourceTransfer.TargetVessel != null);
                this.Redraw();
            }

            activeSnackConsumption.ResourceQuantities(out var availableResources, out var availableStorage);
            List<ITieredProducer> tieredProducers = activeSnackConsumption.Vessel.FindPartModulesImplementing<ITieredProducer>();
            List<ITieredCombiner> tieredCombiners = activeSnackConsumption.Vessel.FindPartModulesImplementing<ITieredCombiner>();

            string minerStatusMessage = FlightGlobals.ActiveVessel.vesselModules
                .OfType<SnackConsumption>()
                .FirstOrDefault()
                ?.GetMinerStatusMessage();
            BuildStatusString(activeSnackConsumption, availableResources, availableStorage, tieredProducers, tieredCombiners, crewCount, crewDelta, out string message);
            this.consumptionAndProductionInformation = (minerStatusMessage == null ? "" : minerStatusMessage + "\r\n\r\n") + message;
        }

        internal static void BuildStatusString(
            SnackConsumption activeSnackConsumption,
            Dictionary<string, double> resources,
            Dictionary<string, double> storage,
            List<ITieredProducer> tieredProducers,
            List<ITieredCombiner> tieredCombiners,
            int crewCount,
            int crewDelta,
            out string message)
        {
            StringBuilder text = new StringBuilder();

            ResearchSink researchSink = new ResearchSink();
            TieredProduction.CalculateResourceUtilization(
                crewCount + crewDelta, 1, tieredProducers, tieredCombiners, researchSink, resources, storage,
                out double timePassed, out var _, out Dictionary<string, double> resourcesConsumed,
                out Dictionary<string, double> resourcesProduced);
            if (timePassed == 0)
            {
                text.AppendLine("There aren't enough supplies or producers here to feed any kerbals.");

                if (!activeSnackConsumption.IsAtHome)
                {
                    Dictionary<int, List<ProtoCrewMember>> buckets = new Dictionary<int, List<ProtoCrewMember>>();
                    foreach (var crew in activeSnackConsumption.Vessel.GetVesselCrew())
                    {
                        var kerbalIsKnown = LifeSupportScenario.Instance.TryGetStatus(crew, out double daysSinceMeal, out double daysToGrouchy, out bool isGrouchy);
                        if (!kerbalIsKnown)
                        {
                            // Maybe if ! on kerban we complain about this?
                            // Debug.LogError($"Couldn't find a life support record for {crew.name}");
                        }

                        int bucketKey = isGrouchy ? -1 : (int)daysToGrouchy;
                        if (!buckets.TryGetValue(bucketKey, out var crewInBucket))
                        {
                            crewInBucket = new List<ProtoCrewMember>();
                            buckets.Add(bucketKey, crewInBucket);
                        }
                        crewInBucket.Add(crew);
                    }

                    CrewBlurbs.random = new System.Random(FlightGlobals.ActiveVessel.GetHashCode());
                    foreach (List<ProtoCrewMember> crewInBucket in buckets.Values)
                    {
                        // yeah yeah, recomputing this is wasteful & all...
                        LifeSupportScenario.Instance.TryGetStatus(crewInBucket[0], out double daysSinceMeal, out double daysToGrouchy, out bool isGrouchy);
                        if (isGrouchy)
                        {
                            text.AppendLine(CrewBlurbs.StarvingKerbals(crewInBucket));
                        }
                        else if (daysToGrouchy < 2)
                        {
                            text.AppendLine(CrewBlurbs.GrumpyKerbals(crewInBucket, daysToGrouchy, tieredProducers.Any()));
                        }
                        else
                        {
                            text.AppendLine(CrewBlurbs.HungryKerbals(crewInBucket, daysToGrouchy, tieredProducers.Any()));
                        }
                    }
                }
            }
            else
            {
                if (crewDelta == 0)
                {
                    text.AppendLine($"To sustain its crew of {crewCount + crewDelta}, this vessel is using:");
                }
                else
                {
                    text.AppendLine($"To sustain a crew of {crewCount + crewDelta} this vessel would use:");
                }

                foreach (var resourceName in resourcesConsumed.Keys.OrderBy(n => n))
                {
                    double perDay = TieredProduction.UnitsPerSecondToUnitsPerDay(resourcesConsumed[resourceName]);
                    double daysLeft = resources[resourceName] / perDay;
                    text.AppendLine($"{perDay:N1} {resourceName} per day ({daysLeft:N1} days left)");
                }

                if (resourcesProduced != null && resourcesProduced.Count > 0)
                {
                    text.AppendLine();
                    text.AppendLine("The crew is also producing:");
                    foreach (var resourceName in resourcesProduced.Keys.OrderBy(n => n))
                    {
                        double perDay = TieredProduction.UnitsPerSecondToUnitsPerDay(resourcesProduced[resourceName]);
                        text.AppendLine($"{perDay:N1} {resourceName} per day");
                    }
                }

                bool addedResearchLineBreak = false;
                foreach (var pair in researchSink.Data)
                {
                    if (!addedResearchLineBreak)
                    {
                        text.AppendLine();
                        addedResearchLineBreak = true;
                    }

                    text.AppendLine($"This vessel {(crewDelta == 0 ? "is contributing" : "would contribute")} {pair.Value.KerbalDaysContributedPerDay:N1} units of {pair.Key.DisplayName} research per day.  ({pair.Value.KerbalDaysUntilNextTier:N} are needed to reach the next tier).");
                }
            }

            message = text.ToString();
        }
    }
}
