using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Executor
{
    /// <summary>
    /// Executes the tasks at any time by any number of clients.
    /// </summary>
    public sealed class Executor : IDisposable
    {
        private Queue<Action> _queue;
        private Task _task;
        private CancellationTokenSource _cancellation;
        private AutoResetEvent _go;
        private object _lock = new object();

        /// <summary>
        /// Constructor.
        /// </summary>
        public Executor()
        {
            _queue = new Queue<Action>();
            _go = new AutoResetEvent(false);
            _task = Task.Run((Action)Process);
            _cancellation = new CancellationTokenSource();
        }

        /// <summary>
        /// Adds a not-null task to be executed at some point.
        /// It's expected that the consuming code should handle any exception. If it's not the case
        /// the exception is ignored and the next task (if any) is executed.
        /// </summary>
        public void AddForExecution(Action task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));
            if (_cancellation.IsCancellationRequested)
                throw new InvalidOperationException("Unable to add tasks after disposal.");

            lock (_lock)
                _queue.Enqueue(task);
            _go.Set();
        }

        /// <summary>
        /// Disposes the allocated resources.
        /// </summary>
        /// <param name="disposing"><c>True</c> if called via <see cref="Dispose()"/> method.</param>
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
            if (!_cancellation.IsCancellationRequested)
            {
                _cancellation.Cancel();
                _go.Set();
                _task.Wait(); // Allow the worker thread to handle cancellation
            }
        }

        /// <summary>
        /// Disposes allocated resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~Executor()
        {
            Dispose(false);
        }

        /// <summary>
        /// Processes the incoming tasks in the endless loop until cancellation is requested.
        /// </summary>
        private void Process()
        {
            while (true)
            {
                if (_cancellation.IsCancellationRequested) return;
                Action task = null;
                lock (_lock)
                {
                    if (_queue.Count > 0)
                        task = _queue.Dequeue();
                }

                if (task != null)
                {
                    try
                    {
                        task();
                    }
                    catch (Exception)
                    {
                        // It's expected the the client code handles the exceptions.
                        // Here we swallow the exception as a fallback strategy 
                        // to allow the next tasks to be executed.
                    }
                }
                else
                    _go.WaitOne();
            }
        }
    }
}