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
                await XunitTestAssemblyRunner.Instance.Run(TestAssembly, testCases, executionMessageSink, executionOptions);
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
        private sealed class XUnit3TestAssembly : IXunitTestAssembly
        {
            public XUnit3TestAssembly(Assembly assembly)
            {
                Assembly = assembly;
            }

            public Assembly Assembly { get; }

            public IReadOnlyCollection<Type> AssemblyFixtureTypes => Array.Empty<Type>();

            public IReadOnlyCollection<IBeforeAfterTestAttribute> BeforeAfterTestAttributes => Array.Empty<IBeforeAfterTestAttribute>();

            public ICollectionBehaviorAttribute? CollectionBehavior => null;

#pragma warning disable SA1316 // Tuple element names should use correct casing
            public IReadOnlyDictionary<string, (Type Type, CollectionDefinitionAttribute Attribute)> CollectionDefinitions => new Dictionary<string, (Type Type, CollectionDefinitionAttribute Attribute)>();
#pragma warning restore SA1316 // Tuple element names should use correct casing

            public string TargetFramework => throw new NotImplementedException();

            public ITestCaseOrderer? TestCaseOrderer => null;

            public ITestCollectionOrderer? TestCollectionOrderer => null;

            public Version Version => throw new NotImplementedException();

            public Guid ModuleVersionID => throw new NotImplementedException();

            public string AssemblyName => throw new NotImplementedException();

            public string AssemblyPath => throw new NotImplementedException();

            public string? ConfigFilePath => throw new NotImplementedException();

            public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => throw new NotImplementedException();

            public string UniqueID => throw new NotImplementedException();
        }
#endif
    }
}
