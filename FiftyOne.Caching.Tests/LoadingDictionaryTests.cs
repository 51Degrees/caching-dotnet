using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FiftyOne.Caching.Tests
{
    [TestClass]
    public class LoadingDictionaryTests
    {
        private CancellationTokenSource _token;
 
        private class ReturnKeyLoader<T> : IValueTaskLoader<T, T>
        {
            private readonly int _delayMillis;

            public ReturnKeyLoader() : this(0)
            {

            }

            public ReturnKeyLoader(int delayMillis)
            {
                _delayMillis = delayMillis;
            }

            public int Calls { get; private set; } = 0;

            public int Cancels { get; private set; } = 0;

            public Task<T> Load(T key, CancellationToken token)
            {
                Calls++;
                if (_delayMillis > 0)
                {
                    Task.Delay(_delayMillis, token)
                        .Wait();
                }
                if (token.IsCancellationRequested)
                {
                    Cancels++;
                }
                return Task.FromResult(key);
            }
        }

        private class ExceptionLoader<T> : IValueTaskLoader<T, T>
        {
            public int Calls { get; private set; } = 0;

            private readonly string _message;

            public ExceptionLoader(string message)
            {
                _message = message;
            }

            public Task<T> Load(T key, CancellationToken token)
            {
                Calls++;
                throw new Exception(_message);
            }
        }

        [TestInitialize]
        public void Init()
        {
            _token = new CancellationTokenSource();
        }

        [TestMethod]
        public void LoadingDictionary_GetMiss()
        {
            // Arrange

            var value = "teststring";
            var loader = new ReturnKeyLoader<string>();
            var dict = new LoadingDictionary<string, string>(loader);

            // Act

            var result = dict[value, _token.Token];

            // Assert

            Assert.IsNotNull(result);
            Assert.AreEqual(value, result);
            Assert.AreEqual(1, loader.Calls);
        }

        [TestMethod]
        public void LoadingDictionary_TryGetMiss()
        {
            // Arrange

            var value = "teststring";
            var loader = new ReturnKeyLoader<string>();
            var dict = new LoadingDictionary<string, string>(loader);

            // Act

            var success = dict.TryGet(value, _token.Token, out var result);

            // Assert

            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.AreEqual(value, result);
            Assert.AreEqual(1, loader.Calls);
        }

        [TestMethod]
        public void LoadingDictionary_GetHit()
        {
            // Arrange

            var value = "teststring";
            var loader = new ReturnKeyLoader<string>();
            var dict = new LoadingDictionary<string, string>(loader);

            // Act

            var _ = dict[value, _token.Token];
            var result = dict[value, _token.Token];

            // Assert

            Assert.IsNotNull(result);
            Assert.AreEqual(value, result);
            Assert.AreEqual(1, loader.Calls);
        }

        [TestMethod]
        public void LoadingDictionary_TryGetHit()
        {
            // Arrange

            var value = "teststring";
            var loader = new ReturnKeyLoader<string>();
            var dict = new LoadingDictionary<string, string>(loader);

            // Act

            var _ = dict.TryGet(value, _token.Token, out _);
            var success = dict.TryGet(value, _token.Token, out var result);

            // Assert

            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.AreEqual(value, result);
            Assert.AreEqual(1, loader.Calls);
        }

        [TestMethod]
        public void LoadingDictionary_GetConcurrent()
        {
            // Arrange

            var value = "teststring";
            var loader = new ReturnKeyLoader<string>();
            var dict = new LoadingDictionary<string, string>(loader);
            var results = new ConcurrentDictionary<int, string>();

            // Act

            Parallel.For(0, 1, (i) =>
            {
                var result = dict[value, _token.Token];
                results[i] = result;
            });

            // Assert

            foreach (var result in results)
            {
                Assert.AreEqual(value, result.Value);
            }
            Assert.AreEqual(1, loader.Calls);
        }

        [TestMethod]
        public void LoadingDictionary_TryGetConcurrent()
        {
            // Arrange

            var value = "teststring";
            var loader = new ReturnKeyLoader<string>();
            var dict = new LoadingDictionary<string, string>(loader);
            var successes = new ConcurrentDictionary<int, bool>();
            var results = new ConcurrentDictionary<int, string>();

            // Act

            Parallel.For(0, 1, (i) =>
            {
                if (dict.TryGet(value, _token.Token, out var result))
                {
                    results[i] = result;
                }
            });

            // Assert

            Assert.Equals(2, results.Values.Count);
            foreach (var result in results)
            {
                Assert.AreEqual(value, result.Value);
            }
            Assert.AreEqual(1, loader.Calls);
        }

        [TestMethod]
        public void LoadingDictionary_GetFails()
        {
            // Arrange

            var message = "exceptionmessage";
            var value = "teststring";
            var loader = new ExceptionLoader<string>(message);
            var dict = new LoadingDictionary<string, string>(loader);

            // Act
            
            Exception exception = null;
            try
            {
                _ = dict[value, _token.Token];
            }
            catch (Exception e)
            {
                exception = e;
            }

            // Assert

            Assert.IsNotNull(exception);
            Assert.IsNotNull(exception.InnerException);
            Assert.AreEqual(message, exception.InnerException.Message);
        }

        [TestMethod]
        public void LoadingDictionary_TryGetFails()
        {
            // Arrange

            var message = "exceptionmessage";
            var value = "teststring";
            var loader = new ExceptionLoader<string>(message);
            var dict = new LoadingDictionary<string, string>(loader);

            // Act
            
            var success = dict.TryGet(value, _token.Token, out _);

            // Assert

            Assert.IsFalse(success);
        }

        [TestMethod]
        public void LoadingDictionary_GetCancelled()
        {
            // Arrange

            var millis = 100;
            var value = "teststring";
            var loader = new ReturnKeyLoader<string>(millis);
            var dict = new LoadingDictionary<string, string>(loader);
            TaskCanceledException exception = null;

            // Act

            var start = DateTime.Now;
            _token.CancelAfter(millis / 2);
            try
            {
                var result = dict[value, _token.Token];
            }
            catch (TaskCanceledException e)
            {
                exception = e;
            }
            var end = DateTime.Now;

            // Assert

            Assert.IsTrue((end - start).TotalMilliseconds < millis);
            Assert.AreEqual(1, loader.Cancels);
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        public void LoadingDictionary_TryGetCancelled()
        {
            // Arrange

            var millis = 100;
            var value = "teststring";
            var loader = new ReturnKeyLoader<string>(millis);
            var dict = new LoadingDictionary<string, string>(loader);

            // Act

            var start = DateTime.Now;
            _token.CancelAfter(millis / 2);
            var success = dict.TryGet(value, _token.Token, out _);
            var end = DateTime.Now;

            // Assert

            Assert.IsTrue((end - start).TotalMilliseconds < millis);
            Assert.AreEqual(1, loader.Cancels);
            Assert.IsFalse(success);
        }

        [TestMethod]
        public void LoadingDictionary_GetPreloaded()
        {
            // Arrange

            var values = new Dictionary<string, string>()
            {
                { "one", "one" },
                { "two", "two" },
                { "three", "three" }
            };
            var loader = new ReturnKeyLoader<string>();
            var dict = new LoadingDictionary<string, string>(loader, values);

            // Act

            foreach (var value in values)
            {
                var result = dict[value.Key, _token.Token];
                Assert.AreEqual(value.Value, result);
            }

            // Assert

            Assert.AreEqual(0, loader.Calls);
        }

        [TestMethod]
        public void LoadingDictionary_TryGetPreloaded()
        {
            // Arrange

            var values = new Dictionary<string, string>()
            {
                { "one", "one" },
                { "two", "two" },
                { "three", "three" }
            };
            var loader = new ReturnKeyLoader<string>();
            var dict = new LoadingDictionary<string, string>(loader, values);

            // Act

            foreach (var value in values)
            {
                var success = dict.TryGet(value.Key, _token.Token, out var result);
                Assert.IsTrue(success);
                Assert.AreEqual(value.Value, result);
            }

            // Assert

            Assert.AreEqual(0, loader.Calls);
        }

        [TestMethod]
        public void LoadingDictionary_GetNotPreloaded()
        {
            // Arrange

            var values = new Dictionary<string, string>()
            {
                { "one", "one" },
                { "two", "two" },
                { "three", "three" }
            };
            var loader = new ReturnKeyLoader<string>();
            var dict = new LoadingDictionary<string, string>(loader, values);

            // Act

            foreach (var value in values)
            {
                var result = dict[value.Key, _token.Token];
                Assert.AreEqual(value.Value, result);
            }
            var result2 = dict["four", _token.Token];

            // Assert

            Assert.AreEqual(1, loader.Calls);
            Assert.AreEqual("four", result2);
        }

        [TestMethod]
        public void LoadingDictionary_TryGetNotPreloaded()
        {
            // Arrange

            var values = new Dictionary<string, string>()
            {
                { "one", "one" },
                { "two", "two" },
                { "three", "three" }
            };
            var loader = new ReturnKeyLoader<string>();
            var dict = new LoadingDictionary<string, string>(loader, values);

            // Act

            foreach (var value in values)
            {
                var success = dict.TryGet(value.Key, _token.Token, out var result);
                Assert.IsTrue(success);
                Assert.AreEqual(value.Value, result);
            }
            var success2 = dict.TryGet("four", _token.Token, out var result2);

            // Assert

            Assert.AreEqual(1, loader.Calls);
            Assert.IsTrue(success2);
            Assert.AreEqual("four", result2);
        }

        [TestMethod]
        public void LoadingDictionary_GetCanceledIsRemoved()
        {
            // Arrange

            var value = "testvalue";
            var millis = 100;
            var loader = new ReturnKeyLoader<string>(millis);
            var dict = new LoadingDictionary<string, string>(loader);
            var count = 2;

            // Act

            for (int i = 0; i < count; i++)
            {
                _token.CancelAfter(millis / 2);
                Assert.ThrowsException<TaskCanceledException>(() => dict[value, _token.Token]);
            }

            // Assert

            Assert.AreEqual(count, loader.Calls);
            Assert.AreEqual(count, loader.Cancels);
        }

        [TestMethod]
        public void LoadingDictionary_GetFailedIsRemoved()
        {
            // Arrange

            var value = "testvalue";
            var loader = new ExceptionLoader<string>("some exception message");
            var dict = new LoadingDictionary<string, string>(loader);
            var count = 2;

            // Act

            for (int i = 0; i < count; i++)
            {
                Assert.ThrowsException<Exception>(() => dict[value, _token.Token]);
            }

            // Assert

            Assert.AreEqual(count, loader.Calls);
        }

        [TestMethod]
        public void LoadingDictionary_TryGetCanceledIsRemoved()
        {
            // Arrange

            var value = "testvalue";
            var millis = 100;
            var loader = new ReturnKeyLoader<string>(millis);
            var dict = new LoadingDictionary<string, string>(loader);
            var count = 2;

            // Act

            for (int i = 0; i < count; i++)
            {
                _token.CancelAfter(millis / 2);
                var success = dict.TryGet(value, _token.Token, out _);
                Assert.IsFalse(success);
            }

            // Assert

            Assert.AreEqual(count, loader.Calls);
            Assert.AreEqual(count, loader.Cancels);
        }

        [TestMethod]
        public void LoadingDictionary_TryGetFailedIsRemoved()
        {
            // Arrange

            var value = "testvalue";
            var loader = new ExceptionLoader<string>("some exception message");
            var dict = new LoadingDictionary<string, string>(loader);
            var count = 2;

            // Act

            for (int i = 0; i < count; i++)
            {
                var succes = dict.TryGet(value, _token.Token, out _);
                Assert.IsFalse(succes);
            }

            // Assert

            Assert.AreEqual(count, loader.Calls);
        }

        private void GetIndexer<K, V>(
            ILoadingDictionary<K, V> dict,
            K key,
            Type exceptionType,
            out V value)
        {
            V tmpValue = default;
            Exception exception = null;
            try
            { 
                tmpValue = dict[key, _token.Token];
            }
            catch (Exception e)
            {
                exception = e;
            }

            if (exceptionType != null)
            {
                Assert.IsNotNull(exception);
                Assert.AreEqual(exceptionType, exception.GetType());
            }
            else
            {
                Assert.IsNull(exception);
            }
            value = tmpValue;
        }

        private void GetTryer<K, V>(
            ILoadingDictionary<K, V> dict,
            K key,
            Type exceptionType,
            out V value)
        {
            var result = dict.TryGet(key, _token.Token, out value);
            if (exceptionType != null)
            {
                Assert.IsFalse(result);
            }
            else
            {
                Assert.IsTrue(result);
            }
        }
    }
}
