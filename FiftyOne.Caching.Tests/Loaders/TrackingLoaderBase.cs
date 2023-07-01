/* *********************************************************************
 * This Original Work is copyright of 51 Degrees Mobile Experts Limited.
 * Copyright 2023 51 Degrees Mobile Experts Limited, Davidson House,
 * Forbury Square, Reading, Berkshire, United Kingdom RG1 3EU.
 *
 * This Original Work is licensed under the European Union Public Licence
 * (EUPL) v.1.2 and is subject to its terms as set out below.
 *
 * If a copy of the EUPL was not distributed with this file, You can obtain
 * one at https://opensource.org/licenses/EUPL-1.2.
 *
 * The 'Compatible Licences' set out in the Appendix to the EUPL (as may be
 * amended by the European Commission) shall be deemed incompatible for
 * the purposes of the Work and the provisions of the compatibility
 * clause in Article 5 of the EUPL shall not apply.
 *
 * If using the Work as, or as part of, a network application, by
 * including the attribution notice(s) required under Article 5 of the EUPL
 * in the end user terms of the application under an appropriate heading,
 * such notice(s) shall fulfill the requirements of that article.
 * ********************************************************************* */

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
