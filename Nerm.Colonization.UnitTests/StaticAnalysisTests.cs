﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nerm.Colonization.UnitTests
{
    [TestClass]
    public class StaticAnalysisTests
    {
        private StubColonizationResearchScenario colonizationResearch;
        private StubProducer drill1 = new StubProducer(StubColonizationResearchScenario.Stuff, null, 11, TechTier.Tier1);
        private StubProducer fertFactory1 = new StubProducer(StubColonizationResearchScenario.Fertilizer, StubColonizationResearchScenario.Stuff, 6, TechTier.Tier1);
        private StubProducer farm1 = new StubProducer(StubColonizationResearchScenario.Snacks, StubColonizationResearchScenario.Fertilizer, 3, TechTier.Tier1);
        private StubProducer farm2 = new StubProducer(StubColonizationResearchScenario.Snacks, StubColonizationResearchScenario.Fertilizer, 3, TechTier.Tier1);
        private StubProducer shinies1 = new StubProducer(StubColonizationResearchScenario.Shinies, StubColonizationResearchScenario.Stuff, 5, TechTier.Tier1);
        private StubContainer snacksContainer = new StubContainer() { Content = StubColonizationResearchScenario.Snacks, Tier = TechTier.Tier1, Amount = 0 };
        private StubContainer fertOutputContainer = new StubContainer() { Content = StubColonizationResearchScenario.Fertilizer, Tier = TechTier.Tier1, Amount = 0 };
        private StubContainer fertInputContainer = new StubContainer() { Content = StubColonizationResearchScenario.Fertilizer, Tier = TechTier.Tier4, Amount = 100 };
        private StubContainer shiniesContainer = new StubContainer() { Content = StubColonizationResearchScenario.Shinies, Tier = TechTier.Tier1, Amount = 0 };
        private StubProducer hydro1 = new StubProducer(StubColonizationResearchScenario.HydroponicSnacks, StubColonizationResearchScenario.Fertilizer, 1, TechTier.Tier2);
        private StubProducer hydro2 = new StubProducer(StubColonizationResearchScenario.HydroponicSnacks, StubColonizationResearchScenario.Fertilizer, 2, TechTier.Tier2);

        private List<ITieredProducer> producers;
        private List<ITieredContainer> containers;

        [TestInitialize]
        public void TestInitialize()
        {
            // We set up for complete happiness
            colonizationResearch = new StubColonizationResearchScenario(TechTier.Tier2);
            colonizationResearch.SetMaxTier(StubColonizationResearchScenario.farmingResearchCategory, "munmuss", TechTier.Tier1);
            colonizationResearch.SetMaxTier(StubColonizationResearchScenario.productionResearchCategory, "munmuss", TechTier.Tier1);
            colonizationResearch.SetMaxTier(StubColonizationResearchScenario.scanningResearchCategory, "munmuss", TechTier.Tier1);
            colonizationResearch.SetMaxTier(StubColonizationResearchScenario.shiniesResearchCategory, "munmuss", TechTier.Tier1);

            producers = new List<ITieredProducer>()
            {
                this.drill1,
                this.fertFactory1,
                this.farm1,
                this.farm2,
                this.shinies1,
            };
            containers = new List<ITieredContainer>()
            {
                this.snacksContainer,
                this.fertOutputContainer,
                this.shiniesContainer,
            };
        }

        // CheckBodyIsSet

        [TestMethod]
        public void WarningsTest_NoPartsTest()
        {
            var result = StaticAnalysis.CheckBodyIsSet(colonizationResearch, new List<ITieredProducer>(), new List<ITieredContainer>());
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Any());
        }

        [TestMethod]
        public void WarningsTest_HappyParts()
        {
            var result = StaticAnalysis.CheckBodyIsSet(colonizationResearch, this.producers, this.containers);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Any());
        }


        [TestMethod]
        public void WarningsTest_MissingBodyAssignment()
        {
            this.farm1.Body = null;
            this.farm2.Body = null;
            var actual = StaticAnalysis.CheckBodyIsSet(colonizationResearch, this.producers, this.containers).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual("Need to set up the target for the world-specific parts", actual[0].Message);
            Assert.IsNotNull(actual[0].FixIt);
            actual[0].FixIt();
            Assert.AreEqual("munmuss", this.farm1.Body);
            actual = StaticAnalysis.CheckBodyIsSet(colonizationResearch, this.producers, this.containers).ToList();
            Assert.AreEqual(0, actual.Count);

            // If nothing is set up
            foreach (var p in this.producers)
            {
                p.Body = null;
            }
            actual = StaticAnalysis.CheckBodyIsSet(colonizationResearch, this.producers, this.containers).ToList();
            // Then it gets complained about, but no fix is offered
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual("Need to set up the target for the world-specific parts", actual[0].Message);
            Assert.IsNull(actual[0].FixIt);
        }

        [TestMethod]
        public void WarningsTest_MismatchedBodyAssignment()
        {
            this.farm1.Body = "splut";
            this.farm2.Body = null;
            var actual = StaticAnalysis.CheckBodyIsSet(colonizationResearch, this.producers, this.containers).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual("Not all of the body-specific parts are set up for munmuss", actual[0].Message);
            Assert.IsNotNull(actual[0].FixIt);
            actual[0].FixIt();
            Assert.AreEqual("munmuss", this.farm1.Body);
            Assert.AreEqual("munmuss", this.farm2.Body);
            actual = StaticAnalysis.CheckBodyIsSet(colonizationResearch, this.producers, this.containers).ToList();
            Assert.AreEqual(0, actual.Count);
        }

        // CheckTieredProduction

        [TestMethod]
        public void WarningsTest_CheckTieredProduction_Hydroponics()
        {
            List<ITieredProducer> hydroProducers = new List<ITieredProducer>() { this.hydro1, this.hydro2 };
            List<ITieredContainer> hydroContainers = new List<ITieredContainer>() { this.snacksContainer, this.fertInputContainer };

            // Verify no false-positives.
            var actual = StaticAnalysis.CheckTieredProduction(colonizationResearch, hydroProducers, hydroContainers).ToList();
            Assert.AreEqual(0, actual.Count);

            hydroProducers[0].Tier = TechTier.Tier0;
            hydroProducers[1].Tier = TechTier.Tier2;
            actual = StaticAnalysis.CheckTieredProduction(colonizationResearch, hydroProducers, hydroContainers).ToList();
            Assert.AreEqual(2, actual.Count);
            Assert.AreEqual($"All orbital-production parts should be set to {TechTier.Tier2.DisplayName()}", actual[0].Message);
            Assert.IsNotNull(actual[0].FixIt);
            actual[0].FixIt();
            Assert.AreEqual(TechTier.Tier2, hydroProducers[0].Tier);
            Assert.AreEqual(TechTier.Tier2, hydroProducers[0].Tier);

            Assert.AreEqual($"Not all of the parts producing {StubColonizationResearchScenario.HydroponicSnacks.BaseName} are set at {TechTier.Tier2.DisplayName()}", actual[1].Message);
            Assert.IsNotNull(actual[1].FixIt);
        }


        [TestMethod]
        public void WarningsTest_CheckTieredProduction_LandedUndertiered()
        {
            // Verify no false-positives.
            var actual = StaticAnalysis.CheckTieredProduction(colonizationResearch, this.producers, this.containers).ToList();
            Assert.AreEqual(0, actual.Count);

            // Validate it catches that it's consistent, but undertiered
            foreach (var p in this.producers) p.Tier = TechTier.Tier0;
            actual = StaticAnalysis.CheckTieredProduction(colonizationResearch, this.producers, this.containers).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(actual[0].Message, $"All production parts should be set to {TechTier.Tier1.DisplayName()}");
            Assert.IsNotNull(actual[0].FixIt);
            actual[0].FixIt();
            actual = StaticAnalysis.CheckTieredProduction(colonizationResearch, this.producers, this.containers).ToList();
            Assert.AreEqual(0, actual.Count);
        }

        [TestMethod]
        public void WarningsTest_CheckTieredProduction_LandedMixedupFarms()
        {
            // Validate it catches that it's consistent, but undertiered
            farm1.Tier = TechTier.Tier0;
            var actual = StaticAnalysis.CheckTieredProduction(colonizationResearch, this.producers, this.containers).ToList();
            Assert.AreEqual(2, actual.Count);
            Assert.AreEqual(actual[0].Message, $"All production parts should be set to {TechTier.Tier1.DisplayName()}");
            Assert.AreEqual(actual[1].Message, $"Not all of the parts producing {farm1.Output.BaseName} are set at {farm2.Tier}");
            Assert.IsNotNull(actual[1].FixIt);
            actual[1].FixIt();
            Assert.AreEqual(TechTier.Tier1, farm1.Tier);
            actual = StaticAnalysis.CheckTieredProduction(colonizationResearch, this.producers, this.containers).ToList();
            Assert.AreEqual(0, actual.Count);
        }

        [TestMethod]
        public void WarningsTest_CheckTieredProduction_MixedTiers()
        {
            colonizationResearch.SetMaxTier(StubColonizationResearchScenario.farmingResearchCategory, "munmuss", TechTier.Tier2);

            // Validate it catches that it's consistent, but undertiered
            farm1.Tier = TechTier.Tier2;
            farm2.Tier = TechTier.Tier2;
            var actual = StaticAnalysis.CheckTieredProduction(colonizationResearch, this.producers, this.containers).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(actual[0].Message, $"There are {TechTier.Tier2.DisplayName()} producers of Snacks, but it requires equal-tier {StubColonizationResearchScenario.Fertilizer.BaseName} production in order to work.");
            Assert.IsTrue(actual[0].IsClearlyBroken);
            Assert.IsNull(actual[0].FixIt);

            colonizationResearch.SetMaxTier(StubColonizationResearchScenario.productionResearchCategory, "munmuss", TechTier.Tier2);
            fertFactory1.Tier = TechTier.Tier2;
            drill1.Tier = TechTier.Tier2;
            actual = StaticAnalysis.CheckTieredProduction(colonizationResearch, this.producers, this.containers).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(actual[0].Message, $"Scanning technology at munmuss has not progressed beyond {TechTier.Tier1.DisplayName()} - scroungers won't produce if a scanner at their tier is present in-orbit.");
            Assert.IsTrue(actual[0].IsClearlyBroken);
            Assert.IsNull(actual[0].FixIt);
        }

        [TestMethod]
        public void WarningsTest_CheckCorrectCapacity()
        {
            // Verify no false-positives.
            var actual = StaticAnalysis.CheckCorrectCapacity(colonizationResearch, this.producers, this.containers).ToList();
            Assert.AreEqual(0, actual.Count);

            // Verify if we need an excess, it gets reported
            this.producers.Add(new StubProducer(StubColonizationResearchScenario.Snacks, StubColonizationResearchScenario.Fertilizer, 3, TechTier.Tier1));
            actual = StaticAnalysis.CheckCorrectCapacity(colonizationResearch, this.producers, this.containers).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual($"The ship needs at least 9 production of {StubColonizationResearchScenario.Fertilizer.BaseName} but it is only producing 6", actual[0].Message);
            Assert.IsFalse(actual[0].IsClearlyBroken);
            Assert.IsNull(actual[0].FixIt);

            // Verify it catches missing stuff in storage
            List<ITieredProducer> hydroProducers = new List<ITieredProducer>() { this.hydro1, this.hydro2 };
            List<ITieredContainer> hydroContainers = new List<ITieredContainer>() { this.snacksContainer, this.fertInputContainer };
            // Verify no false-positives.
            actual = StaticAnalysis.CheckCorrectCapacity(colonizationResearch, hydroProducers, hydroContainers).ToList();
            Assert.AreEqual(0, actual.Count);

            // What if we forgot the fertilizer
            hydroContainers[1].Amount = 0;
            actual = StaticAnalysis.CheckCorrectCapacity(colonizationResearch, hydroProducers, hydroContainers).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual($"The ship needs {StubColonizationResearchScenario.Fertilizer.BaseName} to produce {StubColonizationResearchScenario.HydroponicSnacks.BaseName}", actual[0].Message);
            Assert.IsNull(actual[0].FixIt);
        }

        [TestMethod]
        public void WarningsTest_CheckTieredProductionStorage()
        {
            // Verify no false-positives.
            var actual = StaticAnalysis.CheckTieredProductionStorage(colonizationResearch, this.producers, this.containers).ToList();
            Assert.AreEqual(0, actual.Count);

            this.fertOutputContainer.Tier = TechTier.Tier0;
            actual = StaticAnalysis.CheckTieredProductionStorage(colonizationResearch, this.producers, this.containers).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual($"This craft is producing {StubColonizationResearchScenario.Fertilizer.TieredName(TechTier.Tier1)} but there's no storage for it.", actual[0].Message);
            Assert.IsFalse(actual[0].IsClearlyBroken);
            Assert.IsNull(actual[0].FixIt);
        }

        [TestMethod]
        public void WarningsTest_CheckExtraBaggage()
        {
            // Verify no false-positives.
            var actual = StaticAnalysis.CheckExtraBaggage(colonizationResearch, this.producers, this.containers).ToList();
            Assert.AreEqual(0, actual.Count);

            this.fertOutputContainer.Amount = 1;
            actual = StaticAnalysis.CheckExtraBaggage(colonizationResearch, this.producers, this.containers).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual($"This vessel is carrying {StubColonizationResearchScenario.Fertilizer.TieredName(TechTier.Tier1)}.  That kind of cargo that should just be produced - that's fine for testing mass & delta-v, but you wouldn't really want to fly this way.", actual[0].Message);
            Assert.IsFalse(actual[0].IsClearlyBroken);
            Assert.IsNull(actual[0].FixIt);
        }
    }
}
