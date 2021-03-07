using Bee.Eee.Utility.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSpy
{
  static class HoleDetectFilters
  {
    public static Map CorrelationRun(ILogger log, Map map)
    {      
      double[] result = new double[map.elevations.Length];

      for (int j = pitFilter.Length/2; j < map.height - pitFilter.Length/2; j++)
      {
        int row = j * map.width;
        for (int i = pitFilter.Length/2; i < map.width - pitFilter.Length/2; i++)
        {
          int x = i + row;          
          result[x] = HorizontalCorrelation(log, map, x) * VerticalCorrelation(log, map, x);
        }
      }

      Map newMap = map.Clone();
      newMap.elevations = result;
      return newMap;
    }

    private static double[] pitFilter = new double[] { 2, 0.33, 0.1, -5, -0.1, -0.33, -2};
    private static double HorizontalCorrelation(ILogger log, Map map, int centerIndex)
    {
      double value = 0;
      int mapIndex = centerIndex - pitFilter.Length / 2;
      for (int i = 0; i < pitFilter.Length; i++, mapIndex++)
      {
        value += pitFilter[i] * (map.elevations[mapIndex] - map.physicalLow) / map.physicalHeight;
      }
      return value;
    }

    private static double VerticalCorrelation(ILogger log, Map map, int centerIndex)
    {
      double value = 0;
      int mapIndex = centerIndex - pitFilter.Length / 2 * map.width;
      for (int i = 0; i < pitFilter.Length; i++, mapIndex+=map.width)
      {
        value += pitFilter[i] * (map.elevations[mapIndex] - map.physicalLow) / map.physicalHeight;
      }
      return value;
    }

    public static double[] LevelDetect(ILogger log, Map map, bool count = false)
    {
      double[] nextElevations = new double[map.elevations.Length];
      const int maxDiameter = 60;
      const int radius = maxDiameter / 2;
      const int radiusSquared = radius * radius;

      int[] indexOffsets =
        Enumerable.Range(0, maxDiameter * maxDiameter)
        .Select(i => new { x = i % maxDiameter - radius, y = i / maxDiameter - radius })
        .Where(p => p.x * p.x + p.y * p.y <= radiusSquared && !(p.x == 0 && p.y == 0))
        .Select(p => p.x + p.y * map.width)
        .ToArray();

      Func<int, double> getPitMatch = (index) =>
       {
         double value = 0;
         double elevation = map.elevations[index];
         for (int i = 0; i < indexOffsets.Length; i++)
         {
           int localIndex = indexOffsets[i] + index;
           double diff = map.elevations[localIndex] - elevation;
           double inc = (count) ? 1 : diff;
           value += (diff > 0) ? inc : 0;
         }
         return value;
       };

      int tenth = map.height / 10;
      int per = 0;
      for (int j = radius; j < map.height - radius; j++)
      {
        if ( j % tenth == 0)
        {
          per+=10;
          log.Log($"LevelDetect: {per}% done");
        }
        int row = j * map.width;
        for (int i = radius; i < map.width - radius; i++)
        {
          int index = i + row;
          nextElevations[index] = getPitMatch(index);
        }
      }
      
      return nextElevations;      
    }
  }
}
