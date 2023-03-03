using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FiftyOne.Caching.Tests.Loaders
{
    internal abstract class TrackingLoaderBase<TKey, TValue> : IValueTaskLoader<TKey, TValue>, IValueLoader<TKey, TValue>
    {
        private readonly int _delayMillis;

        private volatile int _calls = 0;

        private volatile int _cancels = 0;

        public int Calls => _calls;

        public int Cancels => _cancels;

        public TrackingLoaderBase() : this(0)
        {

        }

        public TrackingLoaderBase(int delayMillis)
        {
            _delayMillis = delayMillis;
        }


        public Task<TValue> Load(TKey key, CancellationToken token)
        {
            Interlocked.Increment(ref _calls);
                return Task.Run(() =>
                {
                    if (_delayMillis > 0)
                    {
                        var start = DateTime.Now;
                        while (DateTime.Now < start.AddMilliseconds(_delayMillis) &&
                            token.IsCancellationRequested == false)
                        {
                            Thread.Sleep(1);
                        }
                        if (token.IsCancellationRequested)
                        {
                            Interlocked.Increment(ref _cancels);
                            throw new OperationCanceledException();
                        }
                    }
                    return GetValue(key);
                });
        }

        public TValue Load(TKey key)
        {
            Interlocked.Increment(ref _calls);
            if (_delayMillis > 0)
            {
                var start = DateTime.Now;
                while (DateTime.Now < start.AddMilliseconds(_delayMillis))
                {
                    Thread.Sleep(1);
                }
            }
            return GetValue(key);
        }

        protected abstract TValue GetValue(TKey key);
    }
}
