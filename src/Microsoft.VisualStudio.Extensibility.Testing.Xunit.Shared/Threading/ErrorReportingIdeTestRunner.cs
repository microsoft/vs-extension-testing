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
#pragma warning disable CA1062 // Validate arguments of public methods
            var aggregator = ctxt.Aggregator;
#pragma warning restore CA1062 // Validate arguments of public methods
#endif

#if !USES_XUNIT_3
            if (aggregator is null)
            {
                throw new ArgumentNullException(nameof(aggregator));
            }
#endif

#pragma warning disable SA1001 // Commas should be spaced correctly
#pragma warning disable SA1113 // Comma should be on the same line as previous parameter
#pragma warning disable SA1115 // Parameter should follow comma
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1111 // Closing parenthesis should be on line of last parameter
            return aggregator.RunAsync(
                () =>
                {
                    var exception = new InvalidOperationException("Test execution was skipped due to a prior exception in the harness.", _exception);

#if USES_XUNIT_3
                    return new ValueTask<TimeSpan>(Task.FromException<TimeSpan>(exception));
#else
                    return Task.FromException<decimal>(exception);
#endif
                }
#if USES_XUNIT_3
                , default
#endif
                );
#pragma warning restore SA1111 // Closing parenthesis should be on line of last parameter
#pragma warning restore SA1009 // Closing parenthesis should be spaced correctly
#pragma warning restore SA1115 // Parameter should follow comma
#pragma warning restore SA1113 // Comma should be on the same line as previous parameter
#pragma warning restore SA1001 // Commas should be spaced correctly
        }
    }
}
