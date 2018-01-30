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
            PointCloud cloud = new PointCloud(@"c:\maps\templePeak2.las");
            const int size = 1600;            
            //const double top = 4630751.00;
            //const double left = 455823.0;
            //const double mapHeight = 1600;
            //const double mapWidth = 1600;
            Map map = cloud.MakeMap(size);
            cloud.ExtractElevationMap(map);            
            Debug.WriteLine("Extracted map");
            cloud.FillMap(map);
            Debug.WriteLine("Filled map");
            CaveFinder finder = new CaveFinder();
            var caves = finder.FindCaves(map);
            Debug.WriteLine($"{JsonConvert.SerializeObject(caves, Formatting.Indented)}");
            var img = new Imaging();
            img.MakeImage(map);
            img.RenderElevationImage( map, true, false);
            img.AddCaves(map, caves, 255, 0, 0);
            img.Save(map, @"c:\maps\caveSpy2.bmp");
        }
    }
}
