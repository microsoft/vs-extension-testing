// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Threading.Tasks;
    using Xunit;
#if !USES_XUNIT_3
    using Xunit.Abstractions;
#endif
    using Xunit.Sdk;
#if USES_XUNIT_3
    using Xunit.v3;
#endif

    public class IdeTestFrameworkExecutor : XunitTestFrameworkExecutor
    {
#if USES_XUNIT_3
        public IdeTestFrameworkExecutor(Assembly assembly)
            : base(new XUnit3TestAssembly(assembly))
        {
        }
#else
        public IdeTestFrameworkExecutor(AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider, IMessageSink diagnosticMessageSink)
            : base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
        {
        }
#endif

#if USES_XUNIT_3
        public override async ValueTask RunTestCases(IReadOnlyCollection<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
#else
        [SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Follows pattern expected by Xunit framework.")]
        protected override async void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
#endif
        {
            try
            {
#if USES_XUNIT_3
                await new IdeTestAssemblyRunner().Run(TestAssembly, testCases, executionMessageSink, executionOptions);
#else
                using (var assemblyRunner = new IdeTestAssemblyRunner(TestAssembly, testCases, DiagnosticMessageSink, executionMessageSink, executionOptions))
                {
                    await assemblyRunner.RunAsync();
                }
#endif
            }
            catch
            {
            }
        }

#if USES_XUNIT_3
        private sealed class XUnit3TestAssembly : XunitTestAssembly, IXunitTestAssembly
        {
            public XUnit3TestAssembly(Assembly assembly)
                : base(assembly, configFileName: null, assembly.GetName().Version)
            {
            }

            ITestCollectionOrderer? IXunitTestAssembly.TestCollectionOrderer
            {
                get
                {
                    var fromBase = base.TestCollectionOrderer ?? DefaultTestCollectionOrderer.Instance;
                    return new TestCollectionOrdererWrapper(); // TODO: Copy TestCollectionOrdererWrapper
                }
            }

            // Unnecessary cast suggestion here is likely a false positive?
            public ITestCollectionOrderer? TestCollectionOrder => ((IXunitTestAssembly)this).TestCollectionOrderer;

        }
#endif
    }
}
