// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel;
    using System.Data;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
#if !USES_XUNIT_3
    using Xunit.Abstractions;
#endif
    using Xunit.Harness;
#if USES_XUNIT_3
    using Xunit.Internal;
#endif
    using Xunit.Sdk;
#if USES_XUNIT_3
    using Xunit.v3;
#endif

    public sealed class IdeInstanceTestCase : IdeTestCaseBase
#if USES_XUNIT_3
#pragma warning disable SA1001 // Commas should be spaced correctly
        , ISelfExecutingXunitTestCase
#pragma warning restore SA1001 // Commas should be spaced correctly
#endif
    {
        /// <summary>
        /// Keep track of unique <see cref="IdeInstanceTestCase"/> instances returned for a given discovery pass. The
        /// <see cref="ITestFrameworkDiscoveryOptions"/> instance used for discovery is assumed to be a singleton
        /// instance used for one complete discovery pass. If this instance is used for subsequent discovery passes,
        /// the instance test cases might not show up in the discovery.
        /// </summary>
        private static readonly ConditionalWeakTable<ITestFrameworkDiscoveryOptions, StrongBox<ImmutableDictionary<VisualStudioInstanceKey, IdeInstanceTestCase>>> _instances = new();

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the deserializer; should only be called by deriving classes for deserialization purposes", error: true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public IdeInstanceTestCase()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
        }

#if USES_XUNIT_3
        public IdeInstanceTestCase(
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
            : base(testMethod, testCaseDisplayName, uniqueID, @explicit, visualStudioInstanceKey, includeRootSuffixInDisplayName: true, skipExceptions, skipReason, skipType, skipUnless, skipWhen, traits, testMethodArguments, sourceFilePath, sourceLineNumber, timeout)
        {
        }
#else
        public IdeInstanceTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, TestMethodDisplayOptions defaultMethodDisplayOptions, ITestMethod testMethod, VisualStudioInstanceKey visualStudioInstanceKey, object?[]? testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, visualStudioInstanceKey, testMethodArguments)
        {
        }
#endif

#if !USES_XUNIT_3
        protected override bool IncludeRootSuffixInDisplayName => true;
#endif

        public static IdeInstanceTestCase? TryCreateNewInstanceForFramework(
            ITestFrameworkDiscoveryOptions discoveryOptions,
#if !USES_XUNIT_3
            IMessageSink diagnosticMessageSink,
#endif
            VisualStudioInstanceKey visualStudioInstanceKey)
        {
            var lazyInstances = _instances.GetValue(discoveryOptions, static _ => new StrongBox<ImmutableDictionary<VisualStudioInstanceKey, IdeInstanceTestCase>>(ImmutableDictionary<VisualStudioInstanceKey, IdeInstanceTestCase>.Empty));
            var testMethod = IdeFactDiscoverer.CreateVisualStudioTestMethod(visualStudioInstanceKey);
#if USES_XUNIT_3
            var details = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, new FactAttribute());
            var traits = TestIntrospectionHelper.GetTraits(testMethod, null);

            var candidateTestCase = new IdeInstanceTestCase(
                details.ResolvedTestMethod,
                details.TestCaseDisplayName,
                details.UniqueID,
                details.Explicit,
                visualStudioInstanceKey,
                details.SkipExceptions,
                details.SkipReason,
                details.SkipType,
                details.SkipUnless,
                details.SkipWhen,
                testMethod.Traits.ToReadWrite(StringComparer.OrdinalIgnoreCase),
                timeout: details.Timeout);
#else
            var candidateTestCase = new IdeInstanceTestCase(diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, visualStudioInstanceKey);
#endif
            var testCase = ImmutableInterlocked.GetOrAdd(ref lazyInstances.Value, visualStudioInstanceKey, candidateTestCase);
            if (testCase != candidateTestCase)
            {
                // A different call to this method already returned the test case for this instance
                return null;
            }

            return candidateTestCase;
        }

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
                // Use XunitTestCaseRunner so the skip gets reported without trying to open VS
#if USES_XUNIT_3
                var tests = await aggregator.RunAsync(CreateTests, Array.Empty<IXunitTest>());
                return await XunitTestCaseRunner.Instance.Run(this, tests, messageBus, aggregator, cancellationTokenSource, displayName, SkipReason, ExplicitOption.Off, constructorArguments);
#else
                return await new XunitTestCaseRunner(this, DisplayName, SkipReason, constructorArguments, TestMethodArguments, messageBus, aggregator, cancellationTokenSource).RunAsync();
#endif
            }
            else
            {
#if USES_XUNIT_3
                throw new NotImplementedException("TODO");
#else
                return await new IdeTestCaseRunner(SharedData, VisualStudioInstanceKey, this, displayName, SkipReason, constructorArguments, TestMethodArguments, messageBus, aggregator, cancellationTokenSource).RunAsync();
#endif
            }
        }
    }
}
