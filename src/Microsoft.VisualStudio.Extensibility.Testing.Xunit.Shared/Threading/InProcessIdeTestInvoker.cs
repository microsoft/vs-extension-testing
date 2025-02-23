// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
#if !USES_XUNIT_3
    using Xunit.Abstractions;
#endif
    using Xunit.Harness;
    using Xunit.Sdk;
#if USES_XUNIT_3
    using Xunit.v3;
#endif

#if USES_XUNIT_3
    using BeforeAfterTestAttributeType = Xunit.v3.IBeforeAfterTestAttribute;
#else
    using BeforeAfterTestAttributeType = Xunit.Sdk.BeforeAfterTestAttribute;
#endif

    public class InProcessIdeTestInvoker : XunitTestInvoker
    {
        private readonly Stack<BeforeAfterTestAttributeType> _beforeAfterAttributesRun = new();
        private readonly IReadOnlyList<BeforeAfterTestAttributeType> _beforeAfterAttributes;

        public InProcessIdeTestInvoker(
#if USES_XUNIT_3
            IXunitTest test,
#else
            ITest test,
#endif
            IMessageBus messageBus,
            Type testClass,
            object[] constructorArguments,
            MethodInfo testMethod,
            object[] testMethodArguments,
            IReadOnlyList<BeforeAfterTestAttributeType> beforeAfterAttributes,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
            _beforeAfterAttributes = beforeAfterAttributes;
        }

        public
#if !USES_XUNIT_3
            new
#endif
#if USES_XUNIT_3
            ValueTask<TimeSpan>
#else
            Task<decimal>
#endif
            RunAsync()
        {
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1111 // Closing parenthesis should be on line of last parameter
#pragma warning disable SA1001 // Commas should be spaced correctly
#pragma warning disable SA1113 // Comma should be on the same line as previous parameter
#pragma warning disable SA1115 // Parameter should follow comma
#pragma warning disable SA1116 // Split parameters should start on line after declaration
            return Aggregator.RunAsync(async delegate
            {
                if (!CancellationTokenSource.IsCancellationRequested)
                {
                    var testClassInstance = CreateTestClass();
                    try
                    {
                        var asyncLifetime = testClassInstance as IAsyncLifetime;
                        if (asyncLifetime != null)
                        {
                            try
                            {
                                await asyncLifetime.InitializeAsync();
                            }
                            catch (Exception ex) when (DataCollectionService.LogAndPropagate(ex))
                            {
                                throw ExceptionUtilities.Unreachable;
                            }
                        }

                        if (!CancellationTokenSource.IsCancellationRequested)
                        {
                            await BeforeTestMethodInvokedAsync();
                            if (!CancellationTokenSource.IsCancellationRequested && !Aggregator.HasExceptions)
                            {
                                await InvokeTestMethodAsync(testClassInstance);
                            }

                            await AfterTestMethodInvokedAsync();
                        }

                        if (asyncLifetime != null)
                        {
                            await Aggregator.RunAsync(async () =>
                            {
                                try
                                {
                                    await asyncLifetime.DisposeAsync();
                                }
                                catch (Exception ex) when (DataCollectionService.LogAndPropagate(ex))
                                {
                                    throw ExceptionUtilities.Unreachable;
                                }
                            });
                        }
                    }
                    finally
                    {
                        Aggregator.Run(delegate
                        {
#if USES_XUNIT_3
                            Test.DisposeTestClass(testClassInstance, MessageBus, CancellationTokenSource);
#else
                            Test.DisposeTestClass(testClassInstance, MessageBus, Timer, CancellationTokenSource);
#endif
                        });
                    }
                }

#if USES_XUNIT_3
                // TODO: Measure time correctly
                return TimeSpan.Zero;
#else
                return Timer.Total;
#endif
            }
#pragma warning restore SA1116 // Split parameters should start on line after declaration
#if USES_XUNIT_3
            , TimeSpan.Zero
#endif
            );
#pragma warning restore SA1115 // Parameter should follow comma
#pragma warning restore SA1113 // Comma should be on the same line as previous parameter
#pragma warning restore SA1001 // Commas should be spaced correctly
#pragma warning restore SA1111 // Closing parenthesis should be on line of last parameter
#pragma warning restore SA1009 // Closing parenthesis should be spaced correctly
        }

        protected override object CreateTestClass()
        {
            try
            {
                return base.CreateTestClass();
            }
            catch (Exception ex) when (DataCollectionService.LogAndPropagate(ex))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static BeforeTestStarting CreateBeforeTestStarting(ITest test, string attributeName)
        {
#if USES_XUNIT_3
            return new BeforeTestStarting()
            {
                AssemblyUniqueID = test.TestCase.TestCollection.TestAssembly.UniqueID,
                AttributeName = attributeName,
                TestCaseUniqueID = test.TestCase.UniqueID,
                TestClassUniqueID = test.TestCase.TestClass?.UniqueID,
                TestCollectionUniqueID = test.TestCase.TestCollection.UniqueID,
                TestMethodUniqueID = test.TestCase.TestMethod?.UniqueID,
                TestUniqueID = test.UniqueID,
            };
#else
            return new BeforeTestStarting(test, attributeName);
#endif
        }

        private static AfterTestStarting CreateAfterTestStarting(ITest test, string attributeName)
        {
#if USES_XUNIT_3
            return new AfterTestStarting()
            {
                AssemblyUniqueID = test.TestCase.TestCollection.TestAssembly.UniqueID,
                AttributeName = attributeName,
                TestCaseUniqueID = test.TestCase.UniqueID,
                TestClassUniqueID = test.TestCase.TestClass?.UniqueID,
                TestCollectionUniqueID = test.TestCase.TestCollection.UniqueID,
                TestMethodUniqueID = test.TestCase.TestMethod?.UniqueID,
                TestUniqueID = test.UniqueID,
            };
#else
            return new AfterTestStarting(test, attributeName);
#endif
        }

        private static BeforeTestFinished CreateBeforeTestFinished(ITest test, string attributeName)
        {
#if USES_XUNIT_3
            return new BeforeTestFinished()
            {
                AssemblyUniqueID = test.TestCase.TestCollection.TestAssembly.UniqueID,
                AttributeName = attributeName,
                TestCaseUniqueID = test.TestCase.UniqueID,
                TestClassUniqueID = test.TestCase.TestClass?.UniqueID,
                TestCollectionUniqueID = test.TestCase.TestCollection.UniqueID,
                TestMethodUniqueID = test.TestCase.TestMethod?.UniqueID,
                TestUniqueID = test.UniqueID,
            };
#else
            return new BeforeTestFinished(test, attributeName);
#endif
        }

        private static AfterTestFinished CreateAfterTestFinished(ITest test, string attributeName)
        {
#if USES_XUNIT_3
            return new AfterTestFinished()
            {
                AssemblyUniqueID = test.TestCase.TestCollection.TestAssembly.UniqueID,
                AttributeName = attributeName,
                TestCaseUniqueID = test.TestCase.UniqueID,
                TestClassUniqueID = test.TestCase.TestClass?.UniqueID,
                TestCollectionUniqueID = test.TestCase.TestCollection.UniqueID,
                TestMethodUniqueID = test.TestCase.TestMethod?.UniqueID,
                TestUniqueID = test.UniqueID,
            };
#else
            return new AfterTestFinished(test, attributeName);
#endif
        }

        protected
#if !USES_XUNIT_3
            override
#endif
            Task BeforeTestMethodInvokedAsync()
        {
            foreach (var beforeAfterAttribute in _beforeAfterAttributes)
            {
                var attributeName = beforeAfterAttribute.GetType().Name;
                if (!MessageBus.QueueMessage(CreateBeforeTestStarting(Test, attributeName)))
                {
                    CancellationTokenSource.Cancel();
                }
                else
                {
                    try
                    {
#if USES_XUNIT_3
                        beforeAfterAttribute.Before(TestMethod, Test);
#else
                        beforeAfterAttribute.Before(TestMethod);
#endif
                        _beforeAfterAttributesRun.Push(beforeAfterAttribute);
                    }
                    catch (Exception ex) when (DataCollectionService.LogAndCatch(ex))
                    {
                        Aggregator.Add(ex);
                        break;
                    }
                    finally
                    {
                        if (!MessageBus.QueueMessage(CreateBeforeTestFinished(Test, attributeName)))
                        {
                            CancellationTokenSource.Cancel();
                        }
                    }
                }

                if (CancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }
            }

#if NET472
            return Task.CompletedTask;
#else
            var tcs = new TaskCompletionSource<bool>();
            tcs.SetResult(true);
            return tcs.Task;
#endif
        }

        protected
#if !USES_XUNIT_3
            override
#endif
            async
#if USES_XUNIT_3
            Task
#else
            Task<decimal>
#endif
            InvokeTestMethodAsync(object testClassInstance)
        {
            var oldSyncContext = SynchronizationContext.Current;

            try
            {
                var asyncSyncContext = new AsyncTestSyncContext(oldSyncContext);
                SynchronizationContext.SetSynchronizationContext(asyncSyncContext);

#pragma warning disable SA1114 // Parameter list should follow declaration
#pragma warning disable SA1111 // Closing parenthesis should be on line of last parameter
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
                await Aggregator.RunAsync(
#if !USES_XUNIT_3
                    async () => await Timer.AggregateAsync(
#endif
                        async () =>
                        {
                            var parameterCount = TestMethod.GetParameters().Length;
                            var valueCount = TestMethodArguments == null ? 0 : TestMethodArguments.Length;
                            if (parameterCount != valueCount)
                            {
                                Aggregator.Add(
                                    new InvalidOperationException(
                                        $"The test method expected {parameterCount} parameter value{(parameterCount == 1 ? string.Empty : "s")}, but {valueCount} parameter value{(valueCount == 1 ? string.Empty : "s")} {(valueCount == 1 ? "was" : "were")} provided."));
                            }
                            else
                            {
                                var result = CallTestMethod(testClassInstance);
                                var task = GetTaskFromResult(result);
                                if (task != null)
                                {
                                    if (task.Status == TaskStatus.Created)
                                    {
                                        throw new InvalidOperationException("Test method returned a non-started Task (tasks must be started before being returned)");
                                    }

                                    try
                                    {
                                        await task;
                                    }
                                    catch (Exception ex) when (DataCollectionService.LogAndPropagate(ex))
                                    {
                                        throw ExceptionUtilities.Unreachable;
                                    }
                                }
                                else
                                {
                                    var ex = await asyncSyncContext.WaitForCompletionAsync();
                                    if (ex != null)
                                    {
                                        DataCollectionService.TryLog(ex);
                                        Aggregator.Add(ex);
                                    }
                                }
                            }
                        }
#if !USES_XUNIT_3
                    )
#endif
                    );
#pragma warning restore SA1009 // Closing parenthesis should be spaced correctly
#pragma warning restore SA1111 // Closing parenthesis should be on line of last parameter
#pragma warning restore SA1114 // Parameter list should follow declaration
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(oldSyncContext);
            }

#if !USES_XUNIT_3
            return Timer.Total;
#endif
        }

        protected override object CallTestMethod(object testClassInstance)
        {
            try
            {
                return base.CallTestMethod(testClassInstance);
            }
            catch (Exception ex) when (DataCollectionService.LogAndPropagate(ex))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        protected
#if !USES_XUNIT_3
            override
#endif
            Task AfterTestMethodInvokedAsync()
        {
            foreach (var beforeAfterAttribute in _beforeAfterAttributesRun)
            {
                var attributeName = beforeAfterAttribute.GetType().Name;
                if (!MessageBus.QueueMessage(CreateAfterTestStarting(Test, attributeName)))
                {
                    CancellationTokenSource.Cancel();
                }

                Aggregator.Run(() =>
                {
#if !USES_XUNIT_3
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1111 // Closing parenthesis should be on line of last parameter
                    Timer.Aggregate(() =>
#endif
                    {
                        try
                        {
#if USES_XUNIT_3
                            beforeAfterAttribute.After(TestMethod, Test);
#else
                            beforeAfterAttribute.After(TestMethod);
#endif
                        }
                        catch (Exception ex) when (DataCollectionService.LogAndPropagate(ex))
                        {
                            throw ExceptionUtilities.Unreachable;
                        }
                    }
#if !USES_XUNIT_3
                    );
#pragma warning restore SA1111 // Closing parenthesis should be on line of last parameter
#pragma warning restore SA1009 // Closing parenthesis should be spaced correctly
#endif
                });

                if (!MessageBus.QueueMessage(CreateAfterTestFinished(Test, attributeName)))
                {
                    CancellationTokenSource.Cancel();
                }
            }

#if NET472
            return Task.CompletedTask;
#else
            var tcs = new TaskCompletionSource<bool>();
            tcs.SetResult(true);
            return tcs.Task;
#endif
        }
    }
}
