﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public interface ITieredProducer
    {
        TechTier Tier { get; set; }
        double ProductionRate { get; }
        bool IsResearchEnabled { get; }
        bool IsProductionEnabled { get; }

        /// <summary>
        ///   Contribute production amount to the research stack
        /// </summary>
        /// <param name="amount">The amount of units of supplies manufactured.</param>
        /// <returns>True if there was a research breakthrough as a result of this.</returns>
        bool ContributeResearch(IColonizationResearchScenario target, double amount);

		TieredResource Output { get; }

        /// <summary>
        ///   If this producer needs something to make what it's making, this is set to what it needs.
        /// </summary>
        TieredResource Input { get; }

        string Body { get; set; }
    }
}
