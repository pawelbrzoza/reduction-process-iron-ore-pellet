using System;
using System.Drawing;

namespace grain_growth.Models
{
    public static class Constants
    {
        public const int NUMBER_OF_PHASES = 4;
        public static double RADIOUS;
        public static double DIAMETER;
        public static double CIRCLE_AREA;
        public static Point MIDDLE_POINT = new Point(151, 151);

        public static void UpdateConstants(double r)
        {
            RADIOUS = r;
            DIAMETER = RADIOUS * 2;
            CIRCLE_AREA = Math.PI * Math.Pow(RADIOUS-1, 2);
        }
    }
}
