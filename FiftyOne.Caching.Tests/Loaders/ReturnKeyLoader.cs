/* *********************************************************************
 * This Original Work is copyright of 51 Degrees Mobile Experts Limited.
 * Copyright 2025 51 Degrees Mobile Experts Limited, Davidson House,
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

        public ReturnKeyLoader(int delayMillis, bool runWithToken) : base(delayMillis, runWithToken)
        {
        }

        public ReturnKeyLoader(
            int delayMillis,
            Func<CancellationToken, CancellationToken> taskCancellationTokenProvider,
            Func<CancellationToken, CancellationToken> loopCancellationTokenProvider) :
            base(delayMillis, taskCancellationTokenProvider, loopCancellationTokenProvider)
        {
        }

        protected override T GetValue(T key)
        {
            return key;
        }
    }
}
