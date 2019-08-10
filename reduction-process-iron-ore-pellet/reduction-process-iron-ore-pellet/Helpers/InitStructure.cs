using System;
using System.Collections.Generic;
using System.Drawing;
using grain_growth.Models;

namespace grain_growth.Helpers
{
    public class InitStructure
    {
        public static Grain[] AllGrainsTypes;

        public static readonly List<Point> PointsInside = InitStructure.GetPointsInsideCircle(149, new Point(150, 150));

        public static Range InitCellularAutomata(Models.Properties properties, Color grainColor)
        {
            AllGrainsTypes = new Grain[properties.AmountOfGrains];

            Range tempRange = new Range(properties.RangeWidth, properties.RangeHeight);

            // init grains array by transparent (not used)
            GrainsArrayInit(tempRange);

            // init grains array by white color (empty grains)
            UpdateInsideCircle(tempRange);

            // set random starting coordinates [x,y] and color for grains on circle boundary (equal sections*)
            Point coordinates = RandomCoordinates.Get(tempRange.Width, tempRange.Height);
            for (int grainNumber = 1; grainNumber <= properties.AmountOfGrains; grainNumber++)
            {
                double angle = grainNumber * (360 / properties.AmountOfGrains);
                coordinates.X = (int)(150 + 149 * Math.Cos(angle));
                coordinates.Y = (int)(150 + 149 * Math.Sin(angle));

                AllGrainsTypes[grainNumber - 1] = new Grain()
                {
                    Color = grainColor,
                    Id = grainNumber,
                };

                tempRange.GrainsArray[coordinates.X, coordinates.Y].Color = AllGrainsTypes[grainNumber - 1].Color;
                tempRange.GrainsArray[coordinates.X, coordinates.Y].Id = AllGrainsTypes[grainNumber - 1].Id;
            }

            //// set random starting coordinates [x,y] and color for grains on circle boundary in random way
            //Point coordinates;
            //for (int grainNumber = 1; grainNumber <= properties.AmountOfGrains; grainNumber++)
            //{
            //    do
            //    {
            //        coordinates = RandomCoordinates.Get(tempRange.Width, tempRange.Height, Random);
            //    }
            //    while (tempRange.GrainsArray[coordinates.X, coordinates.Y].Id != -1);

            //    AllGrainsTypes[grainNumber - 1] = new Grain()
            //    {
            //        Color = grainColor,
            //        Id = grainNumber,
            //    };

            //    tempRange.GrainsArray[coordinates.X, coordinates.Y].Color = AllGrainsTypes[grainNumber - 1].Color;
            //    tempRange.GrainsArray[coordinates.X, coordinates.Y].Id = AllGrainsTypes[grainNumber - 1].Id;
            //}

            //// set random starting coordinates [x,y] and color for grains inside circle
            //Point coordinates;
            //for (int grainNumber = 1; grainNumber <= properties.AmountOfGrains; grainNumber++)
            //{
            //    do
            //    {
            //        coordinates = RandomCoordinates.Get(tempRange.Width, tempRange.Height, Random);
            //    }
            //    while (tempRange.GrainsArray[coordinates.X, coordinates.Y].Id != 0);

            //    AllGrainsTypes[grainNumber - 1] = new Grain()
            //    {
            //        Color = Color.FromArgb(Random.Next(10, 240), Random.Next(10, 240), Random.Next(2, 240)),
            //        Id = grainNumber,
            //    };

            //    tempRange.GrainsArray[coordinates.X, coordinates.Y].Color = AllGrainsTypes[grainNumber - 1].Color;
            //    tempRange.GrainsArray[coordinates.X, coordinates.Y].Id = AllGrainsTypes[grainNumber - 1].Id;
            //}

            tempRange.StructureBitmap = new Bitmap(properties.RangeWidth, properties.RangeHeight);
            tempRange.IsFull = false;
            return tempRange;
        }

        public static void GrainsArrayInit(Range tempRange)
        {
            for (int i = 0; i < tempRange.Width; i++)
                for (int j = 0; j < tempRange.Height; j++)
                    if (tempRange.GrainsArray[i, j] == null)
                        tempRange.GrainsArray[i, j] = new Grain()
                        {
                            Id = (int)SpecialId.Id.Transparent,
                            Color = Color.White,
                        };
        }

        public static unsafe void SetPixel(Range tempRange, int x, int y)
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

        public static void DrawBlackCircleBorder(Range tempRange)
        {
            int x, y, x0, y0, d, radius = tempRange.Height / 2;
            x0 = radius;
            y0 = radius;
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

        public static void UpdateInsideCircle(Range tempRange)
        {
            foreach (var point in PointsInside)
            {
                if (tempRange.GrainsArray[point.X, point.Y].Id == -9)
                {
                    tempRange.GrainsArray[point.X, point.Y].Color = Color.White;
                    tempRange.GrainsArray[point.X, point.Y].Id = (int)SpecialId.Id.Empty;
                }
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

        public static void DrawBlackSquareBorder(Range tempRange)
        {
            for (int i = 0; i < tempRange.Height; i++)
            {
                tempRange.GrainsArray[0, i] = new Grain()
                {
                    Id = (int)SpecialId.Id.Border,
                    Color = Color.Black
                };
                tempRange.GrainsArray[tempRange.Width - 1, i] = new Grain()
                {
                    Id = (int)SpecialId.Id.Border,
                    Color = Color.Black
                };
            }

            for (int i = 0; i < tempRange.Width; i++)
            {
                tempRange.GrainsArray[i, 0] = new Grain()
                {
                    Id = (int)SpecialId.Id.Border,
                    Color = Color.Black
                };
                tempRange.GrainsArray[i, tempRange.Height - 1] = new Grain()
                {
                    Id = (int)SpecialId.Id.Border,
                    Color = Color.Black
                };
            }
        }
    }
}