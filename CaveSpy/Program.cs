using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSpy
{
    class Program
    {
        static void Main(string[] args)
        {
            PointCloud cloud = new PointCloud(@"c:\maps\templePeak.las");
            const int size = 1600;
            const double top = 4630751.00;
            const double left = 455823.0;
            const double mapHeight = 1600;
            const double mapWidth = 1600;
            double[] map = cloud.ExtractElevationMap(size, size, false, left, top, left+mapWidth, top+mapHeight);
            Debug.WriteLine("Extracted map");
            cloud.FillMap(map, size, size);
            Debug.WriteLine("Filled map");
            double maxElevation = map.Max();
            double minElevation = map.Where(e => e != 0).Min();
            CaveFinder finder = new CaveFinder();
            var caves = finder.FindCaves(map, size, size);
            Debug.WriteLine($"{JsonConvert.SerializeObject(caves, Formatting.Indented)}");
            var img = new Imaging();
            img.MakeMap( map, size, size, maxElevation, minElevation, true);
            img.AddCaves(caves, 255, 0, 0);
            img.Save(@"c:\maps\caveSpy.bmp");
        }
    }
}
