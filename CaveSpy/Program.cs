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
            string input = "";
            string output = "";
            int size = 0;
            double floodDepth = 1;
            bool useBounds = false;
            double top = 0;
            double left = 0;
            double widthMeters = 0;
            double heightMeters = 0;
            bool lookForCaves = false;

            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                switch (a)
                {
                    case "--input": input = args[++i]; break;
                    case "--output": output = args[++i]; break;
                    case "--flood": floodDepth = double.Parse(args[++i]); break;
                    case "--top": useBounds = true; top = double.Parse(args[++i]); break;
                    case "--left": useBounds = true; left = double.Parse(args[++i]); break;
                    case "--width": useBounds = true; widthMeters = double.Parse(args[++i]); break;
                    case "--height": useBounds = true; heightMeters = double.Parse(args[++i]); break;
                    case "--image-size": size = int.Parse(args[++i]); break;
                    case "--look-for-caves": lookForCaves = true; break;
                }
            }

            if (size <= 0 || string.IsNullOrEmpty(input) || string.IsNullOrEmpty(output))
            {
                PrintError("Invalid or missing parameters.");
                return;
            }

            PointCloud cloud = new PointCloud(input);
            Map map = null;
            if (useBounds)
                map = cloud.MakeMap(size, left, top, widthMeters, heightMeters);
            else
                map = cloud.MakeMap(size);

            Console.WriteLine($"{map.width}x{map.height}");
            cloud.ExtractElevationMap(map);
            Debug.WriteLine("Extracted map");
            cloud.FillMap(map);
            Debug.WriteLine("Filled map");
            var img = new Imaging();
            img.MakeImage(map);
            img.RenderElevationImage(map, true, false);
            if (lookForCaves)
            {
                CaveFinder finder = new CaveFinder();
                var caves = finder.FindCaves(map, floodDepth);
                img.AddCaves(map, caves, 255, 0, 0);
            }
            img.Save(map, output);
        }

        private static void PrintError(string errorMessage)
        {
            Console.WriteLine($"Error: {errorMessage}");
            Console.WriteLine("CaveSpy.exe --input <input .las file> -output <output.bmp> --image-size <size in pixels> [--flood <flood depth in meters>] [--top <GPS coord in UTM> --left <GPS coord in UTM> --width <meters> --height <meters>] [--look-for-caves]");
            Console.WriteLine("--input - the file name of the .las file that will be read in");
            Console.WriteLine("--output - the name of the bitmap that will be written to disk");
            Console.WriteLine("--flood - the depth in meters that the finding algorithm will use to find pits");
            Console.WriteLine("--top -- the northing UTM coordinate of the corner of the map (south,west corner)");
            Console.WriteLine("--left -- the easting UTM coordinate of the corner of the map (south,west corner)");
            Console.WriteLine("--width -- the distance in meters the area will stretch toward the east");
            Console.WriteLine("--height -- the distance in meters the area will stretch toward the north");
            Console.WriteLine("--image-size -- the number of pixels wide the image will be.  The height will be chosen to preserve the aspect ratio.");
            Console.WriteLine("--look-for-caves -- if present then it will apptempt too look for caves using the flood method");
        }
    }
}
