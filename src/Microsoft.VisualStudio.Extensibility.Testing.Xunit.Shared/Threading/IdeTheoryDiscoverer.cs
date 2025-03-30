// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading.Tasks;
#if !USES_XUNIT_3
    using Xunit.Abstractions;
#endif
#if USES_XUNIT_3
    using Xunit.Internal;
#endif
    using Xunit.Sdk;
#if USES_XUNIT_3
    using Xunit.v3;
#endif

    public class IdeTheoryDiscoverer : TheoryDiscoverer
    {
#if !USES_XUNIT_3
        public IdeTheoryDiscoverer(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink)
        {
        }
#endif

#if !USES_XUNIT_3
        protected override IEnumerable<IXunitTestCase> CreateTestCasesForSkip(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, string skipReason)
        {
            foreach (var supportedInstance in IdeFactDiscoverer.GetSupportedInstances(testMethod, theoryAttribute))
            {
                yield return new IdeTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, supportedInstance);
                if (IdeInstanceTestCase.TryCreateNewInstanceForFramework(discoveryOptions, DiagnosticMessageSink, supportedInstance) is { } instanceTestCase)
                {
                    yield return instanceTestCase;
                }
            }
        }

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForSkippedDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, object?[] dataRow, string skipReason)
        {
            foreach (var supportedInstance in IdeFactDiscoverer.GetSupportedInstances(testMethod, theoryAttribute))
            {
                yield return new IdeSkippedDataRowTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, supportedInstance, skipReason, dataRow);
            }
        }
#endif

#if USES_XUNIT_3
        protected override ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, IXunitTestMethod testMethod, ITheoryAttribute theoryAttribute, ITheoryDataRow dataRow, object?[] testMethodArguments)
#else
        protected override IEnumerable<IXunitTestCase> CreateTestCasesForDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, object?[] dataRow)
#endif
        {
            var testCases = new List<IXunitTestCase>();
            foreach (var supportedInstance in IdeFactDiscoverer.GetSupportedInstances(testMethod, theoryAttribute))
            {
#if USES_XUNIT_3
                var details = TestIntrospectionHelper.GetTestCaseDetailsForTheoryDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow, testMethodArguments);
                var traits = TestIntrospectionHelper.GetTraits(testMethod, dataRow);
                testCases.Add(new IdeTestCase(
                    details.ResolvedTestMethod,
                    details.TestCaseDisplayName,
                    details.UniqueID,
                    details.Explicit,
                    supportedInstance,
                    details.SkipExceptions,
                    details.SkipReason,
                    details.SkipType,
                    details.SkipUnless,
                    details.SkipWhen,
                    traits,
                    testMethodArguments,
                    timeout: details.Timeout));
#else
                testCases.Add(new IdeTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, supportedInstance, dataRow));
#endif
#if USES_XUNIT_3
                if (IdeInstanceTestCase.TryCreateNewInstanceForFramework(discoveryOptions, supportedInstance) is { } instanceTestCase)
#else
                if (IdeInstanceTestCase.TryCreateNewInstanceForFramework(discoveryOptions, DiagnosticMessageSink, supportedInstance) is { } instanceTestCase)
#endif
                {
                    testCases.Add(instanceTestCase);
                }
            }

#if USES_XUNIT_3
            return new ValueTask<IReadOnlyCollection<IXunitTestCase>>(testCases);
#else
            return testCases;
#endif
        }

#if USES_XUNIT_3
        protected override ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForTheory(ITestFrameworkDiscoveryOptions discoveryOptions, IXunitTestMethod testMethod, ITheoryAttribute theoryAttribute)
#else
        protected override IEnumerable<IXunitTestCase> CreateTestCasesForTheory(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute)
#endif
        {
            var testCases = new List<IXunitTestCase>();
            foreach (var supportedInstance in IdeFactDiscoverer.GetSupportedInstances(testMethod, theoryAttribute))
            {
#if USES_XUNIT_3
                var details = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, theoryAttribute);

#pragma warning disable CA1062 // Validate arguments of public methods
                testCases.Add(new IdeTheoryTestCase(
                    details.ResolvedTestMethod,
                    details.TestCaseDisplayName,
                    details.UniqueID,
                    details.Explicit,
                    supportedInstance,
                    details.SkipExceptions,
                    details.SkipReason,
                    details.SkipType,
                    details.SkipUnless,
                    details.SkipWhen,
                    testMethod.Traits.ToReadWrite(StringComparer.OrdinalIgnoreCase),
                    timeout: details.Timeout));
#pragma warning restore CA1062 // Validate arguments of public methods
#else
                testCases.Add(new IdeTheoryTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, supportedInstance));
#endif
#if USES_XUNIT_3
                if (IdeInstanceTestCase.TryCreateNewInstanceForFramework(discoveryOptions, supportedInstance) is { } instanceTestCase)
#else
                if (IdeInstanceTestCase.TryCreateNewInstanceForFramework(discoveryOptions, DiagnosticMessageSink, supportedInstance) is { } instanceTestCase)
#endif
                {
                    testCases.Add(instanceTestCase);
                }
            }

#if USES_XUNIT_3
            return new ValueTask<IReadOnlyCollection<IXunitTestCase>>(testCases);
#else
            return testCases;
#endif
        }
    }
}
