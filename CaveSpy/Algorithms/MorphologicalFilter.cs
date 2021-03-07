using Bee.Eee.Utility.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSpy
{
  class MorphologicalFilter
  {
    public static Map Filter(ILogger log, Map map)
    {
      SortedSet<double> sortedResiduals = new SortedSet<double>();
      double[] lastElevations = map.elevations;
      int removalCount = 1;
      int iterationCount = 0;

      // calculate threshold
      for (int row = 0; row < map.height; row++)
      {
        int rowIndex = row * map.width;
        for (int column = 1; column < map.width - 1; column++)
        {
          int index = rowIndex + column;
          double my = (lastElevations[index - 1] + lastElevations[index + 1]) / 2;
          double residual = Math.Abs(map.elevations[index] - my);
          sortedResiduals.Add(residual);
        }
      }

      double threashold = sortedResiduals.Skip((int)(sortedResiduals.Count * 0.50) - 1).First();
      Console.WriteLine($"Threshold: {threashold}");

      while (removalCount > 0 && iterationCount < 200)
      {
        double[] nextElevations = new double[map.elevations.Length];
        removalCount = 0;
        iterationCount++;
        // calculate residuals        
        for (int row = 0; row < map.height; row++)
        {
          int rowIndex = row * map.width;
          for (int column = 1; column < map.width - 1; column++)
          {
            int index = rowIndex + column;
            double elevation = lastElevations[index];
            Func<int, int, double, (int, double)> findNext = (start, end, defaultValue) =>
               {
                 int direction = (start < end) ? 1 : -1;
                 int count = Math.Abs(start - end);
                 for (int ii = 0, z = start; ii < count; ii++, z += direction)
                 {
                   if (lastElevations[z] > 0)
                   {
                     return (ii + 1, lastElevations[z]);
                   }
                 }
                 return (1, defaultValue);
               };

            if (elevation > 0)
            {
              (int spanBefore, double elevationBefore) = findNext(index - 1, rowIndex, elevation);
              (int spanAfter, double elevationAfter) = findNext(index + 1, rowIndex + map.width - 1, elevation);

              double m = (elevationBefore - elevationAfter) / ((index - spanBefore) - (index + spanAfter));              
              double y = spanBefore * m + elevationBefore;
              double residual = Math.Abs(elevation - y);
              if (residual > threashold)
              {
                nextElevations[index] = Math.Min(elevationBefore,elevationAfter);
                removalCount++;
              }
              else
              {
                nextElevations[index] = elevation;
              }
            }
          }
        }

        lastElevations = nextElevations;
        Console.WriteLine($"#{iterationCount} Removal: {removalCount}");
      }

      Console.WriteLine($"Finished MorphologicalFilter {iterationCount} iterations");

      Map filteredMap = map.Clone();
      filteredMap.elevations = lastElevations;
      return filteredMap;
    }
  }
}
