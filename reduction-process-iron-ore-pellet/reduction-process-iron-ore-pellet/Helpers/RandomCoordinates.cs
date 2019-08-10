using System;
using System.Drawing;

namespace grain_growth.Helpers
{
    public class RandomCoordinates
    {
        public static Random Random = new Random();

        public static Point Get(int width, int height)
        {
            return new Point(Random.Next(1, width - 1), Random.Next(1, height - 1));
        }
    }
}
