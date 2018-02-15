using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Bee.Eee.Utility.Threading
{
    /// <summary>
    /// Multiple Producer Single Consumer queue that is thread-safe and fast
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MPSCQueue<T> : IMPSCQueue<T>
    {
        private QueueItem _enqueueHere;
        private QueueItem _dequeueHere;
        private object _lock;
        private AutoResetEvent _itemEnqueued;

        public WaitHandle ItemEnqueued { get { return _itemEnqueued; } } 

        /// <summary>
        /// The size of the data segments to assemble
        /// </summary>
        public MPSCQueue()
        {
            _enqueueHere = new QueueItem();
            _dequeueHere = _enqueueHere;
            _itemEnqueued = new AutoResetEvent(false);
            _lock = new object();
        }

        /// <summary>
        /// Place data within the queue
        /// </summary>
        /// <param name="item">The data to be placed</param>
        public void Enqueue(T item)
        {
            lock( _lock )
            {
                var oldHead = _enqueueHere;
                _enqueueHere.Item = item;
                _enqueueHere.Next = new QueueItem();
                _enqueueHere = _enqueueHere.Next;
                oldHead.Ready = true;
            }

            _itemEnqueued.Set();
        }

        /// <summary>
        /// Pulls off a read item.  Will return null if no segment is available
        /// </summary>
        /// <returns></returns>
        public bool TryDequeue( out T result )
        {
            if (_dequeueHere.Ready)
            {
                // get value
                result = _dequeueHere.Item;

                // advance to the next item
                _dequeueHere = _dequeueHere.Next;
                
                return true;
            }
            else
            {
                result = default(T);
                return false;
            }
        }

        public bool TryPeek(out T result)
        {
            if (_dequeueHere.Ready)
            {
                // get value
                result = _dequeueHere.Item;                

                return true;
            }
            else
            {
                result = default(T);
                return false;
            }
        }    
            

        private class QueueItem
        {
            public T Item;
            public volatile bool Ready;
            public QueueItem Next;

            public QueueItem()
            {
                
            }
        }

        private int _isDisposed = 0;
        public bool IsDisposed { get { return 1 == Interlocked.Add(ref _isDisposed, 0); } }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (1 == Interlocked.Exchange(ref _isDisposed, 1))
                return;

            if (isDisposing)
            {
                if (null != _itemEnqueued)
                    _itemEnqueued.Dispose();

                _itemEnqueued = null;
                _dequeueHere = null;
                _enqueueHere = null;
            }
        }
    }

    public interface IMPSCQueue<T> : IDisposable
    {
        /// <summary>
        /// Handle to wait until an item is queued up
        /// </summary>
        WaitHandle ItemEnqueued { get; }

        /// <summary>
        /// Thread safe enqueu.
        /// </summary>
        /// <param name="item"></param>
        void Enqueue(T item);
        /// <summary>
        /// Removes next item from the queue and returns it.  The Dequeue must only happen by one consumer, inotherwords it's not thread-safe, but must be synchronized.
        /// </summary>
        /// <param name="result">The item removed from the queue</param>
        /// <returns></returns>
        bool TryDequeue(out T result);
        /// <summary>
        /// Get the last item without removing it from the queue.  This is not thread-safe and should be synchronized with any calls to TryDequeue
        /// </summary>
        /// <param name="result">The next item that can come off the queue.</param>
        /// <returns></returns>
        bool TryPeek(out T result);        
    }
}
