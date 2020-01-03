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

using System;

namespace FiftyOne.Caching
{
    public interface IPutCacheBuilder<TBuilder> : ICacheBuilder
        where TBuilder : IPutCacheBuilder<TBuilder>
    {
        /// <summary>
        /// Set whether or not an existing item in the cache should be updated
        /// with the value given to the put method. By default this is false,
        /// meaning that if put is called for a key which already exists in the
        /// cache, the existing value is kept and there is no result to the put
        /// method
        /// </summary>
        /// <param name="update">
        /// True if existing items should be updated by the put method.
        /// </param>
        /// <returns>
        /// This builder.
        /// </returns>
        TBuilder SetUpdateExisting(bool update);

        new IPutCache<K, V> Build<K, V>(int cacheSize);
    }

    /// <summary>
    /// Interface that combines the put cache builder and LRU cache 
    /// builder interfaces.
    /// </summary>
    public interface ILruPutCacheBuilder : 
        IPutCacheBuilder<ILruPutCacheBuilder>,
        ILruCacheBuilder<ILruPutCacheBuilder>
    {
    }
}
