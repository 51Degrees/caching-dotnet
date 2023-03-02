using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FiftyOne.Caching
{
    /// <summary>
    /// Implementation of <see cref="ILoadingDictionary{TKey, TValue}"/>.
    /// This implementation fulfills the expectations described in the
    /// interface description.
    /// As per the source (https://referencesource.microsoft.com/#mscorlib/system/Collections/Concurrent/ConcurrentDictionary.cs,2f8bcdfbad10304f)
    /// <see cref="ConcurrentDictionary{TKey, TValue}.GetOrAdd(TKey, Func{TKey, TValue})"/>
    /// is not guaranteed to only call the factory once. Hence, we wrap the
    /// Tasks in a <see cref="Lazy{T}"/>, meaning the value doesn't get
    /// evaluated until it is read.
    /// </summary>
    /// <typeparam name="TKey">
    /// Type of the key.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// Type of the values stored against the keys.
    /// </typeparam>
    public class LoadingDictionary<TKey, TValue> : ILoadingDictionary<TKey, TValue>
        where TValue : class
    {
        /// <summary>
        /// Internal dictionary of loaded values.
        /// </summary>
        private readonly ConcurrentDictionary<TKey, Lazy<Task<TValue>>> _dictionary;

        /// <summary>
        /// The loader to use when a key is not already present.
        /// </summary>
        private readonly IValueTaskLoader<TKey, TValue> _loader;

        /// <summary>
        /// Logger to use.
        /// </summary>
        private readonly ILogger<LoadingDictionary<TKey, TValue>> _logger;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger">
        /// The logger to use for messages.
        /// </param>
        /// <param name="loader">
        /// The loader to use when loading values.
        /// </param>
        /// <param name="initial">
        /// Collection of initial values to pre-populate the dictionary with.
        /// </param>
        /// <param name="concurrencyLevel">
        /// The estimated number of threads that will fetch from the dictionary
        /// concurrently.
        /// </param>
        /// <param name="capacity">
        /// The initial number of elements that the can contain.
        /// </param>
        public LoadingDictionary(
            ILogger<LoadingDictionary<TKey, TValue>> logger,
            IValueTaskLoader<TKey, TValue> loader,
            IEnumerable<KeyValuePair<TKey, TValue>> initial,
            int concurrencyLevel,
            int capacity)
            : this(logger, loader, concurrencyLevel, capacity)
        {
            if (initial != null)
            {
                foreach (var pair in initial)
                {
                    _dictionary[pair.Key] = new Lazy<Task<TValue>>(() => Task.FromResult(pair.Value));
                }
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger">
        /// The logger to use for messages.
        /// </param>
        /// <param name="loader">
        /// The loader to use when loading values.
        /// </param>
        /// <param name="initial">
        /// Collection of initial values to pre-populate the dictionary with.
        /// </param>
        public LoadingDictionary(
            ILogger<LoadingDictionary<TKey, TValue>> logger,
            IValueTaskLoader<TKey, TValue> loader,
            IEnumerable<KeyValuePair<TKey, TValue>> initial)
            : this(logger, loader, initial, Environment.ProcessorCount, 50000) // todo move to constant defaults
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger">
        /// The logger to use for messages.
        /// </param>
        /// <param name="loader">
        /// The loader to use when loading values.
        /// </param>
        /// <param name="concurrencyLevel">
        /// The estimated number of threads that will fetch from the dictionary
        /// concurrently.
        /// </param>
        /// <param name="capacity">
        /// The initial number of elements that the can contain.
        /// </param>
        public LoadingDictionary(
            ILogger<LoadingDictionary<TKey, TValue>> logger,
            IValueTaskLoader<TKey, TValue> loader,
            int concurrencyLevel,
            int capacity)
        {
            _logger = logger;
            _dictionary = new ConcurrentDictionary<TKey, Lazy<Task<TValue>>>(
                concurrencyLevel,
                capacity);
            _loader = loader;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger">
        /// The logger to use for messages.
        /// </param>
        /// <param name="loader">
        /// The loader to use when loading values.
        /// </param>
        public LoadingDictionary(
            ILogger<LoadingDictionary<TKey, TValue>> logger,
            IValueTaskLoader<TKey, TValue> loader)
            : this(logger, loader, null, Environment.ProcessorCount, 50000) // todo move to constant defaults
        {
        }

        /// <summary>
        /// Gets the value associated with the key, either from an existing
        /// entry, or by loading it first.
        /// </summary>
        /// <param name="key">
        /// Key in the dictionary.
        /// </param>
        /// <param name="cancellationToken">
        /// Token used to cancel the load operation.
        /// </param>
        /// <returns>
        /// Value for the key provided.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// If the key was not already in the dictionary, and could not be loaded
        /// by the loader. The exception from the loader will be the inner
        /// exception.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If the token cancels the operation.
        /// </exception>
        public TValue this[TKey key, CancellationToken cancellationToken]
        {
            get
            {
                try
                {
                    return Get(key, cancellationToken);
                }
                catch (OperationCanceledException e)
                {
                    // The exception was from cancellation, so re-throw.
                    throw e;
                }
                catch (Exception e)
                {
                    // The exception was not a result of cancellation, so wrap
                    // the exception in a KeyNotFound.
                    if (e.GetType() == typeof(AggregateException) && (e as AggregateException).InnerExceptions.Count == 1)
                    {
                        // The exception was an AggregateException thrown from
                        // a Wait. So if there is only one, cut out the AggregateException.
                        e = e.InnerException;
                    }
                    throw new KeyNotFoundException(
                        "An exception occurred in the loader while trying to load the value.",
                        e);
                }
            }
        }

        /// <summary>
        /// Enumeration of the keys currently loaded.
        /// </summary>
        public IEnumerable<TKey> Keys => _dictionary.Keys;

        /// <summary>
        /// Check whether the key has already been loaded into the dictionary.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Try to get  the value associated with the key, either from an existing
        /// entry, or by loading it first. Rather than throwing an exception, false
        /// is returned to indicate failure.
        /// </summary>
        /// <param name="key">
        /// Key in the dictionary.
        /// </param>
        /// <param name="cancellationToken">
        /// Token used to cancel the load operation.
        /// </param>
        /// <param name="value">
        /// Value for the key provided, or null.
        /// </param>
        /// <returns>
        /// True if the get was successful, and value was populated.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// If the operation was canceled through the token.
        /// </exception>
        public bool TryGet(TKey key, CancellationToken cancellationToken, out TValue value)
        {
            try
            {
                value = Get(key, cancellationToken);
                return true;
            }
            catch (OperationCanceledException e)
            {
                throw e;
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }
        
        /// <summary>
        /// Internal get method used by <see cref="this[TKey, CancellationToken]"/>
        /// and <see cref="TryGet(TKey, CancellationToken, out TValue)"/>.
        /// </summary>
        /// <param name="key">
        /// Key in the dictionary.
        /// </param>
        /// <param name="cancellationToken">
        /// Token used to cancel the load operation.
        /// </param>
        /// <returns>
        /// Value for the key provided.
        /// </returns>
        private TValue Get(TKey key, CancellationToken cancellationToken)
        {
            Func<TKey, Lazy<Task<TValue>>> load = (k) =>
            {
                return new Lazy<Task<TValue>>(() => _loader.Load(k, cancellationToken));
            };

            // First try at getting the value.
            var result = _dictionary.GetOrAdd(key, load);
            try
            {
                result.Value.Wait(cancellationToken);
            }
            catch (Exception)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Remove(key);
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            if (result.Value.Status == TaskStatus.RanToCompletion &&
                result.Value.IsFaulted == false)
            {
                return result.Value.Result;
            }
            else
            {
                // The first try failed, but the operation was not canceled, 
                // so have another go.
                Remove(key);
                result = _dictionary.GetOrAdd(key, load);

                try
                {
                    result.Value.Wait(cancellationToken);
                }
                catch (Exception e)
                {
                    Remove(key);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(cancellationToken);
                    }
                    else
                    {
                        // This time we throw the exception as there won't be
                        // another try.
                        throw e;
                    }
                }

                if (result.Value.Status == TaskStatus.RanToCompletion &&
                    result.Value.IsFaulted == false)
                {
                    return result.Value.Result;
                }
                else
                {
                    // Both tries failed, so throw a KeyNotFound.
                    Remove(key);
                    throw new KeyNotFoundException();
                }
            }
        }

        /// <summary>
        /// Try to remove the key from the dictionary.
        /// In the case that removal fails, an error is logged.
        /// </summary>
        /// <param name="key"></param>
        private void Remove(TKey key)
        {
            if (_dictionary.TryRemove(key, out _) == false)
            {
                _logger.LogError($"Failed to remove entry for key '{key}'.");
            }
        }
    }
}
