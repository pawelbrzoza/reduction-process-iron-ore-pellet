using System;
using System.Collections.Generic;
using System.Drawing;
using FastBitmapLib;

using grain_growth.Models;
using grain_growth.Helpers;

namespace grain_growth.Algorithms
{
    public class StructureHandler : RandomCoordinate
    {
        public static List<Point> PointsArea;

        public static Range InitCellularAutomata(MainProperties properties, int id, Color grainColor)
        {
            Range tempRange = new Range(properties.RangeWidth, properties.RangeHeight);

            InitBlankArea(tempRange);

            Point coordinates = GetRandomPoint(tempRange.Width, tempRange.Height);
            if (properties.StartingPointsType == StartingPointsType.RandomBoundary)
            {
                // set random starting coordinates [x,y] and color for grains on circle boundary in random way
                for (int grainNumber = 1; grainNumber <= properties.AmountOfGrains; grainNumber++)
                {
                    do
                    {
                        coordinates = GetRandomPoint(tempRange.Width, tempRange.Height);
                    }
                    while (tempRange.GrainsArray[coordinates.X, coordinates.Y].Id != (int)SpecialId.Id.Border);

                    tempRange.GrainsArray[coordinates.X, coordinates.Y].Id = id;
                    tempRange.GrainsArray[coordinates.X, coordinates.Y].Color = grainColor;
                }
            }
            else if (properties.StartingPointsType == StartingPointsType.RegularBoundary)
            {
                // set random starting coordinates [x,y] and color for grains on circle boundary (equal sections*)
                for (int grainNumber = 1; grainNumber <= properties.AmountOfGrains; grainNumber++)
                {
                    double angle = (double)grainNumber * (360.0 / (double)properties.AmountOfGrains);
                    coordinates.X = (int)(Constants.MIDDLE_POINT.X + (Constants.RADIUS-2) * Math.Cos(angle * Math.PI / 180.0));
                    coordinates.Y = (int)(Constants.MIDDLE_POINT.Y + (Constants.RADIUS-2) * Math.Sin(angle * Math.PI / 180.0));

                    tempRange.GrainsArray[coordinates.X, coordinates.Y].Id = id;
                    tempRange.GrainsArray[coordinates.X, coordinates.Y].Color = grainColor;
                }
            }
            else
            {
                // set random starting coordinates [x,y] and color for grains inside circle
                for (int grainNumber = 1; grainNumber <= properties.AmountOfGrains; grainNumber++)
                {
                    do
                    {
                        coordinates = GetRandomPoint(tempRange.Width, tempRange.Height);
                    }
                    while (tempRange.GrainsArray[coordinates.X, coordinates.Y].Id != (int)SpecialId.Id.Empty);

                    tempRange.GrainsArray[coordinates.X, coordinates.Y].Id = id;
                    tempRange.GrainsArray[coordinates.X, coordinates.Y].Color = grainColor;
                }
            }
            return tempRange;
        }

        public static void InitBlankArea(Range tempRange)
        {
            // initialization grains array by transparent (not used)
            GrainsArrayInit(tempRange);

            // initialization grains array by white color (empty grains)
            UpdateInsideCircle(tempRange);

            // initialization black circle border
            DrawBlackCircleBorder(tempRange);
        }

        public static void GrainsArrayInit(Range tempRange)
        {
            for (int i = 0; i < tempRange.Width; i++)
                for (int j = 0; j < tempRange.Height; j++)
                    if (tempRange.GrainsArray[i, j] == null)
                        tempRange.GrainsArray[i, j] = new Grain()
                        {
                            Id = (int)SpecialId.Id.Transparent,
                            Color = Color.HotPink,
                        };
        }

        public static void UpdateInsideCircle(Range tempRange)
        {
            foreach (var p in PointsArea)
            {
                if (tempRange.GrainsArray[p.X, p.Y].Id == (int)SpecialId.Id.Transparent)
                {
                    tempRange.GrainsArray[p.X, p.Y].Id = (int)SpecialId.Id.Empty;
                    tempRange.GrainsArray[p.X, p.Y].Color = Color.White;
                }
            }
        }

        public static void CountGrains(Phase[] phases, Bitmap bitmap)
        {
            phases[0].Counter = phases[1].Counter = phases[2].Counter = phases[3].Counter = 0;
            using (FastBitmap fastBitmap = bitmap.FastLock())
            {
                for (int i = 1; i < bitmap.Width - 1; ++i)
                    for (int j = 1; j < bitmap.Height - 1; ++j)
                        for (int k = 0; k < Constants.NUMBER_OF_PHASES; k++)
                            if (fastBitmap.GetPixel(i, j) == phases[k].Color)
                                ++phases[k].Counter;
            }
        }

        public static List<Point> GetPointsInsideCircle(int radius, Point center)
        {
            List<Point> pointsInside = new List<Point>();

            for (int x = center.X - radius; x < center.X + radius; x++)
            {
                for (int y = center.Y - radius; y < center.Y + radius; y++)
                {
                    if ((x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y) < radius * radius)
                    {
                        pointsInside.Add(new Point(x, y));
                    }
                }
            }
            return pointsInside;
        }

        private static void DrawBlackCircleBorder(Range tempRange)
        {
            int x, y, x0, y0, d, radius = (int)Constants.RADIUS;
            x0 = Constants.MIDDLE_POINT.X;
            y0 = Constants.MIDDLE_POINT.Y;
            x = 0;
            y = radius;
            d = 5 - 4 * radius;
            CirclePoints(tempRange, x, y, x0, y0);
            while (y > x)
            {
                if (d < 0)
                {
                    d += x * 8 + 12;
                    x++;
                }
                else
                {
                    d += (x - y) * 8 + 20;
                    x++;
                    y--;
                }
                CirclePoints(tempRange, x, y, x0, y0);
            }
        }

        private static void SetPixel(Range tempRange, int x, int y)
        {
            if ((x >= 0) && (y >= 0) && (x < tempRange.Width) && (y < tempRange.Height))
            {
                tempRange.GrainsArray[x, y].Id = (int)SpecialId.Id.Border;
                tempRange.GrainsArray[x, y].Color = Color.Black;
            }
        }

        private static void CirclePoints(Range tempRange, int x, int y, int x0, int y0)
        {
            SetPixel(tempRange, x + x0, y + y0);
            SetPixel(tempRange, y + x0, x + y0);
            SetPixel(tempRange, y + x0, -x + y0);
            SetPixel(tempRange, x + x0, -y + y0);
            SetPixel(tempRange, -y + x0, -x + y0);
            SetPixel(tempRange, -x + x0, -y + y0);
            SetPixel(tempRange, -y + x0, x + y0);
            SetPixel(tempRange, -x + x0, y + y0);
        }

        public static void UpdateBitmap(Phase phase, Bitmap mainBitmap)
        {
            using (FastBitmap fastBitmap = mainBitmap.FastLock())
            {
                for (int i = 1; i < mainBitmap.Width - 1; ++i)
                    for (int j = 1; j < mainBitmap.Height - 1; ++j)
                    {
                        if (phase.Range.GrainsArray[i, j].Id != (int)SpecialId.Id.Transparent &&
                            phase.Range.GrainsArray[i, j].Id != (int)SpecialId.Id.Empty)
                        {
                            fastBitmap.SetPixel(i, j, phase.Range.GrainsArray[i, j].Color);
                        }
                    }
            }
        }

        public static void InstantFillColor(Phase phase, int id, Color color)
        {
            for (int i = 1; i < phase.Range.Width - 1; ++i)
                for (int j = 1; j < phase.Range.Height - 1; ++j)
                    if (phase.Range.GrainsArray[i, j].Id == (int)SpecialId.Id.Empty)
                    {
                        phase.Range.GrainsArray[i, j].Id = id;
                        phase.Range.GrainsArray[i, j].Color = color;
                    }
        }
    }
}
