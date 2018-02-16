using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bee.Eee.Utility.Logging
{
    /// <summary>
    /// Dumps logs to the console.
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        private string _process;
        private string _subProcess;
        private ICategoryManager _categoryManager;

        public ConsoleLogger(string process, object categories = null)
            :this(new CategoryManager(), process, "Main", categories)
        {
        }

        public ConsoleLogger(ICategoryManager categoryManager, string process, string subProcess = null, object categories = null)
        {
            if (null == categoryManager)
                throw new ArgumentNullException("categoryManager");

            _categoryManager = categoryManager;

            if (string.IsNullOrWhiteSpace(process))
                throw new ArgumentNullException("process");

            _process = process;

            if (string.IsNullOrWhiteSpace(subProcess))
                throw new ArgumentNullException("subProcess");

            _subProcess = subProcess;

            if (null != categories)
                _categoryManager.RegisterCategories(categories);
        }

        public ILogger CreateSub(string subProcessName)
        {
            return new ConsoleLogger(_categoryManager, _process, subProcessName);
        }

        public void DisableCategroy(int categoryId)
        {
            _categoryManager.DisableCategroy(categoryId);
        }

        public void EnableCategory(int categoryId)
        {
            _categoryManager.EnableCategory(categoryId);
        }       

        public bool IsCategoryEnabled(int categoryId)
        {
            return _categoryManager.IsCategoryEnabled(categoryId);
        }
        

        public void Log(string message)
        {
            Log(Level.Info, message);
        }

        public void Log(Level errorLevel, string message)
        {
            switch (errorLevel)
            {
                case Level.Info:
                    Console.ForegroundColor = ConsoleColor.White; break;
                case Level.Debug:
                    Console.ForegroundColor = ConsoleColor.Gray; break;
                case Level.Warn:
                    Console.ForegroundColor = ConsoleColor.DarkYellow; break;
                case Level.Error:
                    Console.ForegroundColor = ConsoleColor.Red; break;
                case Level.Exception:
                    Console.ForegroundColor = ConsoleColor.DarkCyan; break;
                case Level.Notify:
                    Console.ForegroundColor = ConsoleColor.Green; break;
            }            

            Console.WriteLine($"{DateTime.Now} {_process} {_subProcess ?? "--"} {message}");
        }

        public void LogException(Exception ex, string message)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(message);
            sb.AppendLine($" {ex.GetType().Name} : {ex.Message}");
            sb.AppendLine($"Stack: {ex.StackTrace}");

            Log(Level.Exception, sb.ToString());
        }

        public void LogIf(int categoryId, string message)
        {
            if (_categoryManager.IsCategoryEnabled(categoryId))
                Log(Level.Info, message);
        }

        public void LogIf(int categoryId, Level errorLevel, string message)
        {
            if (_categoryManager.IsCategoryEnabled(categoryId))
                Log(errorLevel, message);
        }

        public void RegisterCategories(object categories)
        {
            _categoryManager.RegisterCategories(categories);
        }

        public void RegisterCategory(int categoryId, string categoryName)
        {
            _categoryManager.RegisterCategory(categoryId, categoryName);
        }

        public void SetEnabledCategories(int[] categories)
        {
            _categoryManager.SetEnabledCategories(categories);
        }
    }
}
