using Bee.Eee.Utility.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSpy
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime startTime = DateTime.UtcNow;
            ILogger logger = new ConsoleLogger("Main");
            ScriptEngine se = new ScriptEngine(logger);
            se.RunScript("default.lisp");


            Console.WriteLine($"Processing time: {DateTime.UtcNow - startTime}");
            if (Debugger.IsAttached)
                Console.ReadKey();
        }

        //private static void PrintError(string errorMessage)
        //{
        //    Console.WriteLine($"Error: {errorMessage}");
        //    Console.WriteLine("CaveSpy.exe --input <input .las file> --output <output.bmp> --image-size <size in pixels> [--top <GPS coord in UTM> --left <GPS coord in UTM> --width <meters> --height <meters>] [--look-for-caves] [--flood <flood depth in meters>]");
        //    Console.WriteLine("--input - the file name of the .las or a .map file that will be read in");
        //    Console.WriteLine("--output - the name of the output that will be written to disk.  Supports format .bmp, .kml, and .map (a intermediate format for CaveSpy)");
        //    Console.WriteLine("--image-size -- the number of pixels wide the image will be.  The height will be chosen to preserve the aspect ratio.");
        //    Console.WriteLine("--top -- the northing UTM coordinate of the corner of the map (south,west corner)");
        //    Console.WriteLine("--left -- the easting UTM coordinate of the corner of the map (south,west corner)");
        //    Console.WriteLine("--width -- the distance in meters the area will stretch toward the east");
        //    Console.WriteLine("--height -- the distance in meters the area will stretch toward the north");
        //    Console.WriteLine("--flood - the depth in meters that the finding algorithm will use to find pits");
        //    Console.WriteLine("--look-for-caves -- if present then it will apptempt too look for caves using the flood method");
        //    Console.WriteLine("--false-elevation-coloring -- if present then coloring representing elevation will be added.");
        // }
    }    
}
