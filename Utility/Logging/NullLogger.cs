using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bee.Eee.Utility.Logging
{
    /// <summary>
    /// An empty logger for testing.
    /// </summary>
    public class NullLogger : ILogger
    {
        public ILogger CreateSub(string subProcessName)
        {
            return this;
        }

        public void DisableCategroy(int categoryId)
        {
            
        }

        public void EnableCategory(int categoryId)
        {

        }

        public void Init(string processName, string subProcessName = null)
        {

        }

        public bool IsCategoryEnabled(int categoryId)
        {
            return false;
        }

        public void Log(string messageFormat, params object[] args)
        {

        }

        public void Log(Level errorLevel, string messageFormat, params object[] args)
        {

        }

        public void LogException(Exception ex, string messageForamt, params object[] args)
        {

        }

        public void LogIf(int categoryId, string messageFormat, params object[] args)
        {

        }

        public void LogIf(int categoryId, Level errorLevel, string messageFormat, params object[] args)
        {

        }

        public void RegisterCategories(object categories)
        {
            
        }

        public void RegisterCategory(int categoryId, string categoryName)
        {

        }

        public void SetEnabledCategories(int[] categories)
        {

        }
    }
}
