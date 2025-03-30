// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
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

    public sealed class IdeTheoryTestCase : IdeTestCaseBase
#if USES_XUNIT_3
#pragma warning disable SA1001 // Commas should be spaced correctly
        , ISelfExecutingXunitTestCase
#pragma warning restore SA1001 // Commas should be spaced correctly
#endif
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the deserializer; should only be called by deriving classes for deserialization purposes", error: true)]
        public IdeTheoryTestCase()
        {
        }

#if USES_XUNIT_3
        public IdeTheoryTestCase(
            IXunitTestMethod testMethod,
            string testCaseDisplayName,
            string uniqueID,
            bool @explicit,
            VisualStudioInstanceKey visualStudioInstanceKey,
            Type[]? skipExceptions = null,
            string? skipReason = null,
            Type? skipType = null,
            string? skipUnless = null,
            string? skipWhen = null,
            Dictionary<string, HashSet<string>>? traits = null,
            object?[]? testMethodArguments = null,
            string? sourceFilePath = null,
            int? sourceLineNumber = null,
            int? timeout = null)
            : base(testMethod, testCaseDisplayName, uniqueID, @explicit, visualStudioInstanceKey, includeRootSuffixInDisplayName: false, skipExceptions, skipReason, skipType, skipUnless, skipWhen, traits, testMethodArguments, sourceFilePath, sourceLineNumber, timeout)
        {
        }
#else
        public IdeTheoryTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, TestMethodDisplayOptions defaultMethodDisplayOptions, ITestMethod testMethod, VisualStudioInstanceKey visualStudioInstanceKey, object?[]? testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, visualStudioInstanceKey, testMethodArguments)
        {
        }
#endif

#if USES_XUNIT_3
        public async ValueTask<RunSummary> Run(ExplicitOption explicitOption, IMessageBus messageBus, object?[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
#else
        public override async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
#endif
        {
            string displayName =
#if USES_XUNIT_3
                TestCaseDisplayName;
#else
                DisplayName;
#endif

            if (!string.IsNullOrEmpty(SkipReason))
            {
                // Use XunitTheoryTestCaseRunner so the skip gets reported without trying to open VS
#if USES_XUNIT_3
                var tests = await aggregator.RunAsync(CreateTests, Array.Empty<IXunitTest>());
                return await XunitTestCaseRunner.Instance.Run(this, tests, messageBus, aggregator, cancellationTokenSource, displayName, SkipReason, ExplicitOption.Off, constructorArguments);
#else
                return await new XunitTheoryTestCaseRunner(this, displayName, SkipReason, constructorArguments, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource).RunAsync();
#endif
            }
            else
            {
#if USES_XUNIT_3
                return await IdeTheoryTestCaseRunner.RunAsync(SharedData, null!/*TODO*/, messageBus, constructorArguments, null! /*TODO: beforeAfterAttributes*/, aggregator, cancellationTokenSource);
#else
                return await new IdeTheoryTestCaseRunner(SharedData, VisualStudioInstanceKey, this, displayName, SkipReason, constructorArguments, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource).RunAsync();
#endif
            }
        }
    }
}
