using Bee.Eee.Utility.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Bee.Eee.Utility.Threading
{
    /// <summary>
    /// Provides a framework around a thread.  It provides an init function, a thread-safe messaging
    /// interface, an idle function, and a on finished function, all called within the thread.    
    /// </summary>
    public class MessageThread : IMessageThread, IDisposable
    {
        private ExecutionGuard _exObs;
        private static bool VerboseEvents = false;
        private Queue<object> _messages; // the thread messages
        private ManualResetEvent _haveInMessage; // an event to more effectively handle messages
        private Thread _thread; // the thread instance
        private volatile bool _running; // true then the thread is running, false means the thread has exited.
        private volatile bool _stopRequested; // that the thread has been canceled.
        private long _msgCnt;
        private int _idleLoopSpeed;

        protected string ThreadName { get; set; }

        public static event EventHandler<MessageThreadNewArgs> OnNewMessageThread;

        /// <summary>
        /// True, the thread is running.  False the thread has exited.
        /// </summary>
        public bool IsRunning
        {
            get { return _running; }
        }

        public int IdleLoopSpeed
        {
            get { return _idleLoopSpeed; }
            set { _idleLoopSpeed = value; }
        }

        protected ILogger Logger { get; set; }
     
        /// <summary>
        /// Constructor
        /// </summary>
        public MessageThread(ILogger logger, string threadName)
        {
            Logger = logger;

            ThreadName = threadName ?? string.Format("{0}:MessageThread", GetType().Name);

            _messages = new Queue<object>();
            _haveInMessage = new ManualResetEvent(true);
            _idleLoopSpeed = 250;
            _exObs = new ExecutionGuard();
        }

        /// <summary>
        /// Starts the thread.
        /// </summary>
        public void Start()
        {
            if (_running)
                return;

            _thread = new Thread(new ThreadStart(DoWork));
            _thread.Name = ThreadName;
            _thread.Start();

            try
            {
                if (null != OnNewMessageThread)
                    OnNewMessageThread(this, new MessageThreadNewArgs() { internalThread = _thread, name = ThreadName });
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Signals the thread to stop and waits for it to finished.  Must not be called
        /// within the thread.
        /// </summary>
        public void Stop()
        {
            _stopRequested = true;
            _haveInMessage.Set();
        }

        /// <summary>
        /// Sends a thread-safe message, that is handled by OnMessage
        /// </summary>
        /// <param name="message"></param>
        public void Push(object message)
        {
            lock (_messages)
            {
                _messages.Enqueue(message);
                _haveInMessage.Set();
            }
        }

        /// <summary>
        /// Sends a thread-safe message and Blocks until the message has been handled.
        /// </summary>
        /// <param name="message"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly")]
        public void SendSyncMessage(object message)
        {
            if (Thread.CurrentThread == _thread)
                throw new InvalidOperationException("Cannot call SendSyncMessage from own thread (OnInit, OnIdle, OnMessage, or OnFinished)!  It will dead lock.");

            using (SyncMessage msg = new SyncMessage(message))
            {
                // send message
                Push(msg);
                // wait for handling
                msg.wait.WaitOne();
            }
        }

        /// <summary>
        /// Is called when the thread starts, for initialization purposes.
        /// </summary>
        protected virtual void OnInit()
        {
        }

        /// <summary>
        /// Called when a thread message is sent.
        /// </summary>
        /// <param name="message">The message object</param>
        protected virtual void OnMessage(object message)
        {
        }

        /// <summary>
        /// Called every 250ms by default (see IdleLoopSpeed) or directly after the OnMessage 
        /// call is handled, which ever happens first.
        /// </summary>
        protected virtual void OnIdle()
        {
        }

        /// <summary>
        /// Called right before the exit of the thread.
        /// </summary>
        protected virtual void OnFinished()
        {
        }

        // CA1031 - General exceptions in this context makes sense because we have no idea what is happening in the 
        //          called functions OnInit, OnFinished, etc.

        /// <summary>
        /// Thread function that defines the flow of the thread.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void DoWork()
        {
            _exObs.Execute(() =>
           {
               // the base init itself
               try
               {
                   OnInit();
               }
               catch (Exception ex)
               {
                   Logger.LogException(ex, "MessageThread OnInit threw an exception");
               }

               while (true)
               {
                   // slow down the loop and wait for a message
                   _haveInMessage.WaitOne(_idleLoopSpeed);

                   while (true)
                   {
                       // get message
                       object message = null;
                       lock (_messages)
                       {
                           if (VerboseEvents)
                           {
                               _msgCnt++;
                               if (_msgCnt % 10000 == 0)
                               {
                                   Logger.Log("MessageThread: {0} Message Queue Count: {1}", GetType().Name, _messages.Count);
                                   _msgCnt = 0;
                               }
                           }

                           if (_messages.Count > 0)
                               message = _messages.Dequeue();
                           else
                           {
                               _haveInMessage.Reset();
                               break;
                           }
                       }

                       // call on message function
                       object m = null;

                       // see if it's a synchronus message
                       SyncMessage sm = message as SyncMessage;
                       if (sm != null)
                       {
                           m = sm.message;
                       }
                       else
                       {
                           m = message;
                       }

                       // handle message
                       try
                       {
                           OnMessage(m);
                       }
                       catch (Exception ex)
                       {
                           Logger.LogException(ex, "OnMessage threw an exception");
                       }

                       // signal synchronus messages
                       if (null != sm)
                           sm.wait.Set();
                   }

                   // call idle function
                   try
                   {
                       OnIdle();
                   }
                   catch (Exception ex)
                   {
                       Logger.LogException(ex, "OnIdle threw an exception");
                   }

                   if (_stopRequested)
                       break;
               }

               // call finished function before exiting thread context.
               try
               {
                   OnFinished();
               }
               catch (Exception ex)
               {
                   Logger.LogException(ex, "OnFinished threw an exception");
               }

               _running = false;

               Logger.Log("Exiting thread: {0}", this.GetType().ToString());
           });
        }

        #region IDisposable implementation

        private int _isDisposed = 0;

        private bool IsDisposed
        {
            get { return 1 == Interlocked.Add(ref _isDisposed, 0); }
        }


        /// <summary>
        /// Make sure the thread is stopped when dispose is called.
        /// </summary>
        /// <param name="isDisposing"></param>
        protected virtual void Dispose(bool isDisposing)
        {
            if (1 == Interlocked.Exchange(ref _isDisposed, 1))
                return;


            if (isDisposing)
            {
                Stop();

                _exObs.DisableExecute();

                if (_haveInMessage != null)
                    _haveInMessage.Close();                    
            }
            _haveInMessage = null;
            Logger = null;
        }

        /// <summary>
        /// Make sure the thread is stopped when dispose is called.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion IDisposable implementation

        private class SyncMessage : IDisposable
        {
            public object message;
            public AutoResetEvent wait;
            public SyncMessage(object message)
            {
                this.message = message;
                this.wait = new AutoResetEvent(false);
            }

            #region IDisposable implementation

            private int _isDisposed = 0;
            public bool IsDisposed
            {
                get
                {
                    return 1 == Interlocked.Add(ref _isDisposed, 0);
                }
            }

            /// <summary>
            /// Make sure the thread is stopped when dispose is called.
            /// </summary>
            /// <param name="disposing"></param>
            protected virtual void Dispose(bool isDisposing)
            {
                if (1 == Interlocked.Exchange(ref _isDisposed, 1))
                    return;

                if (isDisposing)
                {
                    this.wait.Close();
                }
                this.wait = null;
            }

            /// <summary>
            /// Make sure the thread is stopped when dispose is called.
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion IDisposable implementation
        }
    }

    public interface IMessageThread
    {
        void Push(object message);
        void SendSyncMessage(object message);
    }

    public class MessageThreadNewArgs : EventArgs
    {
        public string name;
        public Thread internalThread;
    }
}
