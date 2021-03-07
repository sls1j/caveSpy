﻿using Bee.Eee.Utility.Logging;
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
      string scriptPath = "default.lisp";
      bool verbose = false;
      for (int i = 0; i < args.Length; i++)
      {
        string arg = args[i];
        switch (arg)
        {
          case "--script":
            scriptPath = args[++i];
            break;
          case "--verbose":
            ;
            verbose = true;
            break;
          case "--version":
            Console.WriteLine($"CaveSpy.exe {Environment.Version.ToString()}");
            return;
        }
      }

      DateTime startTime = DateTime.UtcNow;
      Categories cat = new Categories();

      ILogger logger;
      //if (Debugger.IsAttached)
      //    logger = new TraceLogger("Main");
      //else
      logger = new ConsoleLogger("Main");
      ScriptEngine se = new ScriptEngine(logger, cat);
      if (verbose)
      {
        logger.EnableCategory(cat.ScriptLogging);
      }

      se.RunScript(scriptPath);


      Console.WriteLine($"Processing time: {DateTime.UtcNow - startTime}");
    }

    private static void PrintError(string errorMessage)
    {
      Console.WriteLine($"Error: {errorMessage}");
      Console.WriteLine("CaveSpy.exe [--script <script path>] [--verbose] [ script arguments ]");
      Console.WriteLine("  --script <path> -- the path of the script to execute.  The default value is 'default.lisp'");
      Console.WriteLine("  --verbose -- increase the amount of information printed on the console.");
      Console.WriteLine("  --version -- prints out the version of CaveSpy.exe");
    }
  }
}
