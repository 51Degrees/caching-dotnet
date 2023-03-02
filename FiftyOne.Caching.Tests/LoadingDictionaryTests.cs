using FiftyOne.Caching.Tests.Loaders;
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

        [TestInitialize]
        public void Init()
        {
            _token = new CancellationTokenSource();
            _logger = new Mock<ILogger<LoadingDictionary<string, string>>>();
        }

        /// <summary>
        /// Test that using the get method on an empty dictionary result in
        /// the load method being called, and the expected value being returned.
        /// </summary>
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
            Assert.AreEqual(1, dict.Keys.Count());
        }

        /// <summary>
        /// Test that using the TryGet method on an empty dictionary result in
        /// the load method being called, and the expected value being returned.
        /// </summary>
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
            Assert.AreEqual(1, dict.Keys.Count());
        }

        /// <summary>
        /// Test that using the get method on a dictionary that already contains
        /// a value for the key does not result in the load method being called,
        /// and the expected value being returned.
        /// </summary>
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
            Assert.AreEqual(1, dict.Keys.Count());
        }

        /// <summary>
        /// Test that using the TryGet method on a dictionary that already contains
        /// a value for the key does not result in the load method being called,
        /// and the expected value being returned.
        /// </summary>
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
            Assert.AreEqual(1, dict.Keys.Count());
        }

        /// <summary>
        /// Test that when multiple threads call the get method with the same
        /// key, where the value is not already loaded, that the load method
        /// is only called once. Also check that all the values returned are
        /// correct.
        /// </summary>
        [TestMethod]
        public void LoadingDictionary_GetConcurrent()
        {
            // Arrange

            var value = "teststring";
            var loader = new ReturnKeyLoader<string>();
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);
            var results = new ConcurrentDictionary<int, string>();
            var count = 10;

            // Act

            Parallel.For(0, count, (i) =>
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
            Assert.AreEqual(1, dict.Keys.Count());
        }

        /// <summary>
        /// Test that when multiple threads call the TryGet method with the same
        /// key, where the value is not already loaded, that the load method
        /// is only called once. Also check that all the values returned are
        /// correct.
        /// </summary>
        [TestMethod]
        public void LoadingDictionary_TryGetConcurrent()
        {
            // Arrange

            var value = "teststring";
            var loader = new ReturnKeyLoader<string>();
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);
            var successes = new ConcurrentDictionary<int, bool>();
            var results = new ConcurrentDictionary<int, string>();
            var count = 10;

            // Act

            Parallel.For(0, count, (i) =>
            {
                if (dict.TryGet(value, _token.Token, out var result))
                {
                    results[i] = result;
                }
            });

            // Assert

            Assert.AreEqual(count, results.Values.Count);
            foreach (var result in results)
            {
                Assert.AreEqual(value, result.Value);
            }
            Assert.AreEqual(1, loader.Calls);
            Assert.AreEqual(1, dict.Keys.Count());
        }

        /// <summary>
        /// Test that an exception in the loader is propagated to the caller,
        /// and that the original exception is an inner exception of the
        /// KeyNotFoundException thrown by the get method.
        /// Also check that the result was not added to the dictionary.
        /// </summary>
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
            Assert.AreEqual(0, dict.Keys.Count());
        }

        /// <summary>
        /// Test that an exception in the loader is swallowed by the TryGet
        /// method, and that false is returned.
        /// Also check that the result was not added to the dictionary.
        /// </summary>
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
            Assert.AreEqual(0, dict.Keys.Count());
        }

        /// <summary>
        /// Test that calling the cancellation token results in the get method
        /// returning immediately, and not waiting for the Task to complete.
        /// Also check that the cancellation was passed to the loader properly,
        /// and that the correct exception is thrown.
        /// </summary>
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
                _ = dict[value, _token.Token];
            }
            catch (OperationCanceledException e)
            {
                exception = e;
            }
            var end = DateTime.Now;
            Thread.Sleep(millis);

            // Assert

            Assert.IsTrue((end - start).TotalMilliseconds < millis);
            Assert.AreEqual(1, loader.Cancels);
            Assert.IsNotNull(exception);
        }

        /// <summary>
        /// Test that calling the cancellation token results in the TryGet method
        /// returning immediately, and not waiting for the Task to complete.
        /// Also check that the cancellation was passed to the loader properly,
        /// and that the correct exception is thrown.
        /// </summary>
        [TestMethod]
        public void LoadingDictionary_TryGetCancelled()
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
                _ = dict.TryGet(value, _token.Token, out _);
            }
            catch (OperationCanceledException e)
            {
                exception = e;
            }
            var end = DateTime.Now;
            Thread.Sleep(millis);

            // Assert

            Assert.IsTrue((end - start).TotalMilliseconds < millis);
            Assert.AreEqual(1, loader.Cancels);
            Assert.IsNotNull(exception);
        }

        /// <summary>
        /// Test that getting values for keys that are pre-loaded in the
        /// constructor, that the load method is never called and the correct
        /// values are returned.
        /// </summary>
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
            Assert.AreEqual(values.Count, dict.Keys.Count());
        }

        /// <summary>
        /// Test that getting values for keys that are pre-loaded in the
        /// constructor, that the load method is never called and the correct
        /// values are returned.
        /// </summary>
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
            Assert.AreEqual(values.Count, dict.Keys.Count());
        }

        /// <summary>
        /// Test that pre-loading values does not affect normal operation of
        /// the loader.
        /// </summary>
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
                _ = dict[value.Key, _token.Token];
            }
            var result = dict["four", _token.Token];

            // Assert

            Assert.AreEqual(1, loader.Calls);
            Assert.AreEqual("four", result);
            Assert.AreEqual(values.Count + 1, dict.Keys.Count());
        }

        /// <summary>
        /// Test that pre-loading values does not affect normal operation of
        /// the loader.
        /// </summary>
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
                _ = dict.TryGet(value.Key, _token.Token, out _);
            }
            var success = dict.TryGet("four", _token.Token, out var result);

            // Assert

            Assert.AreEqual(1, loader.Calls);
            Assert.IsTrue(success);
            Assert.AreEqual("four", result);
            Assert.AreEqual(values.Count + 1, dict.Keys.Count());
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

            for (int i = 0; i < count; i++)
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
                }
                Assert.IsNotNull(exception);
                exception = null;
                Thread.Sleep(millis);
                _token = new CancellationTokenSource();
            }

            // Assert

            Assert.AreEqual(count, loader.Calls);
            Assert.AreEqual(count, loader.Cancels);
            Assert.AreEqual(0, dict.Keys.Count());
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

            Assert.AreEqual(count * 2, loader.Calls);
        }

        [TestMethod]
        public void LoadingDictionary_TryGetCanceledIsRemoved()
        {
            // Arrange

            var value = "testvalue";
            var millis = 100;
            var loader = new ReturnKeyLoader<string>(millis);
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);
            OperationCanceledException exception = null;
            var count = 2;

            // Act

            for (int i = 0; i < count; i++)
            {
                Console.WriteLine(i);
                _token.CancelAfter(millis / 2);

                try
                {
                    _ = dict.TryGet(value, _token.Token, out _);
                }
                catch (OperationCanceledException e)
                {
                    exception = e;
                }
                Assert.IsNotNull(exception);
                exception = null;
                Thread.Sleep(millis);
                _token = new CancellationTokenSource();
            }

            // Assert

            Assert.AreEqual(count, loader.Calls);
            Assert.AreEqual(count, loader.Cancels);
            Assert.AreEqual(0, dict.Keys.Count());
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

            Assert.AreEqual(count * 2, loader.Calls);
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
                _ = dict.TryGet(value, _token.Token, out _);
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
        public void LoadingDictionary_GetNull()
        {
            // Arrange

            var value = "teststring";
            var loader = new NullLoader<string, string>();
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);

            // Act

            var result = dict[value, _token.Token];

            // Assert

            Assert.IsNull(result);
            Assert.AreEqual(1, loader.Calls);
        }

        [TestMethod]
        public void LoadingDictionary_TryGetNull()
        {
            // Arrange

            var value = "teststring";
            var loader = new NullLoader<string, string>();
            var dict = new LoadingDictionary<string, string>(_logger.Object, loader);

            // Act

            var success = dict.TryGet(value, _token.Token, out var result);

            // Assert

            Assert.IsTrue(success);
            Assert.IsNull(result);
            Assert.AreEqual(1, loader.Calls);
        }
    }
}
