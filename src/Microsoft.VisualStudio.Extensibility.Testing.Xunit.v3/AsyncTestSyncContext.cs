// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#if USES_XUNIT_3
namespace Xunit.Sdk
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This implementation of <see cref="SynchronizationContext"/> allows the developer to track the count
    /// of outstanding "async void" operations, and wait for them all to complete.
    /// </summary>
    public class AsyncTestSyncContext : SynchronizationContext
    {
        private static readonly TaskFactory TaskFactory = new TaskFactory();

        private readonly AsyncManualResetEvent _event = new AsyncManualResetEvent(true);
        private readonly SynchronizationContext _innerContext;
        private Exception? _exception;
        private int _operationCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncTestSyncContext"/> class.
        /// </summary>
        /// <param name="innerContext">The existing synchronization context (may be null).</param>
        public AsyncTestSyncContext(SynchronizationContext innerContext)
        {
            _innerContext = innerContext;
        }

        /// <inheritdoc/>
        public override void OperationCompleted()
        {
            var result = Interlocked.Decrement(ref _operationCount);
            if (result == 0)
            {
                _event.Set();
            }
        }

        /// <inheritdoc/>
        public override void OperationStarted()
        {
            Interlocked.Increment(ref _operationCount);
            _event.Reset();
        }

        /// <inheritdoc/>
        public override void Post(SendOrPostCallback d, object state)
        {
            // The call to Post() may be the state machine signaling that an exception is
            // about to be thrown, so we make sure the operation count gets incremented
            // before the Task.Run, and then decrement the count when the operation is done.
            OperationStarted();

            try
            {
                if (_innerContext == null)
                {
                    QueueUserWorkItem(() =>
                    {
                        try
                        {
                            d(state);
                        }
                        catch (Exception ex)
                        {
                            _exception = ex;
                        }
                        finally
                        {
                            OperationCompleted();
                        }
                    });
                }
                else
                {
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
                    _innerContext.Post(
                        _ =>
                        {
                            try
                            {
                                d(state);
                            }
                            catch (Exception ex)
                            {
                                _exception = ex;
                            }
                            finally
                            {
                                OperationCompleted();
                            }
                        },
                        null);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
                }
            }
            catch
            {
            }
        }

        /// <inheritdoc/>
        public override void Send(SendOrPostCallback d, object state)
        {
            try
            {
                if (_innerContext != null)
                {
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
                    _innerContext.Send(d, state);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
                }
                else
                {
#pragma warning disable CA1062 // Validate arguments of public methods
                    d(state);
#pragma warning restore CA1062 // Validate arguments of public methods
                }
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
        }

        /// <summary>
        /// Returns a task which is signaled when all outstanding operations are complete.
        /// </summary>
        public async Task<Exception?> WaitForCompletionAsync()
        {
            await _event.WaitAsync();

            return _exception;
        }

        public static void QueueUserWorkItem(Action backgroundTask, EventWaitHandle? finished = null)
        {
#pragma warning disable VSTHRD110 // Observe result of async calls
            TaskFactory.StartNew(
                _ =>
                {
                    var state = (State)_;

                    try
                    {
                        state.BackgroundTask();
                    }
                    finally
                    {
                        if (state.Finished != null)
                        {
                            state.Finished.Set();
                        }
                    }
                },
                new State { BackgroundTask = backgroundTask, Finished = finished },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
#pragma warning restore VSTHRD110 // Observe result of async calls
        }

        private class State
        {
#pragma warning disable SA1401 // Fields should be private
            public Action BackgroundTask = null!;
            public EventWaitHandle? Finished;
#pragma warning restore SA1401 // Fields should be private
        }
    }
}
#endif
