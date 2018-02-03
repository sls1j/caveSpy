using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bee.Eee.Utility.Logging
{
    /// <summary>
    /// Interface defining Logging.
    /// </summary>
    public interface ILogger : ICategoryManager
    {
        void Log(string messageFormat, params object[] args);
        void Log(Level errorLevel, string messageFormat, params object[] args);
        void LogException(Exception ex, string messageForamt, params object[] args);
        void LogIf(int categoryId, string messageFormat, params object[] args);
        void LogIf(int categoryId, Level errorLevel, string messageFormat, params object[] args);        
        ILogger CreateSub(string subProcessName);
    }
}
