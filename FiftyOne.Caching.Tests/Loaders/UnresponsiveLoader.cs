using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FiftyOne.Caching.Tests.Loaders
{
    /// <summary>
    /// Value loader that returns a Task which will not respond to the
    /// cancellation token. Once the test has finished, the Task should
    /// be canceled using the <see cref="Terminate"/> method.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class UnresponsiveLoader<T> : IValueTaskLoader<T, T>
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
}
