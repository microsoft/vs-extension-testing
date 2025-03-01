﻿// Copyright (c) Microsoft. All rights reserved.
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

    public sealed class IdeTheoryTestCaseRunner
#if !USES_XUNIT_3
        : XunitTheoryTestCaseRunner
#endif
    {
#if !USES_XUNIT_3
        public IdeTheoryTestCaseRunner(
            WpfTestSharedData sharedData,
            VisualStudioInstanceKey visualStudioInstanceKey,
            IXunitTestCase testCase,
            string displayName,
            string? skipReason,
            object[] constructorArguments,
            IMessageSink diagnosticMessageSink,
            IMessageBus messageBus,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
            : base(testCase, displayName, skipReason, constructorArguments, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource)
        {
            SharedData = sharedData;
            VisualStudioInstanceKey = visualStudioInstanceKey;
        }

        public WpfTestSharedData SharedData
        {
            get;
        }

        public VisualStudioInstanceKey VisualStudioInstanceKey
        {
            get;
        }

        protected override XunitTestRunner CreateTestRunner(ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod, object[] testMethodArguments, string skipReason, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
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
                throw new NotSupportedException($"{nameof(IdeTheoryAttribute)} can only be used with the {nameof(IdeTestFramework)} test framework");
            }
        }
#endif

#if USES_XUNIT_3
        internal static async Task<RunSummary> RunAsync(WpfTestSharedData sharedData, IXunitTest test, IMessageBus messageBus, object[] constructorArguments, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        {
            if (Process.GetCurrentProcess().ProcessName == "devenv")
            {
                // We are already running inside Visual Studio
                // TODO: Verify version under test
                return await new InProcessIdeTestRunner().Run(test, messageBus, constructorArguments, ExplicitOption.Off, aggregator, cancellationTokenSource, beforeAfterAttributes);
            }
            else if (sharedData.Exception is not null)
            {
                return await new ErrorReportingIdeTestRunner(sharedData.Exception).Run(test, messageBus, constructorArguments, ExplicitOption.Off, aggregator, cancellationTokenSource, beforeAfterAttributes);
            }
            else
            {
                throw new NotSupportedException($"{nameof(IdeTheoryAttribute)} can only be used with the {nameof(IdeTestFramework)} test framework");
            }
        }
#endif
    }
}
