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

        public List<Cave> FindCaves(Map map, double floodDepth)
        {
            int[] minCount = new int[map.elevations.Length];
            var results = new List<Cave>();

            // look for caves via looking for low points
            Queue<int> pointsToTry = new Queue<int>(10);

            int i = map.height + 1;
            int total = map.height * map.width;
            for (int y = 1; y < map.height - 1; y++)
            {
                for (int x = 1; x < map.width - 1; x++, i++)
                {
                    if (i % 10000 == 0)
                        Console.WriteLine($"Procssing {i}/{total} points via flood: cnt {results.Count}");
                    int size = Flood(map, x, y, floodDepth, 300);
                    if (size > 5)
                    {
                        Cave c = new Cave() { X = x, Y = y, Z = map.elevations[i] };
                        results.Add(c);
                    }
                }
                i += 2;
            }


            return results;
        }

        private int Flood(Map map, int xStart, int yStart, double floodHeight, int maxSize)
        {
            HashSet<int> flooded = new HashSet<int>();
            int size = 0;
            bool edge = false;

            double floodLevel = map.elevations[xStart + yStart * map.width] + floodHeight;

            void FloodInner(int x, int y)
            {                
                if (edge)
                    return;                

                Extensions.NeighborhoodLoop(map.width, map.height, x, y,
                    (ii, xx, yy) =>
                    {
                        if (edge)
                            return;

                        if (xx == 0 || yy == 0 || xx == map.width - 1 || yy == map.height - 1)
                        {
                            edge = true;
                            return;
                        }
                       
                        if (size < maxSize && !flooded.Contains(ii) && map.elevations[ii] <= floodLevel)
                        {
                            size++;
                            flooded.Add(ii);
                            FloodInner(xx, yy);
                        }
                    });
            }

            FloodInner(xStart, yStart);

            return (edge || size == maxSize ) ? -1 : size;
        }        
    }

    public class Cave
    {
        public double X;
        public double Y;
        public double Z;
    }

}
