using System;
using System.Drawing;

namespace grain_growth.Models
{
    public class Constants
    {
        public const int NUMBER_OF_PHASES = 4;
        public static double RADIUS;
        public static double DIAMETER;
        public static double CIRCLE_AREA;
        public static Point MIDDLE_POINT = new Point(151, 151);

        public static void UpdateConstants(double r)
        {
            RADIUS = r;
            DIAMETER = RADIUS * 2;
            CIRCLE_AREA = Math.PI * Math.Pow(RADIUS-0.43, 2);
        }
    }
}
