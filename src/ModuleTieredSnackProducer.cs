﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public abstract class ModuleTieredSnackProducer
        : TieredResourceCoverter,
          ISnackProducer
    {
        [KSPField]
        public float capacity;

		protected override string RequiredCrewTrait => "Scientist";

        public abstract double MaxConsumptionForProducedFood { get; }

		public double Capacity => this.capacity;
	}
}
