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
        public ScriptEngine(ILogger logger)
            : base(logger)
        {
            logger.SetEnabledCategories(new int[] { Categories.ScriptLogging });

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
            RegisterCommand("MakeImage", Run_MakeImage);
            RegisterCommand("DrawElevationColor", Run_DrawElevationColor);
            RegisterCommand("DrawHillsideShade", Run_DrawHillsideShade);
            RegisterCommand("DrawCaves", Run_DrawCaves);
            RegisterCommand("DrawSlopeColor", Run_DrawSlopeColor);
            RegisterCommand("DrawDrainageIntensity", Run_DrawDrainageIntensity);
            RegisterCommand("DrawReadColor", Run_DrawRealColor);

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
                case PointCloud pc:
                    {
                        CheckParameterCount(cmd, list, 2);
                        string path = Run<string>(list.items[2]);
                        pc.Save(path);
                    }
                    break;
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
            CheckParameterCount(cmd, list, 4);
            PointCloud pc = Run<PointCloud>(list.items[1]);
            int mapWidth = Run<int>(list.items[2]);
            string includedValues = Run<string>(list.items[3]);
            string includedClassifications = Run<string>(list.items[4]);

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
            alg.LinearFillMap(map);
            return null;
        }

        private object Run_FindCavesByFlood(LispRuntimeCommand cmd, LispList list)
        {
            CheckParameterCount(cmd, list, 2);
            Map map = Run<Map>(list.items[1]);
            double depth = Run<double>(list.items[2]);
            CaveFinder finder = new CaveFinder();
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
            int c = 0;
            CheckParameterCount(cmd, list, 3);
            Image img = Run<Image>(list.items[c++]);
            Map map = Run<Map>(list.items[c++]);



            return null;
        }

        private object Run_DrawHillsideShade(LispRuntimeCommand cmd, LispList list)
        {
            int c = 1;
            CheckParameterCount(cmd, list, 5);
            Image img = Run<Image>(list.items[c++]);
            Map map = Run<Map>(list.items[c++]);
            double heading = Run<double>(list.items[c++]);
            double step = Run<double>(list.items[c++]);
            double intensity = Run<double>(list.items[c++]);

            img.DrawHillshade(map, heading, step, intensity);

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

        private object Run_DrawDrainageIntensity(LispRuntimeCommand cmd, LispList list)
        {
            return null;
        }

        private object Run_DrawRealColor(LispRuntimeCommand cmd, LispList list)
        {
            return null;
        }

        private object Run_ReadFile(LispRuntimeCommand cmd, LispList list)
        {
            CheckParameterCount(cmd, list, 1);
            var path = Run<string>(list.items[1]);

            string ext = Path.GetExtension(path);
            switch (ext)
            {
                case ".las":
                    {
                        var pointCloud = new PointCloud(base.Logger);
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
