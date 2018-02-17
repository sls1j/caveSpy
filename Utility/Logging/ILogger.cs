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
        void Log(string message);
        
        
        void Log(Level errorLevel, string message);
        void LogException(Exception ex, string message);
        void LogIf(int categoryId, string message);
        void LogIf(int categoryId, Level errorLevel, string message);        
        ILogger CreateSub(string subProcessName);
    }
}
