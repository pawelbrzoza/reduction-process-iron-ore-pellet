using System;
using System.Collections.Generic;
using System.Linq;
using grain_growth.Helpers;
using grain_growth.Models;
using FastBitmapLib;

namespace grain_growth.Alghorithms
{
    public class CellularAutomata
    {
        private readonly Random Random = new Random();

        private List<Grain> Neighbourhood;

        public Range Grow(Range prevRange, Models.Properties properties)
        {
            var currRange = new Range(prevRange.Width, prevRange.Height, true);

            InitStructure.GrainsArrayInit(currRange);
            InitStructure.UpdateInsideCircle(currRange);

            foreach (var p in InitStructure.PointsInside)
            {
                if (prevRange.GrainsArray[p.X, p.Y].Id != (int)SpecialId.Id.Empty)
                {
                    // just init if there is already some color (not white)
                    currRange.GrainsArray[p.X, p.Y].Id = prevRange.GrainsArray[p.X, p.Y].Id;
                    currRange.GrainsArray[p.X, p.Y].Color = prevRange.GrainsArray[p.X, p.Y].Color;
                }
                else
                {
                    if (properties.NeighbourhoodType != NeighbourhoodType.Moore2)
                    {
                        switch (properties.NeighbourhoodType)
                        {
                            case NeighbourhoodType.Moore:
                                Neighbourhood = TakeMooreNeighbourhood(p.X, p.Y, prevRange.GrainsArray);
                                break;
                            case NeighbourhoodType.Neumann:
                                Neighbourhood = TakeNeumannNeighbourhood(p.X, p.Y, prevRange.GrainsArray);
                                break;
                        }

                        var most = Neighbourhood.Where(g => !SpecialId.IsSpecialId(g.Id))
                                                .GroupBy(g => g.Id);

                        if (most.Any())
                        {
                            // assign grain which are the most in the list of neighborhoods 
                            currRange.GrainsArray[p.X, p.Y] = most.OrderByDescending(g => g.Count())
                                                                .Select(g => g.First()).First();
                        }
                    }
                    else
                    {
                        // MOORE 2

                        var isGrainUpdate = false;

                        // rule 1 - ordinary moore
                        Neighbourhood = TakeMooreNeighbourhood(p.X, p.Y, prevRange.GrainsArray);

                        var most = Neighbourhood.Where(g => !SpecialId.IsSpecialId(g.Id))
                                                .GroupBy(g => g.Id).OrderByDescending(g => g.Count());

                        if (most.Any())
                        {
                            if (most.First().Count() >= 5 && most.First().Count() <= 8)
                            {
                                currRange.GrainsArray[p.X, p.Y] = most.Select(g => g.First()).First();
                            }
                            else
                            {
                                // rule 2 - nearest moore
                                Neighbourhood = TakeNearestMooreNeighbourhood(p.X, p.Y, prevRange.GrainsArray);

                                most = Neighbourhood.Where(g => !SpecialId.IsSpecialId(g.Id))
                                                    .GroupBy(g => g.Id).OrderByDescending(g => g.Count());

                                if (most.Any())
                                {
                                    if (most.First().Count() == 3)
                                    {
                                        currRange.GrainsArray[p.X, p.Y] = most.Select(g => g.First()).First();
                                        isGrainUpdate = true;
                                    }
                                }
                                if (!isGrainUpdate)
                                {
                                    // rule 3 - further moore
                                    Neighbourhood = TakeFurtherMooreNeighbourhood(p.X, p.Y, prevRange.GrainsArray);

                                    most = Neighbourhood.Where(g => !SpecialId.IsSpecialId(g.Id))
                                                        .GroupBy(g => g.Id).OrderByDescending(g => g.Count());

                                    if (most.Any())
                                    {
                                        if (most.First().Count() == 3)
                                        {
                                            currRange.GrainsArray[p.X, p.Y] = most.Select(g => g.First()).First();
                                            isGrainUpdate = true;
                                        }
                                    }
                                    if (!isGrainUpdate)
                                    {
                                        // rule 4 - ordinary moore with probability
                                        Neighbourhood = TakeMooreNeighbourhood(p.X, p.Y, prevRange.GrainsArray);

                                        most = Neighbourhood.Where(g => !SpecialId.IsSpecialId(g.Id))
                                                            .GroupBy(g => g.Id).OrderByDescending(g => g.Count());

                                        if (most.Any() && (Random.Next(0, 100) <= properties.CurrGrowthProbability))
                                        {
                                            currRange.GrainsArray[p.X, p.Y] = most.Select(g => g.First()).First();
                                            isGrainUpdate = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return currRange;
        }

        private List<Grain> TakeNeumannNeighbourhood(int i, int j, Grain[,] GrainsArray)
        {
            return new List<Grain>
            {
                GrainsArray[i, j + 1],
                GrainsArray[i + 1, j],
                GrainsArray[i - 1, j],
                GrainsArray[i, j - 1],
            };
        }

        private List<Grain> TakeMooreNeighbourhood(int i, int j, Grain[,] GrainsArray)
        {
            return new List<Grain>
            {
                GrainsArray[i - 1, j],
                GrainsArray[i + 1, j],
                GrainsArray[i, j - 1],
                GrainsArray[i, j + 1],
                GrainsArray[i - 1, j - 1],
                GrainsArray[i - 1, j + 1],
                GrainsArray[i + 1, j - 1],
                GrainsArray[i + 1, j + 1]
            };
        }

        private List<Grain> TakeNearestMooreNeighbourhood(int i, int j, Grain[,] GrainsArray)
        {
            return TakeNeumannNeighbourhood(i, j, GrainsArray);
        }

        private List<Grain> TakeFurtherMooreNeighbourhood(int i, int j, Grain[,] GrainsArray)
        {
            return new List<Grain>
            {
                GrainsArray[i - 1, j - 1],
                GrainsArray[i - 1, j + 1],
                GrainsArray[i + 1, j - 1],
                GrainsArray[i + 1, j + 1]
            };
        }

        public static void UpdateBitmap(Range range)
        {
            //Stopwatch sw = Stopwatch.StartNew();
            //for (int i = 0; i < range.Width; i++)
            //    for (int j = 0; j < range.Height; j++)
            //        range.StructureBitmap.SetPixel(i, j, range.GrainsArray[i, j].Color);
            //Console.WriteLine("Serial: {0:f2} s", sw.Elapsed.TotalSeconds);

            //Stopwatch sw = Stopwatch.StartNew();
            using (var fastBitmap = range.StructureBitmap.FastLock())
            {
                for (int i = 0; i < range.Width; i++)
                    for (int j = 0; j < range.Height; j++)
                        fastBitmap.SetPixel(i, j, range.GrainsArray[i, j].Color);
            }
            //Console.WriteLine("Serial: {0:f2} s", sw.Elapsed.TotalSeconds);
        }
    }
}
