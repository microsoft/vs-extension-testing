// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
#if !USES_XUNIT_3
    using Xunit.Abstractions;
#endif
    using Xunit.Sdk;
    using Xunit.Threading;
#if USES_XUNIT_3
    using Xunit.v3;
#endif

    public class InProcessIdeTestAssemblyRunner : MarshalByRefObject
#if !USES_XUNIT_3
#pragma warning disable SA1001 // Commas should be spaced correctly
        , IDisposable
#pragma warning restore SA1001 // Commas should be spaced correctly
#endif
    {
#if USES_XUNIT_3
        private readonly IXunitTestAssembly _testAssembly;
        private readonly IXunitTestCase[] _testCases;
        private readonly IMessageSink _executionMessageSink;
        private readonly ITestFrameworkExecutionOptions _executionOptions;
#else
        private readonly TestAssemblyRunner<IXunitTestCase> _testAssemblyRunner;
#endif

        public InProcessIdeTestAssemblyRunner(
#if USES_XUNIT_3
            IXunitTestAssembly testAssembly,
#else
            ITestAssembly testAssembly,
#endif
            IEnumerable<IXunitTestCase> testCases,
            IMessageSink diagnosticMessageSink,
            IMessageSink executionMessageSink,
            ITestFrameworkExecutionOptions executionOptions)
        {
            var reconstructedTestCases = testCases.Select(testCase =>
            {
                if (testCase is IdeTestCase ideTestCase)
                {
                    return new IdeTestCase(diagnosticMessageSink, ideTestCase.DefaultMethodDisplay, ideTestCase.DefaultMethodDisplayOptions, ideTestCase.TestMethod, ideTestCase.VisualStudioInstanceKey, ideTestCase.TestMethodArguments);
                }
                else if (testCase is IdeTheoryTestCase ideTheoryTestCase)
                {
                    return new IdeTheoryTestCase(diagnosticMessageSink, ideTheoryTestCase.DefaultMethodDisplay, ideTheoryTestCase.DefaultMethodDisplayOptions, ideTheoryTestCase.TestMethod, ideTheoryTestCase.VisualStudioInstanceKey, ideTheoryTestCase.TestMethodArguments);
                }
                else if (testCase is IdeInstanceTestCase ideInstanceTestCase)
                {
                    return new IdeInstanceTestCase(diagnosticMessageSink, ideInstanceTestCase.DefaultMethodDisplay, ideInstanceTestCase.DefaultMethodDisplayOptions, ideInstanceTestCase.TestMethod, ideInstanceTestCase.VisualStudioInstanceKey, ideInstanceTestCase.TestMethodArguments);
                }

                return testCase;
            });

#if USES_XUNIT_3
            _testAssembly = testAssembly;
            _testCases = reconstructedTestCases.ToArray();
            _executionMessageSink = executionMessageSink;
            _executionOptions = executionOptions;
#else
            _testAssemblyRunner = new XunitTestAssemblyRunner(testAssembly, reconstructedTestCases.ToArray(), diagnosticMessageSink, executionMessageSink, executionOptions);
#endif
        }

        public Tuple<int, int, int, decimal> RunTestCollection(IMessageBus messageBus, ITestCollection testCollection, IXunitTestCase[] testCases)
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
#if USES_XUNIT_3
                var result = XunitTestAssemblyRunner.Instance.Run(_testAssembly, _testCases, _executionMessageSink, _executionOptions).GetAwaiter().GetResult();
#else
                var result = _testAssemblyRunner.RunAsync().GetAwaiter().GetResult();
#endif
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

                return Tuple.Create(result.Total, result.Failed, result.Skipped, result.Time);
            }
        }

#if !USES_XUNIT_3
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
#endif

        // The life of this object is managed explicitly
        public override object? InitializeLifetimeService()
        {
            return null;
        }

#if !USES_XUNIT_3
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _testAssemblyRunner.Dispose();
            }
        }
#endif
    }
}
