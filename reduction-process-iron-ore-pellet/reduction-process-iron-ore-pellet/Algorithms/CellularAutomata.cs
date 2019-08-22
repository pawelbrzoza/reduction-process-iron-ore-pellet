using System;
using System.Collections.Generic;
using System.Linq;
using grain_growth.Helpers;
using grain_growth.Models;

namespace grain_growth.Algorithms
{
    public class CellularAutomata : RandomCoordinate
    {
        private List<Grain> NeighborhoodList;

        private bool IsGrainUpdate;

        private IOrderedEnumerable<IGrouping<int, Grain>> MostOfCells;

        public Range Grow(Range prevRange, MainProperties properties)
        {
            var currRange = new Range(prevRange.Width, prevRange.Height);
            StructureHandler.GrainsArrayInit(currRange);
            StructureHandler.UpdateInsideCircle(currRange);

            foreach (var p in StructureHandler.PointsArea)
            {
                if (prevRange.GrainsArray[p.X, p.Y].Id != (int)SpecialId.Id.Empty)
                {
                    // change if there is already some color (not white)
                    currRange.GrainsArray[p.X, p.Y].Id = prevRange.GrainsArray[p.X, p.Y].Id;
                    currRange.GrainsArray[p.X, p.Y].Color = prevRange.GrainsArray[p.X, p.Y].Color;
                }
                else
                {
                    if (properties.NeighbourhoodType != NeighbourhoodType.Moore2)
                    {
                        if(properties.NeighbourhoodType == NeighbourhoodType.Moore)
                                NeighborhoodList = TakeMooreNeighbourhood(p.X, p.Y, prevRange.GrainsArray);
                        else
                                NeighborhoodList = TakeNeumannNeighbourhood(p.X, p.Y, prevRange.GrainsArray);

                        var MostOfCells = NeighborhoodList.Where(g => !SpecialId.IsSpecialId(g.Id));
                                                
                        if (MostOfCells.Any())
                        {
                            // assign grain which are the most in the list of neighborhood
                            currRange.GrainsArray[p.X, p.Y] = MostOfCells.GroupBy(g => g.Id).OrderByDescending(g => g.Count())
                                                                .Select(g => g.First()).First();
                        }
                    }
                    else
                    {
                        // MOORE 2
                        IsGrainUpdate = false;

                        // rule 1 - ordinary Moore
                        NeighborhoodList = TakeMooreNeighbourhood(p.X, p.Y, prevRange.GrainsArray);

                        MostOfCells = NeighborhoodList.Where(g => !SpecialId.IsSpecialId(g.Id))
                                                .GroupBy(g => g.Id).OrderByDescending(g => g.Count());

                        if (MostOfCells.Any())
                        {
                            if (MostOfCells.First().Count() >= 5 && MostOfCells.First().Count() <= 8)
                            {
                                currRange.GrainsArray[p.X, p.Y] = MostOfCells.Select(g => g.First()).First();
                            }
                            else
                            {
                                // rule 2 - nearest Moore (Von Neumann)
                                NeighborhoodList = TakeNearestMooreNeighbourhood(p.X, p.Y, prevRange.GrainsArray);

                                MostOfCells = NeighborhoodList.Where(g => !SpecialId.IsSpecialId(g.Id))
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
                                    // rule 3 - further Moore
                                    NeighborhoodList = TakeFurtherMooreNeighbourhood(p.X, p.Y, prevRange.GrainsArray);

                                    MostOfCells = NeighborhoodList.Where(g => !SpecialId.IsSpecialId(g.Id))
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
                                        // rule 4 - ordinary Moore with probability
                                        NeighborhoodList = TakeMooreNeighbourhood(p.X, p.Y, prevRange.GrainsArray);

                                        MostOfCells = NeighborhoodList.Where(g => !SpecialId.IsSpecialId(g.Id))
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
    }
}
