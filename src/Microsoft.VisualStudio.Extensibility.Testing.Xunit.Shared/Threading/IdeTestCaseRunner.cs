// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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

    public sealed class IdeTestCaseRunner : XunitTestCaseRunner
    {
#if USES_XUNIT_3
        public IdeTestCaseRunner(WpfTestSharedData sharedData, VisualStudioInstanceKey visualStudioInstanceKey)
        {
            SharedData = sharedData;
            VisualStudioInstanceKey = visualStudioInstanceKey;
        }
#else
        public IdeTestCaseRunner(
            WpfTestSharedData sharedData,
            VisualStudioInstanceKey visualStudioInstanceKey,
            IXunitTestCase testCase,
            string displayName,
            string skipReason,
            object?[] constructorArguments,
            object?[]? testMethodArguments,
            IMessageBus messageBus,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
            : base(testCase, displayName, skipReason, constructorArguments, testMethodArguments, messageBus, aggregator, cancellationTokenSource)
        {
            SharedData = sharedData;
            VisualStudioInstanceKey = visualStudioInstanceKey;
        }
#endif

        public WpfTestSharedData SharedData
        {
            get;
        }

        public VisualStudioInstanceKey VisualStudioInstanceKey
        {
            get;
        }

#if USES_XUNIT_3
        protected override async ValueTask<RunSummary> RunTest(XunitTestCaseRunnerContext ctxt, IXunitTest test)
        {
            if (Process.GetCurrentProcess().ProcessName == "devenv")
            {
                // We are already running inside Visual Studio
                // TODO: Verify version under test
#pragma warning disable CA1062 // Validate arguments of public methods
                return await new InProcessIdeTestRunner().Run(test, ctxt.MessageBus, ctxt.ConstructorArguments, ExplicitOption.Off, ctxt.Aggregator, ctxt.CancellationTokenSource, ctxt.BeforeAfterTestAttributes);
#pragma warning restore CA1062 // Validate arguments of public methods
            }
            else if (SharedData.Exception is not null)
            {
                return await new ErrorReportingIdeTestRunner(SharedData.Exception).Run(test, ctxt.MessageBus, ctxt.ConstructorArguments, ExplicitOption.Off, ctxt.Aggregator, ctxt.CancellationTokenSource, ctxt.BeforeAfterTestAttributes);
            }
            else
            {
                throw new NotSupportedException($"{nameof(IdeFactAttribute)} can only be used with the {nameof(IdeTestFramework)} test framework");
            }
        }
#else
        protected override XunitTestRunner CreateTestRunner(ITest test, IMessageBus messageBus, Type testClass, object?[] constructorArguments, MethodInfo testMethod, object?[]? testMethodArguments, string skipReason, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        {
            if (Process.GetCurrentProcess().ProcessName == "devenv")
            {
                // We are already running inside Visual Studio
                // TODO: Verify version under test
                return new InProcessIdeTestRunner(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource);
            }
            else if (SharedData.Exception is not null)
            {
                return new ErrorReportingIdeTestRunner(SharedData.Exception, test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource);
            }
            else
            {
                throw new NotSupportedException($"{nameof(IdeFactAttribute)} can only be used with the {nameof(IdeTestFramework)} test framework");
            }
        }
#endif
    }
}
