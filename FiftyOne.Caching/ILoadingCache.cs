/* *********************************************************************
 * This Original Work is copyright of 51 Degrees Mobile Experts Limited.
 * Copyright 2019 51 Degrees Mobile Experts Limited, 5 Charlotte Close,
 * Caversham, Reading, Berkshire, United Kingdom RG4 7BY.
 *
 * This Original Work is licensed under the European Union Public Licence (EUPL) 
 * v.1.2 and is subject to its terms as set out below.
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


using System.Collections.Generic;

namespace FiftyOne.Caching
{
    /// <summary>
    /// Extension of general cache contract to provide for getting a value with
    /// a particular value loaded. Primarily used to allow the value loader to 
    /// be an already instantiated value of the type V to avoid construction
    /// costs of that value.
    /// (In other words the loader has the signature. " where V : IValueLoader").
    /// Used only in UA Matching.
    /// </summary>
    /// <typeparam name="K">
    /// The type of the key for the data being loaded
    /// </typeparam>
    /// <typeparam name="V">
    /// The type of the data being loaded
    /// </typeparam>
    public interface ILoadingCache<K, V> : ICache<K, V>
    {
        /// <summary>
        /// Get the value using the specified key and calling the specified loader 
        /// if needed.
        /// </summary>
        /// <param name="key">The key of the value to load</param>
        /// <param name="loader">
        /// The loader to use when getting the value
        /// </param>
        /// <returns>
        /// The value from the cache, or loader if not available.
        /// </returns>
        V this[K key, IValueLoader<K, V> loader] { get; }

        /// <summary>
        /// Warm the cache with an initial set of keys.
        /// The size of the cache must be large enough to contain all
        /// of the item in the initial collection.
        /// </summary>
        /// <param name="intial">
        /// The collection to add to the cache.
        /// </param>
        void Warm(IEnumerable<K> initial);
    }
}
