using Bee.Eee.Utility.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CaveSpy
{
  class MapAlgorithms
  {
    private ILogger _logger;

    public MapAlgorithms(ILogger logger)
    {
      _logger = (logger != null) ? logger.CreateSub("MapAlgorithms") : throw new ArgumentNullException("logger");
    }
    public Map ReadCloudIntoMap(int mapWidth, PointCloud cloud, HashSet<int> includedClassifications)
    {
      Map map = new Map();

      var header = cloud.Header;

      map.physicalLeft = header.MinX;
      map.physicalTop = header.MinY;
      map.physicalRight = header.MaxX;
      map.physicalBottom = header.MaxY;
      map.physicalWidth = header.MaxX - header.MinX;
      map.physicalHeight = header.MaxY - header.MinY;
      map.physicalHigh = cloud.Header.MaxZ;
      map.physicalLow = cloud.Header.MinZ;
      map.zone = cloud.Zone;

      map.width = mapWidth;
      map.height = (int)(int)(mapWidth / map.physicalWidth * map.physicalHeight);

      double xScale = (double)(map.width - 1) / map.physicalWidth;
      double yScale = (double)(map.height - 1) / map.physicalHeight;
      double minX = map.physicalLeft;
      double minY = map.physicalTop;

      int size = map.width * map.height;
      map.elevations = new double[size];
      map.classifications = new byte[size];
      map.colors = new Color[size];
      var contributers = new int[size];
      bool filterByClass = includedClassifications.Count > 0;
      HashSet<int> classifications = new HashSet<int>();
      List<PointData>[] pointCloud = Enumerable.Range(0, size).Select(i => new List<PointData>()).ToArray();

      cloud.HandlePoints((i, p) =>
      {
        if (i % 1000000 == 0)
          _logger.Log(Level.Debug, $"{i:0,000}/{header.NumberOfPointRecords:0,000} {(double)i / (double)header.NumberOfPointRecords * 100: 0.00}%");

        var c = p.Classification & 0xF;
        if (filterByClass && !includedClassifications.Contains(c))
          return;

        classifications.Add(p.Classification & 0x0F);

        double x = p.X * header.XScaleFactor + header.XOffset;
        double y = p.Y * header.YScaleFactor + header.YOffset;
        double z = p.Z * header.ZScaleFactor + header.ZOffset;

        // skip if not in area of interest
        if (x < map.physicalLeft || x > map.physicalRight || y < map.physicalTop || y > map.physicalBottom)
          return;



        int xi = (int)((x - minX) * xScale);
        int yi = map.height - (int)((y - minY) * yScale) - 1;
        int ii = yi * map.width + xi;

        pointCloud[ii].Add(p);
        if (map.elevations[ii] == 0.0)
          map.elevations[ii] = z;
        else
          map.elevations[ii] = Math.Min(map.elevations[ii], z);

        contributers[ii] = 1;
        //map.elevations[ii] += z;
        //contributers[ii]++;
        map.classifications[ii] = p.Classification;
        if (header.PointDataFormat == 2 || header.PointDataFormat == 3)
          map.colors[ii] = new Color(p.Red, p.Green, p.Blue);
      });

      for (int i = 0; i < map.elevations.Length; i++)
      {
        int c = contributers[i];
        if (c > 1)
          map.elevations[i] /= contributers[i];
      }

      _logger.Log($"Classifications present: { string.Join(",", classifications.OrderBy(c => c))}");

      return map;
    }

    public Map TrimClassifications(Map oldMap, HashSet<int> includedClassifications)
    {
      Map map = oldMap.Clone();
      map.elevations = new double[oldMap.elevations.Length];
      map.colors = new Color[oldMap.colors.Length];
      map.classifications = new byte[oldMap.classifications.Length];
      var c = oldMap.classifications;
      for (int i = 0; i < c.Length; i++)
      {
        if (!includedClassifications.Contains((int)c[i] & 0x0F))
        {
          map.classifications[i] = 0;
          map.elevations[i] = 0;
          map.colors[i] = null;
        }
        else
        {
          map.classifications[i] = oldMap.classifications[i];
          map.elevations[i] = oldMap.elevations[i];
          map.colors[i] = oldMap.colors[i];
        }
      }

      return map;
    }

    public Map FillHole(Map oldMap)
    {
      int length = oldMap.elevations.Length;
      double[] lastElevations = oldMap.elevations;
      byte[] lastClassifications = oldMap.classifications;

      int filledHoleCount = 1;
      int iterationCount = 0;
      int w = oldMap.width;
      int h = oldMap.height;      

      while (filledHoleCount > 0)
      {
        iterationCount++;
        filledHoleCount = 0;

        double[] nextElevations = new double[length];
        byte[] nextClassifications = new byte[length];
        
        for (int y = 1; y < h - 1; y++)
        {
          int rowIndex = y * w;
          for (int x = 1; x < w - 1; x++)
          {
            int index = rowIndex + x;
            if ( lastElevations[index] == 0 )
            {
              double totalElevation = 0;
              int totalElevationCount = 0;

              Action<int,int> updateTotalElevation = (xo,yo) =>
              {
                int nIndex = index + xo + (yo * w);

                if (lastElevations[nIndex] > 0)
                {
                  totalElevation += lastElevations[nIndex];
                  totalElevationCount++;
                }
              };

              updateTotalElevation(-1, -1);
              updateTotalElevation(0, -1);
              updateTotalElevation(1, -1);

              updateTotalElevation(-1, 0);
              updateTotalElevation(1, 0);

              updateTotalElevation(-1, 1);
              updateTotalElevation(0, 1);
              updateTotalElevation(-1, 1);

              if ( totalElevationCount > 0)
              {
                filledHoleCount++;
                nextElevations[index] = totalElevation / totalElevationCount;
                nextClassifications[index] = 2; // it's now ground
              }             
            }
            else
            {
              nextElevations[index] = lastElevations[index];
              nextClassifications[index] = lastClassifications[index];
            }
          }
        }

        lastElevations = nextElevations;
        lastClassifications = nextClassifications;

        _logger.Log($"#{iterationCount} Filled {filledHoleCount} holes");
      }

      Map map = oldMap.Clone();
      map.elevations = lastElevations;
      map.classifications = lastClassifications;

      return map;
    }

    public void EdgeFillMapAlgorithm(Map map)
    {
      // find all the edges in current map
      var e = map.elevations;
      var c = map.classifications;
      var origEdges = new List<Grower>();

      for (int y = 1; y < map.height - 1; y++)
      {
        for (int x = 1; x < map.width - 1; x++)
        {
          int i = x + y * map.width;
          bool isEdge = (c[i] != 0 && (
              c[i - map.width - 1] == 0 || c[i - map.width] == 0 || c[i - map.width + 1] == 0 ||
              c[i - 1] == 0 || c[i + 1] == 0 ||
              c[i + map.width - 1] == 0 || c[i + map.width] == 0 || c[i + map.width + 1] == 0));

          if (isEdge)
            origEdges.Add(new Grower() { x = x, y = y, i = i });
        }
      }

      // start filling the holes via copying self into holes until there are no more edges left
      int iterCount = 0;
      while (origEdges.Count > 0)
      {
        Dictionary<int, Grower> newEdges = new Dictionary<int, Grower>();

        // create new edges
        int nextRow = map.width - 2;
        for (int i = 0; i < origEdges.Count; i++)
        {
          var g = origEdges[i];
          var gv = e[g.i];
          for (int y = g.y - 1; y <= g.y + 1; y++)
          {
            for (int x = g.x - 1; x <= g.x + 1; x++)
            {
              int ii = y * map.width + x;

              if ((x != g.x || y != g.y) && (x >= 0 && x < map.width && y >= 0 && y < map.height) && c[ii] == 0)
              {
                Grower ng = null;
                if (newEdges.TryGetValue(ii, out ng))
                {
                  ng.value += gv;
                  ng.contributerCount++;
                }
                else
                {
                  ng = new Grower() { x = x, y = y, i = ii, value = gv, contributerCount = 1 };
                  newEdges.Add(ii, ng);
                }
              }
            }
          }
        }

        // fill in the calculated values
        origEdges = newEdges.Values.ToList();
        newEdges.Clear();

        for (int i = 0; i < origEdges.Count; i++)
        {
          var edge = origEdges[i];
          e[edge.i] = edge.value / edge.contributerCount;
          c[edge.i] = 13; // custom to show that we filled the edge
        }

        iterCount++;
        if (iterCount % 10 == 0)
          _logger.Log(Level.Debug, $"Fill iteration {iterCount} edge count {origEdges.Count}");
      }
    }

    class Grower
    {
      public int x;
      public int y;
      public int i;
      public double value;
      public int contributerCount;
    }

    public Map GeometricMeanFilter(Map oldMap, int N)
    {
      double[] newElevations = new double[oldMap.width * oldMap.height];
      int N2 = N / 2;
      for (int y = 0; y < oldMap.height; y++)
      {
        for (int x = 0; x < oldMap.width; x++)
        {
          double Z = 1.0;
          int cnt = 0;
          for (int i = -N2; i <= N2; i++)
          {
            for (int j = -N2; j <= N2; j++)
            {
              int xx = i + x;
              int yy = j + y;
              if (xx < 0 || xx >= oldMap.width || yy < 0 || yy >= oldMap.height)
                continue;

              cnt++;
              Z *= oldMap.elevations[xx + yy * oldMap.width] + 1;
            }
          }

          newElevations[x + y * oldMap.width] = Math.Pow(Z, 1.0 / (double)cnt) - 1;
        }
      }

      Map map = oldMap.Clone();
      map.elevations = newElevations;
      return map;
    }

    public int[] CalculateSlopeAngle(Map map)
    {
      int[] angles = new int[map.height * map.width];
      for (int y = 0, i = 0; y < map.height; y++)
      {
        for (int x = 0; x < map.width; x++, i++)
        {
          double avgDiffX = 0;
          double avgDiffY = 0;
          int avgCnt = 0;
          double e = map.elevations[i];
          for (int yy = y - 1; yy < y + 1; yy++)
          {
            for (int xx = x - 1; xx <= x + 1; xx++)
            {
              if ((x == xx && y == yy) || yy < 0 || yy >= map.height || xx < 0 || xx >= map.width)
                continue;

              int ii = xx + yy * map.width;
              double diff = (map.elevations[ii] - e) / (Math.Sqrt((xx - x) * (xx - x) + (yy - y) * (yy - y)));
              avgDiffX += diff * (xx - x);
              avgDiffY += diff * (yy - y);
              avgCnt++;
            }
          }
          avgDiffX /= avgCnt;
          avgDiffY /= avgCnt;
          double avgDiff = Math.Sqrt(avgDiffX * avgDiffX + avgDiffY * avgDiffY);
          double angle = Math.Atan(avgDiff) * 180 / Math.PI;
          angles[i] = (int)(angle);
        }
      }
      return angles;
    }

    public Map GenerateMap(string type, int width, int height)
    {
      switch (type)
      {
        case "fill-test":
          return FillTestMap(width, height);
        default:
          return null;
      }
    }

    private Map FillTestMap(int width, int height)
    {
      Map map = new Map();
      map.physicalBottom = 100;
      map.physicalTop = 0;
      map.physicalLeft = 0;
      map.physicalRight = 100;
      map.physicalWidth = 100;
      map.physicalHeight = 100;
      map.width = width;
      map.height = height;
      map.zone = "12T";
      map.elevations = new double[width * height];
      map.classifications = new byte[width * height];

      for (int i = 0; i < map.elevations.Length; i++)
      {
        map.elevations[i] = 100;
        map.classifications[i] = (byte)1;
      }

      // put in some random holes
      Random r = new Random();
      HashSet<int> used = new HashSet<int>();
      for (int i = 0; i < 100; i++)
      {
        // get 
        int ii;
        while (used.Contains(ii = (int)(r.NextDouble() * map.elevations.Length))) ;
        used.Add(ii);
        map.elevations[ii] = 0;
        map.classifications[ii] = 0;
      }

      // put in a few large holes
      for (int i = 0; i < 10; i++)
      {
        int x1 = r.Next(map.width);
        int y1 = r.Next(map.height);
        int rad = r.Next(100) + 10;
        CutCircle(map, x1, y1, rad);
      }

      return map;
    }

    private void CutCircle(Map map, int x1, int y1, int rad)
    {
      int rad2 = rad * rad;
      int x2 = x1 + rad * 2;
      int y2 = y1 + rad * 2;

      int xc = (x1 + x2) / 2;
      int yc = (y1 + y2) / 2;

      for (int yy = y1; yy <= y2; yy++)
      {
        for (int xx = x1; xx <= x2; xx++)
        {
          if (xx < 0 || xx >= map.width || yy < 0 || yy >= map.height)
            continue;

          if ((xx - xc) * (xx - xc) + (yy - yc) * (yy - yc) < rad2)
          {
            int ii = xx + yy * map.width;
            map.elevations[ii] = 0;
            map.classifications[ii] = 0;
          }
        }
      }
    }
  }
}
