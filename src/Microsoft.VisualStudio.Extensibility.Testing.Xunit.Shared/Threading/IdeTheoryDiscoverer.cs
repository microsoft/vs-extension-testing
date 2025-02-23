// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Threading
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
#if !USES_XUNIT_3
    using Xunit.Abstractions;
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
                testCases.Add(new IdeTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, supportedInstance, dataRow));
                if (IdeInstanceTestCase.TryCreateNewInstanceForFramework(discoveryOptions, DiagnosticMessageSink, supportedInstance) is { } instanceTestCase)
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
                testCases.Add(new IdeTheoryTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, supportedInstance));
                if (IdeInstanceTestCase.TryCreateNewInstanceForFramework(discoveryOptions, DiagnosticMessageSink, supportedInstance) is { } instanceTestCase)
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
