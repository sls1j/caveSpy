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

        public List<Cave> FindCaves(double[] map, int width, int height)
        {
            int[] minCount = new int[map.Length];
            var results = new List<Cave>();

            // look for caves via looking for low points
            Queue<int> pointsToTry = new Queue<int>(10);

            int i = height + 1;
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width; x++, i++)
                {
                    // look for local minimums
                    bool notFound = true;

                    int minAt = i;
                    int minX = x;
                    int minY = y;
                    int xStart = x;
                    int yStart = y;
                    double min = map[i];

                    while (notFound)
                    {
                        // check local neighborhood    
                        xStart = minX;
                        yStart = minY;
                        Extensions.NeighborhoodLoop(width, height, xStart, yStart, (ii, xx, yy) =>
                         {
                             if (map[ii] < min)
                             {
                                 min = map[ii];
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

            i = height + 1;
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width; x++, i++)
                {
                    if (minCount[i] > 8)
                    {
                        Cave c = new Cave() { X = x, Y = y, Z = map[i] };
                        results.Add(c);
                    }
                }
                i += 2;
            }

            return results;
        }

        
    }

    public class Cave
    {
        public double X;
        public double Y;
        public double Z;
    }

}
