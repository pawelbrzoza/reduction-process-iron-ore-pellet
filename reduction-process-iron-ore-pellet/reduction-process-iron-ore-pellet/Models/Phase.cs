using System;
using System.Drawing;

namespace grain_growth.Models
{
    public class Phase
    {
        public String Name { get; set; }

        public Range Range { get; set; }

        public double Percentage { get; set; }

        public double Counter { get; set; }

        public bool Started { get; set; }

        public double TemperaturePoint { get; set; }

        public Color Color { get; set; }

        public int GrowthProbability { get; set; }
    }
}
