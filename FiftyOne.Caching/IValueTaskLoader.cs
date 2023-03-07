using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FiftyOne.Caching
{
    /// <summary>
    /// Similar to the <see cref="IValueLoader{K, V}"/> interface, but intended
    /// for thread-safe operation using Tasks.
    /// </summary>
    /// <typeparam name="TKey">
    /// Type of the key.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// Type of the values stored against the keys.
    /// </typeparam>
    public interface IValueTaskLoader<K, V>
    {
        /// <summary>
        /// Load the value for the key provided. The method should return as
        /// fast as possible, and leave all the loading logic to be run within
        /// the task that is returned.
        /// </summary>
        /// <param name="key">
        /// Key in the dictionary.
        /// </param>
        /// <param name="token">
        /// Token used to cancel the task that's returned.
        /// </param>
        /// <returns>
        /// Running task which will result in the value for the key provided.
        /// </returns>
        Task<V> Load(K key, CancellationToken token);
    }
}
