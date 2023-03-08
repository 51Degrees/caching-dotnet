using System;
using System.Collections.Generic;
using System.Text;

namespace FiftyOne.Caching.Tests.Loaders
{
    /// <summary>
    /// Test loader that always returns null.
    /// Calls to the loader are tracked, as are cancellations.
    /// A delay can be provided at construction. This will be the time
    /// the Task takes before completing, or the time for the load method
    /// to return if using the ILoaderValue interface method.
    /// </summary>
    /// <typeparam name="TKey">
    /// Type for the key.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// Type for the value.
    /// </typeparam>
    internal class NullLoader<TKey, TValue> : TrackingLoaderBase<TKey, TValue>
        where TValue : class
    {
        protected override TValue GetValue(TKey key)
        {
            return null;
        }
    }
}
