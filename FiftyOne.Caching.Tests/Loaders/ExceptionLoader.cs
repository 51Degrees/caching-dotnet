using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FiftyOne.Caching.Tests.Loaders
{
    /// <summary>
    /// Loader that throws an exception.
    /// </summary>
    /// <typeparam name="T">
    /// Type for the key and value.
    /// </typeparam>
    internal class ExceptionLoader<T> : IValueTaskLoader<T, T>, IValueLoader<T, T>
    {
        private volatile int _calls = 0;

        private readonly string _message;

        public int Calls => _calls;

        public ExceptionLoader(string message)
        {
            _message = message;
        }

        public Task<T> Load(T key, CancellationToken token)
        {
            Interlocked.Increment(ref _calls);
            return Task.FromException<T>(new Exception(_message));
        }

        public T Load(T key)
        {
            Interlocked.Increment(ref _calls);
            throw new Exception(_message);
        }
    }
}
