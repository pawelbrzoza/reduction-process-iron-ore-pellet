using System;
using System.Collections.Generic;
using System.Drawing;

using grain_growth.Models;
using grain_growth.Helpers;

namespace grain_growth.Algorithms
{
    public class InclusionHandler : RandomCoordinate
    {
        private static List<Point> Points;

        public static void InitPoints(double percentageOfInclusions)
        {
            Points = new List<Point>();
            double amount = percentageOfInclusions / 100.0 * Math.PI * Math.Pow(Constants.RADIUS, 2);

            for (int i = 0; i < amount; i++)
                Points.Add(RandomPointInsideCircle(Constants.MIDDLE_POINT, Constants.RADIUS));
        }

        public static void AddInclusions(Range range)
        {
            foreach (var point in Points)
            {
                range.GrainsArray[point.X, point.Y].Id = (int)SpecialId.Id.Border;
                range.GrainsArray[point.X, point.Y].Color = Color.Black;
            }
        }

        private static Point RandomPointInsideCircle(Point middle, double radius)
        {
            Point p;
            do
            {
                var angle = Random.NextDouble() * Math.PI * 2;
                var r = Math.Sqrt(Random.NextDouble()) * radius;
                double x = middle.X + r * Math.Cos(angle);
                double y = middle.Y + r * Math.Sin(angle);
                p = new Point((int)x, (int)y);
            } while(Points.Contains(p));

            return p;
        }
    }
}
