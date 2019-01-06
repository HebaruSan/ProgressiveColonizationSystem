﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public abstract class ResearchCategory
    {
        private double kerbalYearsToTier1, kerbalYearsToTier2, kerbalYearsToTier3, kerbalYearsToTier4;

        protected ResearchCategory(double kerbalYearsToTier1, double kerbalYearsToTier2, double kerbalYearsToTier3, double kerbalYearsToTier4)
        {
            this.kerbalYearsToTier1 = kerbalYearsToTier1;
            this.kerbalYearsToTier2 = kerbalYearsToTier2;
            this.kerbalYearsToTier3 = kerbalYearsToTier3;
            this.kerbalYearsToTier4 = kerbalYearsToTier4;
        }

        public double KerbalYearsToNextTier(TechTier tier)
        {
            switch(tier)
            {
                case TechTier.Tier0: return this.kerbalYearsToTier1;
                case TechTier.Tier1: return this.kerbalYearsToTier2;
                case TechTier.Tier2: return this.kerbalYearsToTier3;
                case TechTier.Tier3: return this.kerbalYearsToTier4;
                case TechTier.Tier4: default: return double.MaxValue;
            }
        }

        public abstract bool CanDoResearch(Vessel vessel, TechTier currentTier, out string reasonWhyNot);

        public virtual string BreakthroughMessage(TechTier newTier)
        {
            // TODO: We can do a lot better than this.
            return $"{newTier.DisplayName()} has been unlocked";
        }

        public abstract string DisplayName { get; }

        #region Common implementations of CanDoResearch
        protected bool MaxLimitedOnEasyWorlds(Vessel vessel, TechTier currentTier, out string reasonWhyNot)
        {
            switch (currentTier)
            {
                case TechTier.Tier0:
                case TechTier.Tier1:
                    break;
                case TechTier.Tier2:
                    if (IsVeryEasyWorld(vessel))
                    {
                        reasonWhyNot = $"{this.DisplayName} research is limited to {TechTier.Tier2.DisplayName()} on the moons of the home world";
                    }
                    break;
                case TechTier.Tier3:
                    if (!IsFarOut(vessel))
                    {
                        reasonWhyNot = $"{this.DisplayName} research is limited to {TechTier.Tier3.DisplayName()} on easy worlds";
                    }
                    break;
                case TechTier.Tier4:
                default:
                    reasonWhyNot = MaxTierMessage;
                    return false;
            }
            reasonWhyNot = null;
            return true;
        }

        protected bool MaxLimitedNearKerbin(Vessel vessel, TechTier currentTier, out string reasonWhyNot)
        {
            switch(currentTier)
            {
                case TechTier.Tier0:
                case TechTier.Tier1:
                    break;
                case TechTier.Tier2:
                    if (IsNearKerbin(vessel))
                    {
                        reasonWhyNot = $"{this.DisplayName} progression is limited to {TechTier.Tier2.DisplayName()} near {FlightGlobals.GetHomeBody().name}";
                    }
                    break;
                case TechTier.Tier3:
                    if (!IsFarOut(vessel))
                    {
                        reasonWhyNot = $"{this.DisplayName} progression is limited to {TechTier.Tier3.DisplayName()} anywhere remotely near {FlightGlobals.GetHomeBody().name}";
                    }
                    break;
                case TechTier.Tier4:
                default:
                    reasonWhyNot = MaxTierMessage;
                    return false;
            }
            reasonWhyNot = null;
            return true;
        }

        protected bool NoResearchLimits(Vessel vessel, TechTier currentTier, out string reasonWhyNot)
        {
            if (currentTier == TechTier.Tier4)
            {
                reasonWhyNot = MaxTierMessage;
                return false;
            }

            reasonWhyNot = null;
            return true;
        }

        protected string MaxTierMessage = $"All research is capped at {TechTier.Tier4.DisplayName()}";

        protected static bool IsNearKerbin(Vessel vessel)
        {
            // There are more stylish ways to do this.  It's also a bit problematic for the player
            // because if they ignore a craft on its way back from some faroff world until it
            // reaches kerbin's SOI, then they'll lose all that tasty research.
            //
            // A fix would be to look at the vessel's orbit as well, and, if it just carries the
            // vessel out of the SOI, count that.
            double homeworldDistanceFromSun = FlightGlobals.GetHomeBody().orbit.altitude;
            return vessel.distanceToSun > homeworldDistanceFromSun * .9
                && vessel.distanceToSun < homeworldDistanceFromSun * 1.1;
        }

        protected static bool IsFarOut(Vessel vessel)
        {
            double homeworldDistanceFromSun = FlightGlobals.GetHomeBody().orbit.altitude;
            return vessel.distanceToSun > homeworldDistanceFromSun * .9
                && vessel.distanceToSun < homeworldDistanceFromSun * 1.1;
        }

        protected static bool IsVeryEasyWorld(Vessel vessel)
        {
            return vessel.mainBody.name == "Mun" || vessel.mainBody.name == "Minmus";
        }

        protected static bool IsHardWorld(Vessel vessel)
        {
            return vessel.mainBody.name == "Dres" || vessel.mainBody.name == "Moho" || vessel.mainBody.name == "Eve" || vessel.mainBody.name == "Eloo";
        }
        #endregion
    }

    public class HydroponicResearchCategory
        : ResearchCategory
    {
        public HydroponicResearchCategory()
            : base(5.0, 12.0, 7.0, 12.0) 
        { // For hydroponics, getting out there is the hard part, so the requirements diminish at T4
        }

        public override string DisplayName => "Hydroponic";

        public override bool CanDoResearch(Vessel vessel, TechTier currentTier, out string reasonWhyNot)
            => MaxLimitedNearKerbin(vessel, currentTier, out reasonWhyNot);
    }

    public class FarmingResearchCategory
        : ResearchCategory
    {
        public FarmingResearchCategory()
            : base(2.0, 4.0, 10.0, 18.0)
        {
        }

        public override string DisplayName => "Farming";

        public override bool CanDoResearch(Vessel vessel, TechTier currentTier, out string reasonWhyNot)
            => NoResearchLimits(vessel, currentTier, out reasonWhyNot);
    }

    public class ProductionResearchCategory
        : ResearchCategory
    {
        public ProductionResearchCategory()
            : base(2.0, 6.0, 20.0, 40.0)
        {
        }

        public override string DisplayName => "Drilling and Fertilizer";

        public override bool CanDoResearch(Vessel vessel, TechTier currentTier, out string reasonWhyNot)
            => NoResearchLimits(vessel, currentTier, out reasonWhyNot);
    }

    public class ScanningResearchCategory
        : ResearchCategory
    {
        public ScanningResearchCategory()
            : base(.8, 1.6, 3.0, 3.0)
        {
        }

        public override string DisplayName => "Scanning";

        public override bool CanDoResearch(Vessel vessel, TechTier currentTier, out string reasonWhyNot)
            => NoResearchLimits(vessel, currentTier, out reasonWhyNot);
    }

    public class ShiniesResearchCategory
        : ResearchCategory
    {
        public ShiniesResearchCategory()
            : base(.8, 1.6, 3.0, 3.0)
        {
        }

        public override string DisplayName => "Blingology";

        public override bool CanDoResearch(Vessel vessel, TechTier currentTier, out string reasonWhyNot)
            => MaxLimitedOnEasyWorlds(vessel, currentTier, out reasonWhyNot);
    }
}
