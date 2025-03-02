// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#if USES_XUNIT_3

namespace Xunit.Sdk
{
    using System.Threading.Tasks;

    internal class AsyncManualResetEvent
    {
        private volatile TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();

        public AsyncManualResetEvent(bool signaled = false)
        {
            if (signaled)
            {
                _taskCompletionSource.TrySetResult(true);
            }
        }

        public bool IsSet
        {
            get { return _taskCompletionSource.Task.IsCompleted; }
        }

        public Task WaitAsync()
        {
            return _taskCompletionSource.Task;
        }

        public void Set()
        {
            _taskCompletionSource.TrySetResult(true);
        }

        public void Reset()
        {
            if (IsSet)
            {
                _taskCompletionSource = new TaskCompletionSource<bool>();
            }
        }
    }
}
#endif
