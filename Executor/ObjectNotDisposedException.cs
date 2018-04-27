using System;

namespace Executor
{
    /// <summary>
    /// Represents error which occur when the object of class implementing <see cref="IDisposable"/>
    /// is not disposed properly.
    /// </summary>
    public class ObjectNotDisposedException : Exception
    {
        public ObjectNotDisposedException(string message): base(message)
        {
        }
    }
}