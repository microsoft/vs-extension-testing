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
    using Xunit.Sdk;
#if USES_XUNIT_3
    using Xunit.v3;
#endif

    public class ErrorReportingIdeTestRunner : XunitTestRunner
    {
        private readonly Exception _exception;

#if USES_XUNIT_3
        public ErrorReportingIdeTestRunner(Exception exception)
        {
            _exception = exception;
        }
#else
        public ErrorReportingIdeTestRunner(Exception exception, ITest test, IMessageBus messageBus, Type testClass, object?[] constructorArguments, MethodInfo testMethod, object?[]? testMethodArguments, string skipReason, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
            _exception = exception;
        }
#endif

#if USES_XUNIT_3
        protected override ValueTask<TimeSpan> RunTest(XunitTestRunnerContext ctxt)
#else
        protected override Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator)
#endif
        {
#if USES_XUNIT_3
            var aggregator = ctxt.Aggregator;
#endif

#if !USES_XUNIT_3
            if (aggregator is null)
            {
                throw new ArgumentNullException(nameof(aggregator));
            }
#endif

            return aggregator.RunAsync(
                () =>
                {
                    var tcs = new TaskCompletionSource<decimal>();
                    tcs.SetException(new InvalidOperationException("Test execution was skipped due to a prior exception in the harness.", _exception));
                    return tcs.Task;
                });
        }
    }
}
