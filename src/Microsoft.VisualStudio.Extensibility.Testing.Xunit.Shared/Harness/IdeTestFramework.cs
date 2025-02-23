// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System.Reflection;
#if !USES_XUNIT_3
    using Xunit.Abstractions;
#endif
    using Xunit.Sdk;
#if USES_XUNIT_3
    using Xunit.v3;
#endif

    public class IdeTestFramework : XunitTestFramework
    {
#if !USES_XUNIT_3
        public IdeTestFramework(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink)
        {
        }
#endif

        protected override ITestFrameworkExecutor CreateExecutor(
#if USES_XUNIT_3
            Assembly assembly)
#else
            AssemblyName assemblyName)
#endif
        {
#if USES_XUNIT_3
            return new IdeTestFrameworkExecutor(assembly);
#else
            return new IdeTestFrameworkExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
#endif
        }
    }
}
