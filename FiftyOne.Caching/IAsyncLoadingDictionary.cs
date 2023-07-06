using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FiftyOne.Caching
{
    /// <summary>
    /// Read only dictionary which loads values lazily as keys are requested.
    /// The intended implementation of this interface uses Tasks internally to
    /// ensure that concurrent requests for the same key, only result in the
    /// loader being called once for that key. When the value for a given key
    /// cannot be loaded by the loader, a <see cref="KeyNotFoundException"/>
    /// is thrown, with the inner exception being any exception thrown by the
    /// loader.
    /// This interface should always be implemented in a thread-safe manner.
    /// Any implementation should allow for pre-populating the dictionary by
    /// providing an initial collection to the constructor.
    /// </summary>
    /// <typeparam name="TKey">
    /// Type of the key.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// Type of the values stored against the keys.
    /// </typeparam>
    public interface IAsyncLoadingDictionary<TKey, TValue> : ILoadingDictionary<TKey, TValue>
        where TValue : class
    {
        Task<TValue> GetAsync(TKey key, CancellationToken cancellationToken);
    }
}
