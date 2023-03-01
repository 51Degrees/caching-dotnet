using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FiftyOne.Caching.Tests
{
    [TestClass]
    public class LoadingDictionaryTests
    {
        private CancellationTokenSource _token;

        private Mock<ILogger<LoadingDictionary<string, string>>> _logger;

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

            public int Calls => _calls;

            public int Cancels => _cancels;

            private volatile int _calls = 0;

            private volatile int _cancels = 0;

            public Task<T> Load(T key, CancellationToken token)
            {
                Console.WriteLine("loadgin...");
                Interlocked.Increment(ref _calls);
                if (_delayMillis > 0)
                {
                    return Task.Run(() =>
                    {
                        var start = DateTime.Now;
                        while (DateTime.Now < start.AddMilliseconds(_delayMillis) &&
                            token.IsCancellationRequested == false)
                        {
                            Thread.Sleep(1);
                        }
                        if (token.IsCancellationRequested)
                        {
                            Interlocked.Increment(ref _cancels);
                            throw new OperationCanceledException();
                        }
                        return key;
                    });
                }
                else
                {
                    return Task.FromResult(key);
                }
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
                return Task.FromException<T>(new Exception(_message));
            }
        }

        private class UnresponsiveLoader<T> : IValueTaskLoader<T, T>
        {
            private Task<T> _loop = null;
            private readonly object _lock = new object();
            private bool _isCanceled = false;

            public void Terminate()
            {
                _isCanceled = true;
            }

            public Task<T> Load(T key, CancellationToken token)
            {
                if (_loop == null)
                {
                    lock (_lock)
                    {
                        if (_loop == null)
                        {
                            _loop = Task.Run(() =>
                            {
                                while (_isCanceled == false)
                                {
                                    Task.Delay(10);
                                }
                                return key;
                            });
                        }
                    }
                }
                return _loop;
            }
        }

        [TestInitialize]
        public void Init()
        {
            _token = new CancellationTokenSource();
            _logger = new Mock<ILogger<LoadingDictionary<string, string>>>();
        }

        [TestMethod]
        public void LoadingDictionary_GetMiss()
        {
            // Arrange

            var value = "teststring";
            var loader = new ReturnKeyLoader<string>();
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);

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
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);

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
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);

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
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);

            // Act

            _ = dict.TryGet(value, _token.Token, out _);
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
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);
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
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);
            var successes = new ConcurrentDictionary<int, bool>();
            var results = new ConcurrentDictionary<int, string>();

            // Act

            Parallel.For(0, 2, (i) =>
            {
                if (dict.TryGet(value, _token.Token, out var result))
                {
                    results[i] = result;
                }
            });

            // Assert

            Assert.AreEqual(2, results.Values.Count);
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
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);

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
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);

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
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);
            OperationCanceledException exception = null;

            // Act

            var start = DateTime.Now;
            _token.CancelAfter(millis / 2);
            try
            {
                var result = dict[value, _token.Token];
            }
            catch (OperationCanceledException e)
            {
                exception = e;
            }
            var end = DateTime.Now;
            Thread.Sleep(millis);

            // Assert
            Console.WriteLine("checking...");
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
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);

            // Act

            var start = DateTime.Now;
            _token.CancelAfter(millis / 2);
            var success = dict.TryGet(value, _token.Token, out _);
            var end = DateTime.Now;
            Thread.Sleep(millis);

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
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader, values);

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
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader, values);

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
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader, values);

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
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader, values);

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
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);
            OperationCanceledException exception = null;
            var count = 2;

            // Act

            for (int i = 0; i < 2; i++)
            {
                Console.WriteLine(i);
                _token.CancelAfter(millis / 2);

                try
                {
                    _ = dict[value, _token.Token];
                }
                catch (OperationCanceledException e)
                {
                    exception = e;
                    Console.WriteLine("exception...");
                }
                Assert.IsNotNull(exception);
                exception = null;
                Thread.Sleep(millis);
                _token = new CancellationTokenSource();
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
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);
            var count = 2;

            // Act

            for (int i = 0; i < count; i++)
            {
                try
                {
                    _ = dict[value, _token.Token];
                }
                catch { }
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
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);
            var count = 2;

            // Act

            for (int i = 0; i < count; i++)
            {
                _token.CancelAfter(millis / 2);
                var success = dict.TryGet(value, _token.Token, out _);
                Assert.IsFalse(success);
                Thread.Sleep(millis);
                _token = new CancellationTokenSource();
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
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);
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

        [TestMethod]
        public void LoadingDictionary_GetRemoveUnresponsive()
        {
            // Arrange

            var value = "testvalue";
            var loader = new UnresponsiveLoader<string>();
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);

            // Act

            var getter = Task.Run(() =>
            {
                _ = dict[value, _token.Token];
            });
            _token.Cancel();
            try
            {
                getter.Wait(10);
            }
            catch (AggregateException) { }

            // Assert

            Assert.IsTrue(getter.IsCompleted);
            Assert.IsTrue(getter.IsFaulted);
            Assert.AreEqual(0, dict.Keys.Count());

            // Cleanup

            loader.Terminate();
        }

        [TestMethod]
        public void LoadingDictionary_TryGetRemoveUnresponsive()
        {
            // Arrange

            var value = "testvalue";
            var loader = new UnresponsiveLoader<string>();
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);

            // Act

            var getter = Task.Run(() =>
            {
                return dict.TryGet(value, _token.Token, out _);
            });
            _token.Cancel();
            try
            {
                getter.Wait(10);
            }
            catch { }

            // Assert

            Assert.IsTrue(getter.IsCompleted);
            Assert.IsFalse(getter.Result);
            Assert.AreEqual(0, dict.Keys.Count());

            // Cleanup

            loader.Terminate();
        }
    }
}
