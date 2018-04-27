using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Executor
{
    /// <summary>
    /// Executes the tasks at any time by any number of clients.
    /// </summary>
    public sealed class Executor : IDisposable
    {
        private ConcurrentQueue<Action> _queue;
        private Thread _thread;
        private CancellationTokenSource _cancellation;
        private AutoResetEvent _go;
        private bool _disposed;

        /// <summary>
        /// Constructor.
        /// </summary>
        public Executor()
        {
            _queue = new ConcurrentQueue<Action>();
            _go = new AutoResetEvent(false);
            _cancellation = new CancellationTokenSource();
            _thread = new Thread(Process)
            {
                IsBackground = true
            };
            _thread.Start();
        }

        /// <summary>
        /// Adds a not-null task to be executed at some point.
        /// It's expected that the consuming code should handle any exception. If it's not the case
        /// the exception is ignored and the next task (if any) is executed.
        /// </summary>
        public void AddForExecution(Action task)
        {
            CheckDisposed();

            if (task == null)
                throw new ArgumentNullException(nameof(task));
           
            _queue.Enqueue(task);
            _go.Set();
        }

        /// <summary>
        /// Disposes the allocated resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _cancellation.Cancel();
            _go.Set();
            _thread.Join(); // wait for the thread completion to dispose dependencies

            _cancellation.Dispose();
            _go.Dispose();
            _disposed = true;
#if DEBUG
            // https://stackoverflow.com/a/32882853
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~Executor()
        {
            throw new ObjectNotDisposedException("You have to dispose the object correctly.");
#endif
        }

        /// <summary>
        /// Throws <see cref="ObjectDisposedException"/> when the object is disposed.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(null);
        }

        /// <summary>
        /// Processes the incoming tasks in the endless loop until cancellation is requested.
        /// </summary>
        private void Process()
        {
            Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} started.");
            try
            {
                while (true)
                {
                    if (_cancellation.IsCancellationRequested) return;

                    if (_queue.TryDequeue(out Action task))
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
                    {
                        _go.WaitOne();
                    }
                }
            }
            finally
            {
                Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} finished.");
            }
        }
    }
}