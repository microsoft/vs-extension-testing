// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
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
#pragma warning disable CA1062 // Validate arguments of public methods
            : base(new XUnit3TestAssembly(assembly))
#pragma warning restore CA1062 // Validate arguments of public methods
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
                    var original = ExtensibilityPointFactory.GetAssemblyTestCollectionOrderer(Assembly) ?? DefaultTestCollectionOrderer.Instance;
                    return new TestCollectionOrdererWrapper(original); // TODO: Copy TestCollectionOrdererWrapper
                }
            }
        }

        private sealed class TestCollectionOrdererWrapper : ITestCollectionOrderer
        {
            public TestCollectionOrdererWrapper(ITestCollectionOrderer underlying)
            {
                Underlying = underlying;
            }

            public ITestCollectionOrderer Underlying { get; }

            public IReadOnlyCollection<TTestCollection> OrderTestCollections<TTestCollection>(IReadOnlyCollection<TTestCollection> testCollections)
                where TTestCollection : ITestCollection
            {
                var collections = Underlying.OrderTestCollections(testCollections).ToArray();
                var collectionsWithoutIdeInstanceCases = collections.Where(collection => !ContainsIdeInstanceCase(collection));
                var collectionsWithIdeInstanceCases = collections.Where(collection => ContainsIdeInstanceCase(collection));
                return collectionsWithoutIdeInstanceCases.Concat(collectionsWithIdeInstanceCases).ToArray();
            }

            private static bool ContainsIdeInstanceCase(ITestCollection collection)
            {
                var assemblyName = new AssemblyName(collection.TestAssembly.AssemblyName);
                return assemblyName.Name == "Microsoft.VisualStudio.Extensibility.Testing.Xunit";
            }
        }
#endif
    }
}
