using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FiftyOne.Caching.Tests.Loaders
{
    /// <summary>
    /// Basic test loader that just returns the key as the value.
    /// Calls to the loader are tracked, as are cancellations.
    /// A delay can be provided at construction. This will be the time
    /// the Task takes before completing, or the time for the load method
    /// to return if using the ILoaderValue interface method.
    /// </summary>
    /// <typeparam name="T">
    /// Type for the key and value.
    /// </typeparam>
    internal class ReturnKeyLoader<T> : TrackingLoaderBase<T, T>
    {
        public ReturnKeyLoader()
        {
        }

        public ReturnKeyLoader(int delayMillis) : base(delayMillis)
        {
        }

        protected override T GetValue(T key)
        {
            return key;
        }
    }
}
