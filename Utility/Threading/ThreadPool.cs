using System;
using System.Collections.Generic;
using System.Text;
using Thread = System.Threading.Thread;
using Interlocked = System.Threading.Interlocked;
using AutoResetEvent = System.Threading.AutoResetEvent;
using ManualResetEvent = System.Threading.ManualResetEvent;
namespace Bee.Eee.Utility.Threading
{
    /// <summary>
    /// Provides a simple thread pool that allows naming of the individual threads.
    /// </summary>
    public sealed class BeeEeeThreadPool : IArrowThreadPool
    {
        private static int _nextThreadPoolId = 0;

        private string _poolName;
        private Func<DateTime> _utcNow;
        private int _threadPoolId;
        private int _minThreads, _maxThreads;
        private object _lock; // used to keep the threads playing nice when using queues
        private readonly Queue<ThreadPoolThread> _availableThreads;
        private readonly ThreadPoolThread[] _allThreads;
        private int _threadsAssigned;
        private int _workQueueSize;
        private readonly Queue<Action> _workQueue;

        // timer fields
        private int _nextTimeoutId = 1;
        private List<TimeoutInfo> _timeouts = new List<TimeoutInfo>();
        private Thread _timeoutProcessingThread;
        private AutoResetEvent _recheckTimeout = new AutoResetEvent(false);
        private ManualResetEvent _timeoutProcessingFinished = new ManualResetEvent(false);

        /// <summary>
        /// This event is fired whenever there is a new thread pool thread created.  If defined all thread pools with send with this static event.
        /// </summary>
        public static event EventHandler<NewThreadPoolThreadArgs> OnNewThreadPoolThread;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="minThreads">The number of threads that will be started initially.</param>
        /// <param name="maxThreads">The maximum number of threads that will be allowed to grow to.</param>
        /// <param name="poolName">The name of the thread pool.</param>
        public BeeEeeThreadPool(int minThreads, int maxThreads, string poolName) :
            this(minThreads, maxThreads, poolName, null)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="minThreads">The number of threads that will be started initially.</param>
        /// <param name="maxThreads">The maximum number of threads that will be allowed to grow to.</param>
        /// <param name="poolName">The name of the thread pool.</param>
        /// <param name="os">Instance of the OS extraction.  Used for getting the UTC date time</param>
        public BeeEeeThreadPool(int minThreads, int maxThreads, string poolName, Func<DateTime> utcNow)
        {
            _poolName = poolName;
            _threadPoolId = Interlocked.Increment(ref _nextThreadPoolId);
            _minThreads = minThreads;
            _maxThreads = maxThreads;
            if (null == utcNow)
                _utcNow = () => DateTime.UtcNow;
            else
                _utcNow = utcNow;

            _availableThreads = new Queue<ThreadPoolThread>();
            _allThreads = new ThreadPoolThread[_maxThreads];
            _workQueue = new Queue<Action>();
            _lock = new object();
            for (int i = 0; i < _maxThreads; i++)
            {
                var thread = BuildThread(i);
                if (i < _minThreads)
                    thread.Start();
                _availableThreads.Enqueue(thread);
                _allThreads[i] = thread;
            }

            _timeoutProcessingThread = new Thread(ProcessTimeouts);
            _timeoutProcessingThread.Name = string.Format("{0} Timeout", _poolName);
            _timeoutProcessingThread.Start();
        }

        /// <summary>
        /// The number of threads that have been assigned.
        /// </summary>
        public int ThreadsAssigned { get { return Interlocked.Add(ref _threadsAssigned, 0); } }
        /// <summary>
        /// The total number of threads that can be assigned.
        /// </summary>
        public int MaxThreads { get { return _maxThreads; } }
        /// <summary>
        /// The total number of items of work that has been assigned to the thread but has not yet been finished.  This can be greater than the number of threads.
        /// </summary>
        public int WorkQueueSize { get { return Interlocked.Add(ref _workQueueSize, 0); } }

        /// <summary>
        /// Creates a ThreadPoolThread instance.
        /// </summary>
        /// <param name="id">The id for the thread.</param>
        /// <returns>The created thread</returns>
        private ThreadPoolThread BuildThread(int id)
        {
            ThreadPoolThread thread = new ThreadPoolThread(_poolName, id + 1, ThreadStarting);
            thread.OnFinished += WorkFinished;

            return thread;
        }

        /// <summary>
        /// Fires the OnNewThreadPoolThread event.
        /// </summary>
        /// <param name="thread"></param>
        private void ThreadStarting(ThreadPoolThread thread)
        {
            if (null != OnNewThreadPoolThread)
            {
                try
                {
                    OnNewThreadPoolThread(this, new NewThreadPoolThreadArgs()
                    {
                        name = thread.Name,
                        thread = thread.Thread
                    });
                }
                catch (Exception)
                {

                    throw;
                }
            }
        }

        /// <summary>
        /// This puts a work request on the queue to be executed.  The logic then immediatly tries to assign a thread the work.
        /// </summary>
        /// <param name="work"></param>
        public void AssignThread(Action work)
        {
            ThreadPoolThread thread = null;
            Action currentWork = null;
            lock (_lock)
            {
                if (IsDisposed)
                    return;

                _workQueue.Enqueue(work);
                Interlocked.Increment(ref _workQueueSize);

                if (_availableThreads.Count == 0)
                    return;

                // okay have thread and have work
                thread = _availableThreads.Dequeue();
                Interlocked.Increment(ref _threadsAssigned);

                currentWork = _workQueue.Dequeue(); // this seems strange to deque right after an enqueue but there could be work waiting on the work queue
                Interlocked.Decrement(ref _workQueueSize);
            }

            // we do it here -- the thread and must not be null and should not according to the above logic
            thread.AssignWork(currentWork);
        }

        /// <summary>
        /// Kills the assigned work off that is in a thread.
        /// </summary>
        /// <param name="work"></param>
        public void KillThread(Action work)
        {
            ThreadPoolThread t = null;
            lock (_lock)
            {
                int cnt = _allThreads.Length;
                for (int i = 0; i < cnt; i++)
                {
                    t = _allThreads[i];
                    if (t.Work == work)
                        goto kill_it;
                }
                return;
            }

            kill_it:
            t.KillWork();
        }

        /// <summary>
        /// This is called by the ThreadPoolThread when it's finished processing the unit of work it was given.
        /// </summary>
        /// <param name="thread">The thread that finished</param>
        private void WorkFinished(ThreadPoolThread thread)
        {
            if (IsDisposed)
                return;

            Action currentWork = null;
            lock (_lock)
            {
                if (_workQueue.Count > 0)
                {
                    currentWork = _workQueue.Dequeue();
                    Interlocked.Decrement(ref _workQueueSize);
                }
                else
                {
                    _availableThreads.Enqueue(thread);
                    Interlocked.Decrement(ref _threadsAssigned);
                }
            }

            if (null != currentWork)
                thread.AssignWork(currentWork);

            if (null != OnThreadFinished)
            {
                try
                {
                    OnThreadFinished(this, new EventArgs());
                }
                catch (Exception)
                { }
            }
        }

        /// <summary>
        /// Event is fired when a thread has finished a unit of work.
        /// </summary>
        public event EventHandler OnThreadFinished;

        /// <summary>
        /// Schedules a timeout event to occur in a given amount of time.
        /// </summary>
        /// <param name="receiver">The interface that will receive the timeout notification</param>
        /// <param name="state">An object that will be attached to the timeout event</param>
        /// <param name="due">The number of milliseconds that will elapse before the interface is notifies</param>
        /// <returns>A handle that identifies the timeout event</returns>
        public int ScheduleTimeout(ITimeoutReceiver receiver, object state, int due)
        {
            TimeoutInfo info = new TimeoutInfo()
            {
                State = state,
                Receiver = receiver,
                ExpiresAt = _utcNow().AddMilliseconds(due)
            };

            lock (_timeouts)
            {
                info.ID = _nextTimeoutId++;
                if (_nextTimeoutId >= int.MaxValue)
                    _nextTimeoutId = 0;

                // add the timeout in order
                var index = _timeouts.BinarySearch(info, info);
                if (index < 0)
                    index = ~index;
                _timeouts.Insert(index, info);
            }

            _recheckTimeout.Set();

            return info.ID;
        }

        /// <summary>
        /// Cancel a timeout.
        /// </summary>
        /// <param name="timeoutHandle">The handle returned by ScheduleTimeout</param>
        /// <returns>True if successful.  False if the timeout wasn't in the list.</returns>
        public bool CancelTimeout(int timeoutHandle)
        {
            lock (_timeouts)
            {
                int cnt = _timeouts.Count;
                for (int i = 0; i < cnt; i++)
                {
                    if (_timeouts[i].ID == timeoutHandle)
                    {
                        _timeouts.RemoveAt(i);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Cancels all timeouts associated with the given receiver destination.  
        /// </summary>
        /// <param name="receiver">The destination of the timeout</param>
        /// <returns>The number of timeouts removed.</returns>
        public int CancelTimeoutByReceiver(ITimeoutReceiver receiver)
        {
            lock (_timeouts)
            {
                return _timeouts.RemoveAll(t => t.Receiver == receiver);
            }
        }

        /// <summary>
        /// The heart of the timeout thread.  This is used instead of the .Net Timer class because it is more efficient.  This allows for 
        /// a high volume of timeouts to be scheduled and processed without the overhead of creating and disposing the Timer class.
        /// </summary>
        /// <param name="notUsed"></param>
        private void ProcessTimeouts(object notUsed)
        {
            try
            {
                Queue<TimeoutInfo> expired = new Queue<TimeoutInfo>();
                int nextTimeout = 10000;
                while (true)
                {
                    _recheckTimeout.WaitOne(nextTimeout); // we usually don't need too high of percision

                    if (IsDisposed)
                        break;

                    DateTime now = _utcNow();
                    lock (_timeouts)
                    {
                        int deadCount = 0;
                        int i = _timeouts.Count - 1;
                        for (; i >= 0; i--)
                        {
                            var timeout = _timeouts[i];
                            if (timeout.ExpiresAt <= now)
                            {
                                timeout.Expired = true;
                                deadCount++;
                                expired.Enqueue(timeout);
                            }
                            else
                            {
                                break;
                            }
                        }

                        // because the list is sorted we always remove the whole tale of dead timeouts
                        if (deadCount > 0)
                        {
                            _timeouts.RemoveRange(i + 1, deadCount);
                        }

                        // calculate when the next check will occur -- the max is 10 seconds because of possible inaccuracies of the signal
                        if (_timeouts.Count > 0)
                        {
                            var lastTimeOut = _timeouts[_timeouts.Count - 1];
                            int expiresAt = (int)((lastTimeOut.ExpiresAt - now).TotalMilliseconds);
                            nextTimeout = Math.Min(10000, expiresAt);
                        }
                        else
                            nextTimeout = 10000;
                    }

                    // signal all of the expired timeout receivers
                    while (expired.Count > 0)
                    {
                        try
                        {
                            var timeout = expired.Dequeue();
                            timeout.Receiver.ProcessTimeout(timeout.ID, timeout.State);
                        }
                        catch (Exception)
                        {

                        }
                    }
                }
            }
            finally
            {
                _timeoutProcessingFinished.Set();
            }
        }

        /// <summary>
        /// The class that keeps track of the timeout instance
        /// </summary>
        internal class TimeoutInfo : IComparer<TimeoutInfo>
        {
            public int ID { get; set; }
            public DateTime ExpiresAt { get; set; }
            public object State { get; set; }
            public ITimeoutReceiver Receiver { get; set; }
            public bool Expired { get; set; }

            public int Compare(TimeoutInfo x, TimeoutInfo y)
            {
                return DateTime.Compare(y.ExpiresAt, x.ExpiresAt);
            }

            public override string ToString()
            {
                return string.Format("{0} {1} {2}", ID, ExpiresAt.ToString("hh:mm:ss.fff"), Expired);
            }
        }

        private int _isDisposed = 0;

        private bool IsDisposed
        {
            get { return 1 == Interlocked.Add(ref _isDisposed, 0); }
        }

        public void Dispose()
        {
            if (1 == Interlocked.Exchange(ref _isDisposed, 1))
                return;

            lock (_lock)
            {
                for (int i = 0; i < _maxThreads; i++)
                {
                    _allThreads[i].Dispose();
                    _allThreads[i] = null;
                }

                _availableThreads.Clear();
                _workQueue.Clear();
            }

            _recheckTimeout.Set();
            _timeoutProcessingFinished.WaitOne();
            _timeouts.Clear();

            if (null != _recheckTimeout)
                _recheckTimeout.Dispose();

            if (null != _timeoutProcessingFinished)
                _timeoutProcessingFinished.Dispose();

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// A threadpool thread.
        /// </summary>
        private sealed class ThreadPoolThread : IDisposable
        {
            private Thread _thread;
            private int _threadId;
            private AutoResetEvent _haveWork;
            private ManualResetEvent _startedSignal;
            private ManualResetEvent _finished;
            private volatile bool _running;
            private volatile Action _work;
            public event Action<ThreadPoolThread> OnFinished;
            public Action<ThreadPoolThread> _starting;

            private volatile bool _started;

            public ThreadPoolThread(string poolName, int threadId, Action<ThreadPoolThread> starting)
            {
                _threadId = threadId;
                _starting = starting;
                _haveWork = new AutoResetEvent(false);
                _startedSignal = new ManualResetEvent(false);
                _finished = new ManualResetEvent(false);
                _running = true;
                _started = false;
                _thread = new Thread(DoWork);
                _thread.Name = Name = string.Format("{0} #{1}", poolName, threadId);
            }


            public Action Work { get { return _work; } }

            public void Start()
            {
                if (_started)
                    return;

                _started = true;
                _thread.Start();
                _starting(this);
            }

            public void AssignWork(Action work)
            {
                if (!_started)
                {
                    _started = true;
                    _thread.Start();
                    _startedSignal.WaitOne();
                }

                _work = work;
                _haveWork.Set();
            }

            public void KillWork()
            {
                if (_started)
                    _thread.Abort();
                else
                    _finished.Set();
            }

            private void DoWork()
            {
                _startedSignal.Set();
                bool needOnFinished = false;
                try
                {
                    while (_running)
                    {
                        _haveWork.WaitOne();
                        if (null != _work)
                        {
                            try
                            {
                                needOnFinished = true;
                                _work();
                                _work = null; // to allow the item that issued the work item to be garbaged collected
                                needOnFinished = false;
                            }
                            catch (System.Threading.ThreadAbortException)
                            {
                                _running = false;
                            }
                            catch (Exception)
                            {
                                // must catch all exceptions and swallow -- this must not die!
                            }

                            OnFinished(this);
                        }
                    }
                }
                finally
                {
                    _work = null;

                    try
                    {
                        if (needOnFinished)
                            OnFinished(this);
                    }
                    catch (Exception)
                    {
                    }

                    _finished.Set();
                }
            }

            private int _Disposed = 0;

            public void Dispose()
            {
                if (1 == Interlocked.Exchange(ref _Disposed, 1))
                    return;

                try
                {
                    if (_started)
                    {
                        _running = false;
                        _haveWork.Set();
                        if (!_finished.WaitOne(100))
                        {
                            _thread.Abort();
                            _finished.WaitOne();
                        }
                    }

                    if (null != _startedSignal)
                        _startedSignal.Dispose();

                    if (null != _finished)
                        _finished.Dispose();

                    if (null != _haveWork)
                        _haveWork.Dispose();

                    _startedSignal = null;
                    _finished = null;
                    _haveWork = null;
                    _thread = null;
                    _work = null;
                }
                catch (Exception)
                {

                }

                GC.SuppressFinalize(this);
            }

            public string Name { get; private set; }
            public Thread Thread { get { return _thread; } }
        }
    }

    /// <summary>
    /// Interface for a thread pool
    /// </summary>
    public interface IArrowThreadPool : IDisposable
    {
        /// <summary>
        /// Assign work to the thread pool
        /// </summary>
        /// <param name="work">The item of work</param>
        void AssignThread(Action work);
        /// <summary>
        /// Interupt work that is executing
        /// </summary>
        /// <param name="work">The item of work to be killed</param>
        void KillThread(Action work);
        /// <summary>
        /// The number of threads that are assign work.
        /// </summary>
        int ThreadsAssigned { get; }
        /// <summary>
        /// The maximum number of threads that can be running in the thread pool
        /// </summary>
        int MaxThreads { get; }
        /// <summary>
        /// The number of items of work on the queue waiting to be assigned work
        /// </summary>
        int WorkQueueSize { get; }
        /// <summary>
        /// Schedule a timeout
        /// </summary>
        /// <param name="receiver">The receiver of the timeout</param>
        /// <param name="state">An object to be attached to the timeout event</param>
        /// <param name="due">The time to ellapse before the timeout is fired</param>
        /// <returns>A handle that identifies the timeout until it's fired</returns>
        int ScheduleTimeout(ITimeoutReceiver receiver, object state, int due);
        /// <summary>
        /// Cancel the timeout.
        /// </summary>
        /// <param name="timeoutHandle">The handle returned by ScheduleTimeout</param>
        /// <returns>True if the timeout was canceled, false if there was no match.</returns>
        bool CancelTimeout(int timeoutHandle);
        /// <summary>
        /// Cancel all of the timeouts that are destined for the given receiver.
        /// </summary>
        /// <param name="receiver">The receiver</param>
        /// <returns>The number of timeouts canceled</returns>
        int CancelTimeoutByReceiver(ITimeoutReceiver receiver);
    }

    /// <summary>
    /// This interface must be implemented by the receiver of the timeout event
    /// </summary>
    public interface ITimeoutReceiver
    {
        /// <summary>
        /// The timeout method that is called.
        /// </summary>
        /// <param name="id">The id of the timeout as returned by IThreadPool.ScheduleTimeout</param>
        /// <param name="state">The state object as specified in IThreadPool.ScheduleTimeout</param>
        void ProcessTimeout(int id, object state);
    }

    /// <summary>
    /// Event args for a new class.  This is to help registering these threads with the thread monitor
    /// </summary>
    public class NewThreadPoolThreadArgs : EventArgs
    {
        public string name;
        public Thread thread;
    }
}
