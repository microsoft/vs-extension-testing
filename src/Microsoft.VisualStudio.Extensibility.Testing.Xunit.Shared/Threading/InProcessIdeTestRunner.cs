// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
#if !USES_XUNIT_3
    using Xunit.Abstractions;
#endif
    using Xunit.Harness;
    using Xunit.InProcess;
    using Xunit.Sdk;
#if USES_XUNIT_3
    using Xunit.v3;
#endif

    public class InProcessIdeTestRunner : XunitTestRunner
    {
#if !USES_XUNIT_3
        public InProcessIdeTestRunner(ITest test, IMessageBus messageBus, Type testClass, object?[] constructorArguments, MethodInfo testMethod, object?[]? testMethodArguments, string skipReason, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
        }
#endif

#if USES_XUNIT_3
        protected override async ValueTask<TimeSpan> RunTest(XunitTestRunnerContext ctxt)
#else
        protected override async Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator)
#endif
        {
#if USES_XUNIT_3
            var test = ctxt.Test;
            var messageBus = ctxt.MessageBus;
            var testClass = ctxt.TestMethod.DeclaringType;
            var constructorArguments = ctxt.ConstructorArguments;
            var testMethod = ctxt.TestMethod;
            var testMethodArguments = ctxt.TestMethodArguments;
            var beforeAfterAttributes = ctxt.BeforeAfterTestAttributes;
            var aggregator = ctxt.Aggregator;
            var cts = ctxt.CancellationTokenSource;
#else
            var test = Test;
            var messageBus = MessageBus;
            var testClass = TestClass;
            var constructorArguments = ConstructorArguments;
            var testMethod = TestMethod;
            var testMethodArguments = TestMethodArguments;
            var beforeAfterAttributes = BeforeAfterAttributes;
            var cts = CancellationTokenSource;
#endif

            DataCollectionService.InstallFirstChanceExceptionHandler();
            VisualStudio_InProc.Create().ActivateMainWindow();

            var synchronizationContext = new DispatcherSynchronizationContext(Application.Current.Dispatcher, DispatcherPriority.Background);
            var taskScheduler = new SynchronizationContextTaskScheduler(synchronizationContext);
            try
            {
                DataCollectionService.CurrentTest = test;
                return await Task.Factory.StartNew(
                    () => new InProcessIdeTestInvoker(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator, cts).RunAsync(),
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    taskScheduler).Unwrap();
            }
            finally
            {
                DataCollectionService.CurrentTest = null;
            }
        }
    }
}
