using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSpy
{
    class CaveFinder
    {

        public CaveFinder()
        {
        }

        public List<Cave> FindCaves(Map map)
        {
            int[] minCount = new int[map.elevations.Length];
            var results = new List<Cave>();

            // look for caves via looking for low points
            Queue<int> pointsToTry = new Queue<int>(10);

            int i = map.height + 1;
            for (int y = 1; y < map.height - 1; y++)
            {
                for (int x = 1; x < map.width - 1; x++, i++)
                {
                    // look for local minimums
                    bool notFound = true;

                    int minAt = i;
                    int minX = x;
                    int minY = y;
                    int xStart = x;
                    int yStart = y;
                    double min = map.elevations[i];

                    while (notFound)
                    {
                        // check local neighborhood    
                        xStart = minX;
                        yStart = minY;
                        Extensions.NeighborhoodLoop(map.width, map.height, xStart, yStart, (ii, xx, yy) =>
                         {
                             if (map.elevations[ii] < min)
                             {
                                 min = map.elevations[ii];
                                 minAt = ii;
                                 minX = xx;
                                 minY = yy;
                             }
                         });

                        // we found the local min when there is no change in position
                        notFound = minX != xStart || minY != yStart;
                    }
                    minCount[minAt]++;
                }
                i += 2;
            }

            i = 0;
            for (int y = 1; y < map.height - 1; y++)
            {
                for (int x = 1; x < map.width - 1; x++, i++)
                {
                    if (minCount[i] > 3)
                    {
                        var z = map.elevations[i];
                        int floodSize = Flood(map, x, y, 2, 100);
                        
                        if (floodSize > 3 && floodSize < 100)
                        {
                            Cave c = new Cave() { X = x, Y = y, Z = map.elevations[i] };
                            results.Add(c);
                        }
                    }
                }
                i += 2;
            }



            return results;
        }

        private int Flood(Map map, int xStart, int yStart, int floodHeight, int maxSize)
        {
            HashSet<int> flooded = new HashSet<int>();
            int size = 0;

            void FloodInner(int x, int y)
            {
                if (x < 0 || x > map.width || y < 0 || y > map.height)
                    return;

                double floodLevel = map.elevations[x + y * map.width] + floodHeight;

                Extensions.NeighborhoodLoop(map.width, map.height, x, y,
                    (xx, yy, ii) =>
                    {
                        if (size < maxSize && !flooded.Contains(ii) && map.elevations[ii] <= floodLevel)
                        {
                            size++;
                            flooded.Add(ii);
                            FloodInner(xx, yy);
                        }
                    });
            }

            FloodInner(xStart, yStart);

            return size;
        }        
    }

    public class Cave
    {
        public double X;
        public double Y;
        public double Z;
    }

}
