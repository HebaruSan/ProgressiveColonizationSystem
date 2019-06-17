﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FinePrint;
using UnityEngine;

namespace ProgressiveColonizationSystem
{
    /// <summary>
    ///   A module for finding Crush-Ins on the planet
    /// </summary>
    /// <remarks>
    ///   Right now this is stuck onto the scanner, and yet it's not completely glued to the
    ///   scanner's resource-converter mechanism.  The intended effect is that the Tier1 scanner
    ///   finds Tier1 resources for your Tier1 base.  (And ensures that you've actually got the
    ///   right tier scanner).  If the idea of the planet-wide research takes a footing, these
    ///   modules could be stuck onto base control parts.
    /// </remarks>
    public class PksScanner
        : PartModule
    {
        /// <summary>
        ///  The number below which it's safe to say the user doesn't know how to create a scanner network.
        /// </summary>
        public const double BadScannerNetQualityThreshold = 1.0;

        /// <summary>
        ///   The minimum tier where grabbing crushins is required.  See also <see cref="PksTieredResourceConverter.inputRequirementStartingTier"/>
        ///   which is set for drills
        /// </summary>
        [KSPField]
        int minimumTier = 2;

        [KSPEvent(guiActive = true, guiName = "Find Loose Crush-Ins")]
        public void FindResource()
        {
            if (this.vessel.situation != Vessel.Situations.ORBITING)
            {
                ScreenMessages.PostScreenMessage("The vessel is not in a stable orbit");
                return;
            }

            GetTargetInfo(out TechTier tier, out string targetBody);
            if (this.vessel.mainBody.name != targetBody)
            {
                ScreenMessages.PostScreenMessage($"The vessel is not in a stable orbit around {targetBody}");
                return;
            }

            if ((int)tier < this.minimumTier)
            {
                ScreenMessages.PostScreenMessage($"Crushins are not required for production at {tier.DisplayName()}.");
                return;
            }

            var crewRequirement = this.part.FindModuleImplementing<PksCrewRequirement>();
            if (crewRequirement != null && !crewRequirement.IsRunning)
            {
                ScreenMessages.PostScreenMessage($"Finding resources with this scanner is proving really difficult.  Maybe we should turn it on?");
                return;
            }

            if (crewRequirement != null && !crewRequirement.IsStaffed)
            {
                ScreenMessages.PostScreenMessage($"This part requires a qualified Kerbal to run it.");
                return;
            }

            AskUserToPickbase(tier);
        }

        private void GetTargetInfo(out TechTier tier, out string targetBody)
        {
            var scanner = this.part.GetComponent<PksTieredResourceConverter>();
            Debug.Assert(scanner != null, "GetTargetInfo couldn't find a PksTieredResourceConverter - probably the part is misconfigured");
            if (scanner == null)
            {
                throw new Exception($"Misconfigured part - {part.name} should have a PksTieredResourceConverter");
            }

            tier = scanner.Tier;
            targetBody = scanner.Body;
        }

        private void AskUserToPickbase(TechTier scannerTier)
        {
            var stuffResource = ColonizationResearchScenario.Instance.AllResourcesTypes.FirstOrDefault(r => r.MadeFrom((TechTier)this.minimumTier) == ColonizationResearchScenario.Instance.CrushInsResource);

            var baseChoices = new List<DialogGUIButton>();
            double scannerNetQuality = this.ScannerNetQuality();
            Action onlyPossibleChoiceAction = null;
            foreach (var candidate in FlightGlobals.Vessels)
            {
                // Only look at vessels that are on the body of the scanner and are not marked as debris
                if (candidate.mainBody != this.vessel.mainBody
                 || (candidate.situation != Vessel.Situations.LANDED && candidate.situation != Vessel.Situations.SPLASHED)
                 || candidate.vesselType == VesselType.Debris)
                {
                    continue;
                }

                // Look for a scrounger part
                if (TryFindScrounger(scannerTier, candidate, stuffResource, out TechTier maxTierScrounger))
                {
                    void onSelect() => ResourceLodeScenario.Instance.GetOrCreateResourceLoad(this.vessel, candidate, maxTierScrounger, scannerNetQuality);
                    onlyPossibleChoiceAction = onSelect;

                    DialogGUIButton choice = new DialogGUIButton(candidate.vesselName, onSelect, dismissOnSelect: true);
                    baseChoices.Add(choice);
                }
            }

            if (baseChoices.Count == 0)
            {
                ScreenMessages.PostScreenMessage("There doesn't seem to be a base down there");
            }
            else if (baseChoices.Count == 1)
            {
                onlyPossibleChoiceAction();
            }
            else
            {
                PopupDialog.SpawnPopupDialog(
                    new MultiOptionDialog(
                        "Whatsthisdoeven",
                        "", // This actually shows up on the screen as a sort of a wierd-looking subtitle
                        "Which base shall we Look near?",
                        HighLogic.UISkin,
                        new DialogGUIVerticalLayout(baseChoices.ToArray())),
                    persistAcrossScenes: false,
                    skin: HighLogic.UISkin,
                    isModal: true,
                    titleExtra: "TITLE EXTRA!");
            }
        }

        private static bool TryFindScrounger(TechTier scannerTier, Vessel candidate, TieredResource stuffResource, out TechTier highestScroungerTier)
        {
            highestScroungerTier = TechTier.Tier0;

            // Don't trust KSP to actually always have these things populated;
            if (candidate.protoVessel == null || candidate.protoVessel.protoPartSnapshots == null)
            {
                return false;
            }

            bool hasScrounger = false;
            foreach (var protoPart in candidate.protoVessel.protoPartSnapshots)
            {
                if (protoPart.modules == null)
                {
                    continue;
                }

                // Unloaded vessels have modules, but they're not truly populated - that is, the class for
                // the module is created, but the members have their default values...  So we can find the
                // stuff that's coded in the part description (in this case) the "output" field here:
                if (protoPart.partPrefab.Modules.OfType<PksTieredResourceConverter>().Any(c => c.Output == stuffResource))
                {
                    hasScrounger = true;

                    // ...But to get hold of the this that are set in the VAB (like the tier), you have
                    // to parse it out of the config nodes.
                    foreach (var protoModule in protoPart.modules.Where(m => m.moduleName == nameof(PksTieredResourceConverter)))
                    {
                        TechTier tier = PksTieredResourceConverter.GetTechTierFromConfig(protoModule.moduleValues);
                        if (tier > scannerTier)
                        {
                            // There's no practical use of finding stuff near this base - even if it has lower
                            // tier scroungers somewhere, the higher tier ones should be taking priority.
                            return false;
                        }
                        else if (tier > highestScroungerTier)
                        {
                            highestScroungerTier = tier;
                        }
                    }
                }
            }

            return hasScrounger && stuffResource.MadeFrom(highestScroungerTier) != null;
        }

        private double ScannerNetQuality()
        {
            // you might think that situation==ORBITING would be the way to go, but sometimes vessels that
            // are clearly well in orbit are marked as FLYING.
            var scansats = FlightGlobals.Vessels
                .Where(v => v.mainBody == this.vessel.mainBody
                    && v.GetCrewCapacity() == 0 
                    && (v.situation == Vessel.Situations.ORBITING || v.situation == Vessel.Situations.FLYING));
            scansats = scansats.ToArray();
            return scansats.Sum(v => (v.orbit.inclination > 80.0 && v.orbit.inclination  < 100.0) ? 1 : .3);
        }

        public void FixedUpdate()
        {
            base.OnFixedUpdate();
            GetTargetInfo(out TechTier tier, out string targetBody);
            if (tier < (TechTier)minimumTier || this.vessel.mainBody.name != targetBody)
            {
                Events["FindResource"].guiActive = false;
            }
        }
    }
}
