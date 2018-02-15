using Bee.Eee.Utility.Logging;
using Bee.Eee.Utility.Scripting.Lisp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSpy
{
    class ScriptEngine : LispExecuter
    {
        public ScriptEngine(ILogger logger, ICategories categories)
            : base(logger, categories)
        {            
            RegisterCommand("GetArg", Run_GetArg);
            RegisterCommand("Assert", Run_Assert);
            RegisterCommand("GetExtension", Run_GetExtension);
            RegisterCommand("ChangeExtension", Run_ChangeExtension);
            RegisterCommand("FileExists", Run_FileExists);
            RegisterCommand("ReadFile", Run_ReadFile);
            RegisterCommand("SaveToFile", Run_SaveToFile);
            RegisterCommand("MakeMap", Run_MakeMap);
            RegisterCommand("FillHoles", Run_FillHoles);
            RegisterCommand("FindCavesByFlood", Run_FindCavesByFlood);
            RegisterCommand("MapGeometricMeanFilter", MapGeometricMeanFilter);
            RegisterCommand("MakeImage", Run_MakeImage);
            RegisterCommand("DrawElevationColor", Run_DrawElevationColor);
            RegisterCommand("DrawHillsideShade", Run_DrawHillsideShade);
            RegisterCommand("DrawCaves", Run_DrawCaves);
            RegisterCommand("DrawSlopeColor", Run_DrawSlopeColor);
            RegisterCommand("MapDrainage", Run_MapDrainage);
            RegisterCommand("DrawRealColor", Run_DrawRealColor);
            RegisterCommand("DrawClassification", Run_DrawClassification);
            RegisterCommand("GenerateMap", Run_GenerateMap);
            RegisterCommand("DrawIntArray", Run_DrawIntArray);
        }

        private object Run_DrawIntArray(LispRuntimeCommand cmd, LispList list)
        {
            CheckParameterCount(cmd, list, 4);
            int c = 1;
            Image img = Run<Image>(list.items[c++]);
            int[] arr = Run<int[]>(list.items[c++]);
            double spacing = Run<double>(list.items[c++]);
            double opacity = Run<double>(list.items[c++]);
            img.DrawArrayInt(img, arr, spacing, opacity);
            return null;
        }

        private object Run_GenerateMap(LispRuntimeCommand cmd, LispList list)
        {
            MapAlgorithms alg = new MapAlgorithms(Logger);
            int c = 1;
            string type = Run<string>(list.items[c++]);
            int width = Run<int>(list.items[c++]);
            int height = Run<int>(list.items[c++]);
            var map = alg.GenerateMap(type, width, height);

            return map;
        }

        private object MapGeometricMeanFilter(LispRuntimeCommand cmd, LispList list)
        {
            CheckParameterCount(cmd, list, 2);
            int c = 1;
            var map = Run<Map>(list.items[c++]);
            var N = Run<int>(list.items[c++]);
            if (N % 2 != 1)
                throw new ArgumentOutOfRangeException("N must be odd for GeometricMeanFilter");

            var alg = new MapAlgorithms(Logger);
            alg.GeometricMeanFilter(map, N);

            return null;
        }

        private object Run_DrawClassification(LispRuntimeCommand cmd, LispList list)
        {
            CheckParameterCount(cmd, list, 3);
            int c = 1;
            var image = Run<Image>(list.items[c++]);
            var map = Run<Map>(list.items[c++]);
            var classification = Run<int>(list.items[c++]);
            image.DrawClassification(map, classification);
            return null;
        }

        public void RunScript(string path)
        {
            var script = File.ReadAllText(path);
            var program = LispParser.Parse(script);
            Run<object>(program);
        }

        private object Run_GetArg(LispRuntimeCommand cmd, LispList list)
        {
            CheckParameterCount(cmd, list, 1, 2);
            string arg = Run<string>(list.items[1]);
            object defaultValue = null;
            if (list.items.Count > 2)
                defaultValue = Run<object>(list.items[2]);
            else
                defaultValue = string.Empty;

            var args = Environment.GetCommandLineArgs();
            string returnValue = null;
            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (a == arg)
                {
                    if (i + 1 < args.Length)
                        returnValue = args[i + 1];
                    else
                        break;
                }
            }

            if (returnValue == null)
                return defaultValue;

            switch (defaultValue)
            {
                case double d:
                    return double.Parse(returnValue);
                case int i:
                    return int.Parse(returnValue);
                default:
                    return returnValue;
            }            
        }

        private object Run_Assert(LispRuntimeCommand cmd, LispList list)
        {
            CheckParameterCount(cmd, list, 1);
            bool isOkay = Run<bool>(list.items[1]);
            if (isOkay)
                return true;
            else
                throw new Exception($"Failed Assert on {list.line}:{list.position}");
        }

        private object Run_GetExtension(LispRuntimeCommand cmd, LispList list)
        {
            CheckParameterCount(cmd, list, 1);
            string path = Run<string>(list.items[1]);
            return Path.GetExtension(path);
        }

        private object Run_ChangeExtension(LispRuntimeCommand cmd, LispList list)
        {
            CheckParameterCount(cmd, list, 2);
            string path = Run<string>(list.items[1]);
            string newExtension = Run<string>(list.items[2]);
            string newPath = Path.ChangeExtension(path, newExtension);
            return newPath;
        }

        private object Run_FileExists(LispRuntimeCommand cmd, LispList list)
        {
            CheckParameterCount(cmd, list, 1);
            string path = Run<string>(list.items[1]);
            return File.Exists(path);
        }

        private object Run_SaveToFile(LispRuntimeCommand cmd, LispList list)
        {
            object objectToSave = Run<object>(list.items[1]);
            switch (objectToSave)
            {               
                case Map m:
                    {
                        CheckParameterCount(cmd, list, 2);
                        string path = Run<string>(list.items[2]);
                        m.Save(path);
                    }
                    break;
                case Image im:
                    {
                        CheckParameterCount(cmd, list, 2);
                        string path = Run<string>(list.items[2]);
                        im.Save(path);
                    }
                    break;
            }

            return null;
        }

        private object Run_MakeMap(LispRuntimeCommand cmd, LispList list)
        {
            CheckParameterCount(cmd, list, 2);
            PointCloud pc = Run<PointCloud>(list.items[1]);
            int mapWidth = Run<int>(list.items[2]);

            var map = new Map();
            MapAlgorithms alg = new MapAlgorithms(base.Logger);
            alg.ReadCloudIntoMap(map, mapWidth, pc);
            return map;
        }

        private object Run_FillHoles(LispRuntimeCommand cmd, LispList list)
        {
            CheckParameterCount(cmd, list, 1);
            Map map = Run<Map>(list.items[1]);
            MapAlgorithms alg = new MapAlgorithms(base.Logger);
            //alg.LinearFillMap(map);
            alg.EdgeFillMapAlgorithm(map);
            return null;
        }

        private object Run_FindCavesByFlood(LispRuntimeCommand cmd, LispList list)
        {
            CheckParameterCount(cmd, list, 2);
            Map map = Run<Map>(list.items[1]);
            double depth = Run<double>(list.items[2]);
            CaveFinderAlgorithm finder = new CaveFinderAlgorithm();
            var caves = finder.FindCaves(map, depth);
            return caves;
        }

        private object Run_MakeImage(LispRuntimeCommand cmd, LispList list)
        {
            CheckParameterCount(cmd, list, 1);
            Map map = Run<Map>(list.items[1]);
            Image img = new Image(map);
            return img;
        }

        private object Run_DrawElevationColor(LispRuntimeCommand cmd, LispList list)
        {
            int c = 1;
            CheckParameterCount(cmd, list, 4);
            Image img = Run<Image>(list.items[c++]);
            Map map = Run<Map>(list.items[c++]);
            double spacing = Run<double>(list.items[c++]);
            double opacity = Run<double>(list.items[c++]);

            img.DrawElevationColor(img, map, spacing, opacity);


            return null;
        }

        private object Run_DrawHillsideShade(LispRuntimeCommand cmd, LispList list)
        {
            int c = 1;
            CheckParameterCount(cmd, list, 6);
            Image img = Run<Image>(list.items[c++]);
            Map map = Run<Map>(list.items[c++]);
            double heading = Run<double>(list.items[c++]);
            double step = Run<double>(list.items[c++]);
            double intensity = Run<double>(list.items[c++]);
            double opacity = Run<double>(list.items[c++]);

            img.DrawHillshade(map, heading, step, intensity, opacity);

            return null;
        }

        private object Run_DrawCaves(LispRuntimeCommand cmd, LispList list)
        {
            int c = 1;
            CheckParameterCount(cmd, list, 2);
            Image img = Run<Image>(list.items[c++]);
            List<Cave> caves = Run<List<Cave>>(list.items[c++]);

            img.DrawCaves(caves, 1.0);

            return null;
        }

        private object Run_DrawSlopeColor(LispRuntimeCommand cmd, LispList list)
        {
            return null;
        }

        private object Run_MapDrainage(LispRuntimeCommand cmd, LispList list)
        {
            CheckParameterCount(cmd, list, 1);
            int c = 1;
            Map map = Run<Map>(list.items[c++]);
            CaveFinderAlgorithm alg = new CaveFinderAlgorithm();
            
            return alg.MapDrainage(map);
        }

        private object Run_DrawRealColor(LispRuntimeCommand cmd, LispList list)
        {
            return null;
        }

        private object Run_ReadFile(LispRuntimeCommand cmd, LispList list)
        {
            CheckParameterCount(cmd, list, 1,2);
            var path = Run<string>(list.items[1]);

            string ext = Path.GetExtension(path);
            switch (ext)
            {
                case ".las":
                    {
                        string defaultZone = Run<string>(list.items[2]);
                        var pointCloud = new PointCloud(base.Logger);
                        pointCloud.DefaultZone = defaultZone;
                        pointCloud.LoadFromLas(path);
                        return pointCloud;
                    }
                case ".cloud":
                    {
                        var pc = PointCloud.Load(path);
                        return pc;
                    }

                case ".map":
                    {
                        var map = Map.Load(path, base.Logger);
                        return map;
                    }
                default:
                    throw new InvalidOperationException("Can only support reading .las and .map files.");
            }
        }
    }
}
