
using System;

namespace grain_growth.Models
{
    [Serializable]
    public class Properties
    {
        public int RangeWidth { get; set; }

        public int RangeHeight { get; set; }

        public int AmountOfGrains { get; set; }

        public double CurrGrowthProbability { get; set; }

        public int MaxTemperature { get; set; }

        public int CurrTemperature { get; set; }

        public int RiseOfTemperature { get; set; }

        public double GrowthProbability { get; set; }

        public double Fe2O3Temperature { get; set; }

        public double Fe3O4Temperature { get; set; }

        public double FeOTemperature { get; set; }

        public double FeTemperature { get; set; }

        public NeighbourhoodType NeighbourhoodType { get; set; }
    }
}
