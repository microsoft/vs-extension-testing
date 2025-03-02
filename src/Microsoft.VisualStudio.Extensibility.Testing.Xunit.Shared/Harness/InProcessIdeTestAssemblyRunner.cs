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
    using Microsoft.VisualStudio.Experimentation;
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
#if !USES_XUNIT_3
            IMessageSink diagnosticMessageSink,
#endif
            IMessageSink executionMessageSink,
            ITestFrameworkExecutionOptions executionOptions)
        {
            var reconstructedTestCases = testCases.Select(testCase =>
            {
                if (testCase is IdeTestCase ideTestCase)
                {
#if USES_XUNIT_3
                    return new IdeTestCase(ideTestCase.TestMethod, ideTestCase.TestCaseDisplayName, ideTestCase.UniqueID, ideTestCase.Explicit, ideTestCase.VisualStudioInstanceKey, ideTestCase.SkipReason, ideTestCase.SkipType, ideTestCase.SkipUnless, ideTestCase.SkipWhen, ideTestCase.Traits, ideTestCase.TestMethodArguments, ideTestCase.SourceFilePath, ideTestCase.SourceLineNumber, ideTestCase.Timeout);
#else
                    return new IdeTestCase(diagnosticMessageSink, ideTestCase.DefaultMethodDisplay, ideTestCase.DefaultMethodDisplayOptions, ideTestCase.TestMethod, ideTestCase.VisualStudioInstanceKey, ideTestCase.TestMethodArguments);
#endif
                }
                else if (testCase is IdeTheoryTestCase ideTheoryTestCase)
                {
#if USES_XUNIT_3
                    return new IdeTheoryTestCase(ideTheoryTestCase.TestMethod, ideTheoryTestCase.TestCaseDisplayName, ideTheoryTestCase.UniqueID, ideTheoryTestCase.Explicit, ideTheoryTestCase.VisualStudioInstanceKey, ideTheoryTestCase.SkipReason, ideTheoryTestCase.SkipType, ideTheoryTestCase.SkipUnless, ideTheoryTestCase.SkipWhen, ideTheoryTestCase.Traits, ideTheoryTestCase.TestMethodArguments, ideTheoryTestCase.SourceFilePath, ideTheoryTestCase.SourceLineNumber, ideTheoryTestCase.Timeout);
#else
                    return new IdeTheoryTestCase(diagnosticMessageSink, ideTheoryTestCase.DefaultMethodDisplay, ideTheoryTestCase.DefaultMethodDisplayOptions, ideTheoryTestCase.TestMethod, ideTheoryTestCase.VisualStudioInstanceKey, ideTheoryTestCase.TestMethodArguments);
#endif
                }
                else if (testCase is IdeInstanceTestCase ideInstanceTestCase)
                {
#if USES_XUNIT_3
                    return new IdeInstanceTestCase(ideInstanceTestCase.TestMethod, ideInstanceTestCase.TestCaseDisplayName, ideInstanceTestCase.UniqueID, ideInstanceTestCase.Explicit, ideInstanceTestCase.VisualStudioInstanceKey, ideInstanceTestCase.SkipReason, ideInstanceTestCase.SkipType, ideInstanceTestCase.SkipUnless, ideInstanceTestCase.SkipWhen, ideInstanceTestCase.Traits, ideInstanceTestCase.TestMethodArguments, ideInstanceTestCase.SourceFilePath, ideInstanceTestCase.SourceLineNumber, ideInstanceTestCase.Timeout);
#else
                    return new IdeInstanceTestCase(diagnosticMessageSink, ideInstanceTestCase.DefaultMethodDisplay, ideInstanceTestCase.DefaultMethodDisplayOptions, ideInstanceTestCase.TestMethod, ideInstanceTestCase.VisualStudioInstanceKey, ideInstanceTestCase.TestMethodArguments);
#endif
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

#if !USES_XUNIT_3
        // NOTE: These parameters are unused.
        // However, for backward compatibility, we keep them as this method is public.
        public Tuple<int, int, int, decimal> RunTestCollection(IMessageBus messageBus, ITestCollection testCollection, IXunitTestCase[] testCases)
            => RunTestCollection();
#endif

#if USES_XUNIT_3
        public Tuple<int, int, int, decimal> RunTestCollection(TestContextWrapper wrapper)
#else
        public Tuple<int, int, int, decimal> RunTestCollection()
#endif
        {
#if USES_XUNIT_3
#pragma warning disable CA1062 // Validate arguments of public methods
            RestoreFromWrapper(wrapper);
#pragma warning restore CA1062 // Validate arguments of public methods
#endif

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

#if USES_XUNIT_3
        private static void RestoreFromWrapper(TestContextWrapper wrapper)
        {
            if (wrapper.PipelineStage == TestPipelineStage.TestAssemblyExecution)
            {
                // NOTE: We cannot simply call SetForTestAssembly here.
                // SetForTestAssembly will attempt to call TestContext.CurrentInternal.KeyValueStorage which will throw.
                // To hack around that, we call SetForInitialization first, which will create a TestContext with empty dictionary.
                // Then, we copy from the existing dictionary to the new one (in most cases (maybe always?), _keyValueStorage is going to be empty).
                // Then, we call SetForTestAssembly. At this point, KeyValueStorage will not throw.
                TestContext.SetForInitialization(null, false, false);
                foreach (var entry in wrapper.KeyValueStorage)
                {
                    TestContext.Current.KeyValueStorage[entry.Key] = entry.Value;
                }

                // If wrapper.Assembly is non-null, that means it's our own XUnit3TestAssembly.
                // XUnit3TestAssembly is not serializable, so create InProcessTestAssembly that is equivalent.
                var testAssembly = wrapper.Assembly is { } assembly
                    ? new InProcessTestAssembly(assembly, wrapper.TestCollectionOrderer)
                    : wrapper.TestAssembly;

                TestContext.SetForTestAssembly(testAssembly!, wrapper.TestAssemblyStatus!.Value, default(CancellationToken));
            }
            else
            {
                // If we hit this, add more conditions for different stages as needed.
                throw new InvalidOperationException($"Unexpected PipelineStage '{wrapper.PipelineStage}'.");
            }
        }

        private sealed class InProcessTestAssembly : XunitTestAssembly, IXunitTestAssembly
        {
            private readonly ITestCollectionOrderer? _testCollectionOrderer;

            public InProcessTestAssembly(Assembly assembly, ITestCollectionOrderer? testCollectionOrderer)
                : base(assembly, configFileName: null, assembly.GetName().Version)
            {
                _testCollectionOrderer = testCollectionOrderer;
            }

            ITestCollectionOrderer? IXunitTestAssembly.TestCollectionOrderer
                => _testCollectionOrderer;
        }
#endif
    }
}
