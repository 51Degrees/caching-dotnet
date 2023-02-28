using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FiftyOne.Caching
{
    public interface IValueTaskLoader<K, V>
    {
        Task<V> Load(K key, CancellationToken token);
    }
}
