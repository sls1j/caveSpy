using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bee.Eee.Utility.Logging
{
    /// <summary>
    /// Dumps logs to a file. When creating sub-loggers it will dump to the same file.
    /// </summary>
    public class FileLogger : ILogger, IDisposable
    {
        private string _process;
        private string _subProcess;
        private ICategoryManager _categoryManager;

        private FileStream _fout;
        private StreamWriter _writer;
        private bool _ownFile;

        public FileLogger(string process, object categories = null, string logPath = null)
            : this(new CategoryManager(), process, "Main", categories, logPath)
        {

        }

        public bool DumpToConsole { get; set; }

        protected FileLogger(ICategoryManager categoryManager, string process, string subProcess = null, object categories = null, string logPath = null)
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

            string dir = Path.GetDirectoryName(Path.GetFullPath(logPath));
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            _fout = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.Read);
            _writer = new StreamWriter(_fout);
            _ownFile = true;

            Log("**************** New Logging session ****************");
        }

        protected FileLogger(FileLogger logger, string subProcess)
        {
            this._categoryManager = logger._categoryManager;
            this._process = logger._process;
            this._subProcess = subProcess;
            this._categoryManager = logger._categoryManager;
            this._fout = logger._fout;
            this._writer = logger._writer;
            this._ownFile = false;
            this.DumpToConsole = logger.DumpToConsole;
        }


        public ILogger CreateSub(string subProcessName)
        {
            return new FileLogger(this, subProcessName);
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

        private void WriteLog(string log, Level errorLevel)
        {
            lock (_writer)
                _writer.WriteLine(log);

            if (DumpToConsole)
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

                Console.WriteLine("{0}", log);
            }
        }

        public void Log(string messageFormat, params object[] args)
        {
            Log(Level.Info, messageFormat, args);
        }

        public void Log(Level errorLevel, string messageFormat, params object[] args)
        {
            string message = string.Format(messageFormat, args);
            string log = string.Format("{0} {1} {2} {3}",
                DateTime.Now, _process, _subProcess ?? "--", message);

            WriteLog(log, errorLevel);
        }

        public void LogException(Exception ex, string messageFormat, params object[] args)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(messageFormat, args);
            sb.AppendFormat("{0} : {1}", ex.GetType().Name, ex.Message);
            sb.AppendLine();
            sb.AppendFormat("Stack: {0}", ex.StackTrace);
            sb.AppendLine();

            Log(Level.Exception, "{0}", sb.ToString());
        }

        public void LogIf(int categoryId, string messageFormat, params object[] args)
        {
            if (_categoryManager.IsCategoryEnabled(categoryId))
                Log(Level.Info, messageFormat, args);
        }

        public void LogIf(int categoryId, Level errorLevel, string messageFormat, params object[] args)
        {
            if (_categoryManager.IsCategoryEnabled(categoryId))
                Log(errorLevel, messageFormat, args);
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (null != _writer)
                {
                    if (_ownFile)
                    {
                        _writer.Dispose();
                        _fout.Dispose();
                    }

                    _writer = null;
                    _fout = null;
                }
            }
        }
    }
}
