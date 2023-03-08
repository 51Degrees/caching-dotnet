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
    internal class UnresponsiveLoader<T> : TrackingLoaderBase<T, T>
    {
        private bool _isCanceled = false;

        public void Terminate()
        {
            _isCanceled = true;
        }

        protected override T GetValue(T key)
        {
            while (_isCanceled == false)
            {
                Task.Delay(10);
            }
            return key;
        }
    }
}
