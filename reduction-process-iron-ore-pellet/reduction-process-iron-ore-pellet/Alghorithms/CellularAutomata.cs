using System;
using System.Collections.Generic;
using System.Linq;
using grain_growth.Helpers;
using grain_growth.Models;
using FastBitmapLib;
using System.Drawing;

namespace grain_growth.Algorithms
{
    public class CellularAutomata
    {
        private readonly Random Random = new Random();

        private List<Grain> Neighborhood;

        private bool IsGrainUpdate;

        private IOrderedEnumerable<IGrouping<int, Grain>> MostOfCells;

        private Properties properties;

        CellularAutomata() { }

        CellularAutomata(Properties properties)
        {
            this.properties = properties;
        }

        public Range Grow(Range prevRange)
        {
            var currRange = new Range(prevRange.Width, prevRange.Height, true);

            InitStructure.GrainsArrayInit(currRange);
            InitStructure.UpdateInsideCircle(currRange);

            foreach (var p in InitStructure.PointsArea)
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
                                Neighborhood = TakeMooreNeighbourhood(p.X, p.Y, prevRange.GrainsArray);
                                break;
                            case NeighbourhoodType.Neumann:
                                Neighborhood = TakeNeumannNeighbourhood(p.X, p.Y, prevRange.GrainsArray);
                                break;
                        }

                        var most = Neighborhood.Where(g => !SpecialId.IsSpecialId(g.Id))
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

                        IsGrainUpdate = false;

                        // rule 1 - ordinary moore
                        Neighborhood = TakeMooreNeighbourhood(p.X, p.Y, prevRange.GrainsArray);

                        MostOfCells = Neighborhood.Where(g => !SpecialId.IsSpecialId(g.Id))
                                                .GroupBy(g => g.Id).OrderByDescending(g => g.Count());

                        if (MostOfCells.Any())
                        {
                            if (MostOfCells.First().Count() >= 5 && MostOfCells.First().Count() <= 8)
                            {
                                currRange.GrainsArray[p.X, p.Y] = MostOfCells.Select(g => g.First()).First();
                            }
                            else
                            {
                                // rule 2 - nearest moore
                                Neighborhood = TakeNearestMooreNeighbourhood(p.X, p.Y, prevRange.GrainsArray);

                                MostOfCells = Neighborhood.Where(g => !SpecialId.IsSpecialId(g.Id))
                                                    .GroupBy(g => g.Id).OrderByDescending(g => g.Count());

                                if (MostOfCells.Any())
                                {
                                    if (MostOfCells.First().Count() == 3)
                                    {
                                        currRange.GrainsArray[p.X, p.Y] = MostOfCells.Select(g => g.First()).First();
                                        IsGrainUpdate = true;
                                    }
                                }
                                if (!IsGrainUpdate)
                                {
                                    // rule 3 - further moore
                                    Neighborhood = TakeFurtherMooreNeighbourhood(p.X, p.Y, prevRange.GrainsArray);

                                    MostOfCells = Neighborhood.Where(g => !SpecialId.IsSpecialId(g.Id))
                                                        .GroupBy(g => g.Id).OrderByDescending(g => g.Count());

                                    if (MostOfCells.Any())
                                    {
                                        if (MostOfCells.First().Count() == 3)
                                        {
                                            currRange.GrainsArray[p.X, p.Y] = MostOfCells.Select(g => g.First()).First();
                                            IsGrainUpdate = true;
                                        }
                                    }
                                    if (!IsGrainUpdate)
                                    {
                                        // rule 4 - ordinary moore with probability
                                        Neighborhood = TakeMooreNeighbourhood(p.X, p.Y, prevRange.GrainsArray);

                                        MostOfCells = Neighborhood.Where(g => !SpecialId.IsSpecialId(g.Id))
                                                            .GroupBy(g => g.Id).OrderByDescending(g => g.Count());

                                        if (MostOfCells.Any() && Random.Next(0, 100) <= properties.CurrGrowthProbability)
                                        {
                                            currRange.GrainsArray[p.X, p.Y] = MostOfCells.Select(g => g.First()).First();
                                            IsGrainUpdate = true;
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

        public static void UpdateBitmap(Phase phase, Bitmap mainBitmap)
        {
            //Stopwatch sw = Stopwatch.StartNew();
            //for (int i = 0; i < range.Width; i++)
            //    for (int j = 0; j < range.Height; j++)
            //        range.StructureBitmap.SetPixel(i, j, range.GrainsArray[i, j].Color);
            //Console.WriteLine("Serial: {0:f2} s", sw.Elapsed.TotalSeconds);

            //Stopwatch sw = Stopwatch.StartNew();
            using (FastBitmap fastBitmap = mainBitmap.FastLock())
            {
                for (int i = 1; i < mainBitmap.Width - 1; ++i)
                    for (int j = 1; j < mainBitmap.Height - 1; ++j)
                        if (phase.Range.GrainsArray[i, j].Id != (int)SpecialId.Id.Transparent &&
                            phase.Range.GrainsArray[i, j].Id != (int)SpecialId.Id.Empty)
                        {
                            fastBitmap.SetPixel(i, j, phase.Range.GrainsArray[i, j].Color);
                            if(phase.Range.GrainsArray[i, j].Id != (int)SpecialId.Id.Border)
                                ++phase.Counter;
                        }
            }
            //Console.WriteLine("Serial: {0:f2} s", sw.Elapsed.TotalSeconds);
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
