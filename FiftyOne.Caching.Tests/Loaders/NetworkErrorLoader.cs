using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FiftyOne.Caching.Tests.Loaders
{
    internal class NetworkErrorLoader<T> : TrackingLoaderBase<T, T>
    {
        protected override T GetValue(T key)
        {
            throw new WebException("Network failure occurred");
        }
    }
}
