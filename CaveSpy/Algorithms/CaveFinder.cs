 using Bee.Eee.Utility.Logging;
using Bee.Eee.Utility.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CaveSpy
{
    class CaveFinderAlgorithm
    {
        public static List<Cave> FindCaves(ILogger log, Map map, double floodDepth)
        {
            int[] minCount = new int[map.elevations.Length];
            var results = new List<Cave>();

            // look for caves via looking for low points
            Queue<int> pointsToTry = new Queue<int>(10);

            int threadCount = System.Environment.ProcessorCount * 2;
            using (var s = new Semaphore(threadCount, threadCount))
            {
                int total = map.height * map.width;
                for (int y = 1; y < map.height - 1; y++)
                {
                    s.WaitOne();
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        try
                        {
                            List<Cave> newCaves = new List<Cave>();
                            int yy = (int)o;
                            int ii = yy * map.width + 1;
                            for (int x = 1; x < map.width - 1; x++, ii++)
                            {

                                int size = Flood(log, map, x, yy, floodDepth, 300);
                                if (size > 5)
                                {
                                    Cave c = new Cave() { X = x, Y = yy, Z = map.elevations[ii] };
                                    newCaves.Add(c);
                                }
                            }
                            if (newCaves.Count > 0)
                                lock (results)
                                    results.AddRange(newCaves);
                        }
                        finally
                        {
                            s.Release();
                        }
                    }, y);


                    int i = y * map.height;
                    if (y % 4 == 0)
                        log.Log(Level.Info, $"Flood Procssing {i:0,000}/{total:0,000} {(double)i / (double)total * 100:0.00}% points via flood: cnt {results.Count}");
                }

                // resync all the threads
                for (int j = 0; j < threadCount; j++)
                    s.WaitOne();

            }


            return results;
        }

        private static int Flood(ILogger log, Map map, int xStart, int yStart, double floodHeight, int maxSize)
        {
            HashSet<int> flooded = new HashSet<int>();
            int size = 0;
            bool edge = false;

            double floodLevel = map.elevations[xStart + yStart * map.width] + floodHeight;

            var stack = new Stack<Location>();
            stack.Push(new Location(xStart, yStart));

            while (!edge && size < maxSize && stack.Count > 0)
            {
                var loc = stack.Pop();

                Extensions.NeighborhoodLoop(map.width, map.height, loc.x, loc.y,
                    (ii, xx, yy) =>
                    {
                        if (edge)
                            return;

                        if (xx == 0 || yy == 0 || xx == map.width - 1 || yy == map.height - 1)
                        {
                            edge = true;
                            return;
                        }

                        if (size < maxSize && map.elevations[ii] <= floodLevel && !flooded.Contains(ii))
                        {
                            size++;
                            flooded.Add(ii);
                            stack.Push(new Location(xx, yy));
                        }
                    });
            }

            return (edge || size == maxSize) ? -1 : size;
        }

        private class Location
        {
            public int x;
            public int y;

            public Location(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        public static int[] MapDrainage(ILogger log, Map map, int lookDistance)
        {
            int[] result = new int[map.elevations.Length];
            int threadCount = System.Environment.ProcessorCount * 2;
            int maxIter = (int)(Math.Sqrt(map.height * map.height + map.width * map.width) * 2); // allow to wander from one corner to another
            Stopwatch sw = new Stopwatch();
            sw.Start();
            using (var s = new Semaphore(threadCount, threadCount))
            {
                int total = map.width * map.height;
                for (int yLoop = 0; yLoop < map.height; yLoop++)
                {
                    if (yLoop % 25 == 0)
                        log.Log(Level.Info, $"Drainage Procssing {yLoop:0,000}/{map.height:0,000} {(double)yLoop / (double)map.height * 100:0.00}%");

                    s.WaitOne();
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        try
                        {
                            int y = (int)(o);
                            int ii = y * map.width;

                            for (int x = 0; x < map.width; x++)
                            {
                                int xp = x;
                                int yp = y;
                                int xl, yl;
                                int ip = x + y * map.width;
                                int cnt = 0;
                                do
                                {
                                    xl = xp;
                                    yl = yp;
                                    cnt++;

                                    double min = map.elevations[xp + yp * map.width];
                                    for (int yy = yl - lookDistance, row = (yl - lookDistance) * map.width; yy <= yl + lookDistance; yy++, row += map.width)
                                    {
                                        for (int xx = xl - lookDistance; xx <= xl + lookDistance; xx++)
                                        {
                                            if (xx < 0 || xx >= map.width || yy < 0 || yy >= map.height)
                                                continue;

                                            double v = map.elevations[xx + row];
                                            if (v < min)
                                            {
                                                min = v;
                                                xp = xx;
                                                yp = yy;
                                                ip = xx + row;
                                            }
                                        }
                                    }
                                    // need to draw a line from last point to this
                                    if (lookDistance > 1)
                                    {                                      
                                        PlotLine(result, map.width, xl, yl, xp, yp);
                                    }
                                    else
                                        Interlocked.Increment(ref result[ip]);

                                } while (cnt < maxIter && !(xp == xl && yp == yl)); // keep going until we've found the minimum drainage point
                            }
                        }
                        finally
                        {
                            s.Release();
                        }
                    }, yLoop);
                }

                // resync all the threads
                for (int j = 0; j < threadCount; j++)
                    s.WaitOne();

            }
            sw.Stop();
            log.Log($"Time taken: {sw.ElapsedMilliseconds}ms");
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PlotLine(int[] result, int width, int x1, int y1, int x2, int y2)
        {
            int w = x2 - x1;
            int h = y2 - y1;
            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
            if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
            if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
            if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
            int longest = Math.Abs(w);
            int shortest = Math.Abs(h);
            if (!(longest > shortest))
            {
                longest = Math.Abs(h);
                shortest = Math.Abs(w);
                if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
                dx2 = 0;
            }
            int numerator = longest >> 1;
            for (int i = 0; i <= longest; i++)
            {
                Interlocked.Increment(ref result[x1 + y1 * width]);
                numerator += shortest;
                if (!(numerator < longest))
                {
                    numerator -= longest;
                    x1 += dx1;
                    y1 += dy1;
                }
                else
                {
                    x1 += dx2;
                    y1 += dy2;
                }
            }
        }        
    }

    public class Cave
    {
        public double X;
        public double Y;
        public double Z;
    }

}
