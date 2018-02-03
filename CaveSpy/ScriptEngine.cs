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
    class ScriptEngine
    {
        private ILogger _logger;
        LispExecuter _executer;
        public ScriptEngine(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _executer = new LispExecuter(_logger);
            _executer.RegisterCommand("FilterOutClassification", Run_FilterOutClassification);
            _executer.RegisterCommand("FillHoles", Run_FillHoles);
            _executer.RegisterCommand("FindCaveByFlood", Run_FindCaveByFlood);
            _executer.RegisterCommand("ElevationShade", Run_ElevationShade);
            _executer.RegisterCommand("HillsideShade", Run_HillsideShade);
            _executer.RegisterCommand("SlopeColor", Run_SlopeColor);
            _executer.RegisterCommand("DrainageColor", Run_DrainageColor);
        }

        public void RunScript(string path)
        {
            var script = File.ReadAllText(path);
            var program = LispParser.Parse(script);
            _executer.RunList(program);
        }

        private object Run_FillHoles(LispRuntimeCommand cmd, LispList list)
        {
            Console.WriteLine($"Running {new StackFrame(1, true).GetMethod().Name}");
            return null;
        }

        private object Run_FilterOutClassification(LispRuntimeCommand cmd, LispList list)
        {
            Console.WriteLine($"Running {new StackFrame(1, true).GetMethod().Name}");
            return null;
        }

        private object Run_DrainageColor(LispRuntimeCommand cmd, LispList list)
        {
            Console.WriteLine($"Running {new StackFrame(1, true).GetMethod().Name}");
            return null;
        }

        private object Run_SlopeColor(LispRuntimeCommand cmd, LispList list)
        {
            Console.WriteLine($"Running {new StackFrame(1, true).GetMethod().Name}");
            return null;
        }

        private object Run_HillsideShade(LispRuntimeCommand cmd, LispList list)
        {
            Console.WriteLine($"Running {new StackFrame(1, true).GetMethod().Name}");
            return null;
        }

        private object Run_ElevationShade(LispRuntimeCommand cmd, LispList list)
        {
            Console.WriteLine($"Running {new StackFrame(1, true).GetMethod().Name}");
            return null;
        }

        private object Run_FindCaveByFlood(LispRuntimeCommand cmd, LispList list)
        {
            Console.WriteLine($"Running {new StackFrame(1, true).GetMethod().Name}");
            return null;
        }
    }
}
