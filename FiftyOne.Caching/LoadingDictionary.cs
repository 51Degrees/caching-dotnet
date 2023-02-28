using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace FiftyOne.Caching
{
    public class LoadingDictionary<TKey, TValue> : ILoadingDictionary<TKey, TValue>
    {
        public LoadingDictionary(IValueTaskLoader<TKey, TValue> loader)
        {

        }

        public LoadingDictionary(IValueTaskLoader<TKey, TValue> loader, IDictionary<TKey, TValue> initial)
        {

        }

        public TValue this[TKey key, CancellationToken cancellationToken] => throw new NotImplementedException();

        public IEnumerable<TKey> Keys => throw new NotImplementedException();

        public IEnumerable<TValue> Values => throw new NotImplementedException();

        public bool ContainsKey(TKey key)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool TryGet(TKey key, CancellationToken cancellationToken, out TValue value)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
