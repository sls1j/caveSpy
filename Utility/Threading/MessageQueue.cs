using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Threading;
using System.Globalization;

namespace Bee.Eee.Utility.Threading
{
    /// <summary>
    /// A message queue that utilizes a threadpool to execute and handle the messages pushed to it.  Because this message 
    /// queue handles one message at a time and in sequential order there is no need to lock resources that are only touched 
    /// by the message queue even though different threads may execute the code.  This allows for asynchronous handling
    /// of messages.  
    /// </summary>
    public class MessageQueue : IMessageQueue, ITimeoutReceiver, IDisposable
    {
        private static long _nextMessageQueueId = 0;

        private Queue<MessageObject> _messageQueue;
        private IArrowThreadPool _pool;
        private Action _currentMessageHandler;
        private long _messageQueueId;
        private volatile bool _notProcessing;
        private ExecutionGuard _guard;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pool">The thread pool where threads will be pulled from to execute the message handler.</param>
        /// <param name="name">A name for this queue.  Can be used to determine what message queue is running if more than one queue is running on a threadpool.</param>
        public MessageQueue(IArrowThreadPool pool, string name)
        {
            if (null == pool)
                throw new ArgumentNullException("pool");

            _pool = pool;

            if (null == name)
                throw new ArgumentNullException("name");

            Name = name;

            _messageQueueId = Interlocked.Increment(ref _nextMessageQueueId);
            _messageQueue = new Queue<MessageObject>();
            _notProcessing = true;
            _guard = new ExecutionGuard();
        }

        /// <summary>
        /// The name of the MessageQueue.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// A number assigned to the message queue when it's created.
        /// </summary>
        public long MessageQueueId { get { return _messageQueueId; } }

        /// <summary>
        /// This event is called if the message handler throws and unhandled exception.
        /// </summary>
        public event EventHandler<UnhandledExceptionEventArgs> OnException;

        /// <summary>
        /// This puts a message onto the queue to have handled.
        /// </summary>
        /// <param name="message"></param>
        public void Push(MessageObject message)
        {
            if (_guard.EnterExecute())
            {

                try
                {
                    bool doSchedule = false;
                    // push message on to queue
                    lock (_messageQueue)
                    {
                        _messageQueue.Enqueue(message);
                        if (_notProcessing)
                        {
                            _notProcessing = false;
                            doSchedule = true;
                        }
                    }

                    if (doSchedule)
                    {
                        _currentMessageHandler = ProcessMessages;
                        _pool.AssignThread(_currentMessageHandler);
                    }
                }
                finally
                {
                    _guard.ExitExecute();
                }
            }
        }

        /// <summary>
        /// Removes all of the pending messages from the queue.
        /// </summary>
        public void Clear()
        {
            lock (_messageQueue)
                _messageQueue.Clear();            
        }

        /// <summary>
        /// Handles all of the messages.  This will empty the queue one message at a time.  The lock on the message queue is minimal to allow more messages to be pushed by other threads.
        /// </summary>        
        private void ProcessMessages()
        {
            if (_guard.EnterExecute())
            {
                try
                {
                    MessageObject message;
                    while (!_guard.IsDisabled)
                    {
                        lock (_messageQueue)
                        {
                            if (_messageQueue.Count > 0)
                                message = _messageQueue.Dequeue();
                            else
                            {
                                // this allows the process to be restarted
                                _notProcessing = true;
                                return;
                            }
                        }

                        try
                        {
                            if (null != OnMessage)
                                OnMessage(this, message);
                        }
                        catch (Exception ex)
                        {
                            if (null != OnException)
                            {
                                OnException(this, new UnhandledExceptionEventArgs(ex, false));
                            }
                        }
                    }
                }
                finally
                {
                    _guard.ExitExecute();
                }
            }
        }


        /// <summary>
        /// This is like Push except that it will delay the sending of the message until the ms interval timesout.
        /// </summary>
        /// <param name="ms">The number of milliseconds to wait before posting a TimeoutMessageObject onto the message queue.</param>
        /// <param name="state">An object that will be attached to the TimeoutMessageObject. null is a valid value.</param>
        /// <param name="includeStackTrace">This will include a stack trace of the caller of ScheduleTimeout method.  However, since this is very CPU intensive it should be turned off unless debugging.</param>
        /// <returns>A handle to identify the timeout.  This handle can be used to stop the timeout after it's been started.  It's useful for things like watch-dogs.</returns>
        public int ScheduleTimeout(int ms, object state, bool includeStackTrace = false)
        {
            if (_guard.EnterExecute())
            {
                try
                {
                    TimeoutInfo timeout = new TimeoutInfo() { timeoutState = state };

                    if (includeStackTrace)
                        timeout.startedAt = System.Environment.StackTrace;

                    return _pool.ScheduleTimeout(this, timeout, ms);
                }
                finally
                {
                    _guard.ExitExecute();
                }
            }
            else
                return -1;
        }

        /// <summary>
        /// The interface method of the thread pool.  This is called when the timeout expires. It puts a TimeoutMessageObject onto the queue  
        /// </summary>
        /// <param name="timerId">The identifier of the timeout.</param>
        /// <param name="state">The object that was passed in with the schedule.</param>
        void ITimeoutReceiver.ProcessTimeout(int timerId, object state)
        {
            var timer = state as TimeoutInfo;
            var message = new TimeoutMessageObject() { TimeoutId = timerId, startedAt = timer.startedAt, State = timer.timeoutState };
            Push(message);
        }

        /// <summary>
        /// Cancels an already started timeout.  It will kill the timer on the thread pool as well as look through the queue
        /// to remove TimeoutMessageObject from the message queue if it's already been added.
        /// </summary>
        /// <param name="timeoutHandle">The handle identifying the timeout.  It is the value return by the ScheduleTimeout method</param>
        public void CancelTimeout(int timeoutHandle)
        {
            if (_guard.EnterExecute())
            {

                try
                {
                    // remove from the list
                    bool successfullyCancelOnPool = _pool.CancelTimeout(timeoutHandle);

                    // the timeout just fired -- check to see if it's already on the queue.  Remove it if it is.
                    if (successfullyCancelOnPool == false)
                    {
                        lock (_messageQueue)
                        {
                            int messageCount = _messageQueue.Count;
                            for (int i = 0; i < messageCount; i++)
                            {
                                var message = _messageQueue.Dequeue();
                                TimeoutMessageObject tm = message as TimeoutMessageObject;
                                if (null == tm || tm.TimeoutId != timeoutHandle)
                                    _messageQueue.Enqueue(message);
                            }
                        }
                    }
                }
                finally
                {
                    _guard.ExitExecute();
                }
            }
        }

        /// <summary>
        /// A delegate to handle the messages comming of the queue.
        /// </summary>
        public Action<IMessageQueue, MessageObject> OnMessage { get; set; }

        /// <summary>
        /// Wraps the state of the timeout within an objects to build the TimeoutMessageObject when the timeout is triggered.
        /// </summary>
        class TimeoutInfo
        {
            public object timeoutState;
            public string startedAt;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_guard.IsDisabled)
                return;

            if (isDisposing)
            {
                // shutdown the whole process.  Will no longer handle messages.
                _guard.DisableExecute();

                // kill unhandled messages.  No need for a lock since it can't be
                // reached by the public methods anymore.
                _messageQueue.Clear();

                // do not dispose it!  It's not owned by the messageQueue.
                _pool.CancelTimeoutByReceiver(this);
                _pool = null;
                // clear the message handle to allow this to dispose.  Don't want 
                // stray references to outside objects.
                _currentMessageHandler = null;
            }
        }
    }

    /// <summary>
    /// A message Queue.
    /// </summary>
    public interface IMessageQueue
    {
        /// <summary>
        /// Pushes a message on to the queue to be handled.
        /// </summary>
        /// <param name="message"></param>
        void Push(MessageObject message);
        /// <summary>
        /// The name of the message queue.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Removes all of the messages from the queue.
        /// </summary>
        void Clear();
    }   

    /// <summary>
    /// All messages passed to the queue are based on this object.
    /// </summary>
    public class MessageObject
    {
        /// <summary>
        /// This is the stack trace of the message that called the Push or ScheduleTimeout methods.
        /// </summary>
        public string originatedFrom;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="includeStackTrace">If true is specified a stack trace as a string will be included.  Really should only be used for debugging.  It's very CPU intensive.</param>
        public MessageObject(bool includeStackTrace = false)
        {
            if (includeStackTrace)
                originatedFrom = System.Environment.StackTrace;
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }

    /// <summary>
    /// A MessageObject that contains an interface to the object that sends the message.
    /// </summary>
    public class SenderMessage : MessageObject
    {
        public IMessageQueue sender;

        public override string ToString()
        {
            if (null != sender)
                return string.Format(CultureInfo.InvariantCulture, "{0} From {1}", GetType().Name, sender.Name);
            else
                return GetType().Name;
        }
    }

    /// <summary>
    /// This message is generated when a timeout 
    /// </summary>
    public class TimeoutMessageObject : MessageObject
    {
        /// <summary>
        /// The timeout handle returned by MessageQueue.ScheduleTimeout
        /// </summary>
        public int TimeoutId;
        /// <summary>
        /// The object that was passed in by MessageQueue.ScheduleTimeout
        /// </summary>
        public object State;
        /// <summary>
        /// A stack trace of when the timeout was scheduled. In normal operation this will be null. See MessageQueue.ScheduleTimeout
        /// </summary>
        public string startedAt;

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} TimeoutId {1}", GetType().Name, TimeoutId);
        }
    }

}
