
using System;

namespace grain_growth.Models
{
    [Serializable]
    public class Properties
    {
        public int RangeWidth { get; set; }

        public int RangeHeight { get; set; }

        public int AmountOfGrains { get; set; }

        public int CurrGrowthProbability { get; set; }

        public int ConstGrowthProbability { get; set; }

        public int MaxTemperature { get; set; }

        public int CurrTemperature { get; set; }

        public int RiseOfTemperature { get; set; }

        public int BufferTemperature { get; set; }

        public NeighbourhoodType NeighbourhoodType { get; set; }

        public StartingPointsType StartingPointsType { get; set; }
    }
}
