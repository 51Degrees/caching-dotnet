using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace FiftyOne.Caching
{
    public interface ILoadingDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        TValue this[TKey key, CancellationToken cancellationToken] { get; }
        IEnumerable<TKey> Keys { get; }
        IEnumerable<TValue> Values { get; }


        bool ContainsKey(TKey key);

        bool TryGet(TKey key, CancellationToken cancellationToken, out TValue value);
    }
}
