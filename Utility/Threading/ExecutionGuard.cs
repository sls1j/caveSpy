using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Bee.Eee.Utility.Threading
{   
    /// <summary>
    /// This acts as a threadsafe guardian for method execution. It's purpose is to assist with object clean up
    /// by creating a guard around code that might be executing in a different thread sa the dispose come.
    /// It's purpose is to cause the Dispose to wait until all the external threads have finished executing their
    /// code and the object is stable.  Below are some patters of use.
    /// 
    /// 
    /// ExecutionGuard eg = new ExecutionGuard
    /// IAnObjectThatIsDisposed o;
    /// private void OnEvent()
    /// {
    ///   if ( exo.EnterExecute() )
    ///   {   
    ///     try
    ///     {
    ///       // this code is protected from disposing too soon
    ///       ...
    ///       o.DoSomething();
    ///       ...
    ///     }
    ///     finally
    ///     {
    ///       o.ExitExecute();
    ///     }
    ///   }
    /// }
    /// 
    /// private void CalledByThread()
    /// {
    ///     // will only execute if the dispose has not
    ///     // been called.  This guantee's o is valid.
    ///     eg.Execute( ()=> o.DoSomething() );   
    /// }
    /// 
    /// ....
    /// 
    /// private void Dispose()
    /// {
    ///    eg.DisableExecute();
    ///    // now we can safely dispose
    ///    o.Dispose();
    ///    o = null;
    /// }
    /// 
    /// 
    /// </summary>
    public class ExecutionGuard
    {
        private int _inExecutionCount;
        private int _executionDisabledCount;

        public ExecutionGuard( bool isEnabled = true )
        {
            _inExecutionCount = 0;
            _executionDisabledCount = (isEnabled)?0:1;
        }

        /// <summary>
        /// Begin executing a section of code.  If it returns true then the code
        /// can be safely execute, if false then the code should not be execute
        /// but the event left immediately.
        /// </summary>
        /// <returns>True for okay execute, false to indicate that execution should not occur</returns>
        public bool EnterExecute()
        {
            Interlocked.Increment(ref _inExecutionCount);

            // check whether we can execute or not
            if (Interlocked.Add(ref _executionDisabledCount, 0) > 0)
            {
                // cannot execute -- exit -- return the internal state back to what it was before
                Interlocked.Decrement(ref _inExecutionCount);
                return false;
            }

            // can execute continue on
            return true;
        }

        /// <summary>
        /// If EnterExecute returns a true this must be called when leaving the protected code, otherwise DisableExecute will deadlock.
        /// </summary>
        public void ExitExecute()
        {
            Interlocked.Decrement(ref _inExecutionCount);
        }

        /// <summary>
        /// Signals that any protected code should not be executed, then blocks until all of the running protected code completes.
        /// After this function returns any objects within the protected code can be safely disposed.
        /// </summary>
        /// <returns>False if it was already disabled, true if it transitioned from enabled to disabled.</returns>
        public bool DisableExecute()
        {
            // disable the execution
            if (1 == Interlocked.Exchange(ref _executionDisabledCount, 1))
                return false;

            // wait for all executions to exit
            while (Interlocked.Add(ref _inExecutionCount, 0) > 0)
                Thread.Yield();

            return true;
        }

        /// <summary>
        /// Re-enables execution
        /// </summary>
        public void EnableExecute()
        {
            Interlocked.Exchange(ref _executionDisabledCount, 0);
        }

        /// <summary>
        /// Implements the pattern to be executed around the action.  This simplifies some of the
        /// semantics.
        /// </summary>
        /// <param name="a"></param>
        public void Execute(Action action)
        {
            if (EnterExecute())
            {
                try
                {
                    action();
                }
                finally
                {
                    ExitExecute();
                }
            }
        }

        public bool IsDisabled
        {
            get
            {
                return Interlocked.Add(ref _executionDisabledCount, 0) > 0;
            }
        }    
    }

}
