﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FinePrint;
using UnityEngine;

namespace ProgressiveColonizationSystem
{

    public class PksSpecialStuffScrounger
        : PksTieredResourceConverter
    {
        [KSPEvent(guiActive = true)]
        public void FindResource()
        {
            ResourceLodeScenario.Instance.GetOrCreateResourceLoad(this.vessel, Tier);
        }

        protected override bool CanDoProduction(ModuleResourceConverter resourceConverter, out string reasonWhyNotMessage)
        {
            if (!base.CanDoProduction(resourceConverter, out reasonWhyNotMessage))
            {
                return false;
            }

            return ResourceLodeScenario.Instance.TryFindResourceLodeInRange(this.vessel, out _);
        }
    }
}
