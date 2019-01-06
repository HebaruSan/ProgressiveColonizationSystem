﻿using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nerm.Colonization.UnitTests
{
    public class StubColonizationResearchScenario
        : IColonizationResearchScenario
    {
        public StubColonizationResearchScenario(TechTier currentTier)
        {
            this.AgroponicsMaxTier = currentTier;
        }

        public double AgroponicResearchProgress { get; set; }

		public double AgricultureResearchProgress { get; set; }

		public double ProductionResearchProgress { get; set; }

		public TechTier AgroponicsMaxTier { get; private set; }


        internal void Reset()
        {
            AgricultureResearchProgress = 0;
            AgroponicResearchProgress = 0;
            ProductionResearchProgress = 0;
        }

        // The tests have their own copy of this table - the real one may get tweaked, and that could throw some of
        //  the tests off.
        public static ResearchCategory hydroponicResearchCategory = new HydroponicResearchCategory();
        public static ResearchCategory farmingResearchCategory = new FarmingResearchCategory();
        public static ResearchCategory productionResearchCategory = new ProductionResearchCategory();
        public static ResearchCategory scanningResearchCategory = new ScanningResearchCategory();
        public static ResearchCategory shiniesResearchCategory = new ShiniesResearchCategory();

        private static TieredResource[] AllTieredResources =
        {
            new EdibleResource("HydroponicSnacks", ProductionRestriction.Orbit, hydroponicResearchCategory, false, false, .2, .4, .55, .7, .95),
            new EdibleResource("Snacks", ProductionRestriction.Orbit, farmingResearchCategory, true, false, .6, .85, .95, .98, 1.0),
            new TieredResource("Fertilizer", "Kerbal-Days", ProductionRestriction.LandedOnBody, productionResearchCategory, true, false),
            new TieredResource("Shinies", "Bling-per-day", ProductionRestriction.LandedOnBody, shiniesResearchCategory, true, false),
            new TieredResource("Stuff", null, ProductionRestriction.LandedOnBody, productionResearchCategory, true, false),
            new TieredResource("ScanningData", "Kerbal-Days", ProductionRestriction.OrbitOfBody, scanningResearchCategory, false, true)
        };

        public static TieredResource GetTieredResourceByName(string name)
        {
            return AllTieredResources.First(tr => tr.BaseName == name);
        }

        public bool TryParseTieredResourceName(string tieredResourceName, out TieredResource resource, out TechTier tier)
        {
            int dashIndex = tieredResourceName.IndexOf('-');
            if (dashIndex < 0)
            {
                resource = GetTieredResourceByName(tieredResourceName);
                tier = TechTier.Tier4;
                return resource != null;
            }
            else
            {
                try
                {
                    // Oh, but we do pine ever so much for .Net 4.6...
                    tier = (TechTier)Enum.Parse(typeof(TechTier), tieredResourceName.Substring(dashIndex + 1));
                    var tier4Name = tieredResourceName.Substring(0, dashIndex);
                    resource = GetTieredResourceByName(tier4Name);
                    return resource != null;
                }
                catch (Exception)
                {
                    resource = null;
                    tier = TechTier.Tier0;
                    return false;
                }
            }
        }

        public bool ContributeResearch(TieredResource source, string atBody, double timespentInKerbalSeconds)
        {
            if (source.ResearchCategory == hydroponicResearchCategory)
            {
                this.AgroponicResearchProgress += timespentInKerbalSeconds;
                if (this.AgroponicResearchProgress > ColonizationResearchScenario.KerbalYearsToKerbalSeconds(source.ResearchCategory.KerbalYearsToNextTier(this.AgroponicsMaxTier)))
                {
                    this.AgroponicResearchProgress = 0;
                    ++this.AgroponicsMaxTier;
                    return true;
                }
            }
            else if (source.ResearchCategory == farmingResearchCategory)
            {
                this.AgricultureResearchProgress += timespentInKerbalSeconds;
            }
            else if (source.ResearchCategory == productionResearchCategory)
            {
                this.ProductionResearchProgress += timespentInKerbalSeconds;
            }
            return false;
        }

        public TechTier GetMaxUnlockedTier(TieredResource forResource, string atBody)
        {
            return TechTier.Tier0;
        }
    }
}
