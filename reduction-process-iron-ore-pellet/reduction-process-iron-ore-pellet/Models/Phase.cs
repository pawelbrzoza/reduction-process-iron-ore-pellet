using grain_growth.Algorithms;
using grain_growth.Helpers;
using System;
using System.Drawing;

namespace grain_growth.Models
{
    public class Phase
    {
        public Range Range { get; set; }

        public Range CurrRange { get; set; }

        public Range PrevRange { get; set; }
        
        public CellularAutomata CA { get; set; }

        public MainProperties Properties { get; set; }

        public string Name { get; set; }

        public double Percentage { get; set; }

        public double Counter { get; set; }

        public double TemperaturePoint { get; set; }

        public int GrowthProbability { get; set; }

        public Color Color { get; set; }
    }
}
