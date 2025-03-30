// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Runtime.Remoting;
    using System.Security;
    using System.Threading.Tasks;
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

#if USES_XUNIT_3
        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            LongLivedMarshalByRefObject.DisconnectAll();
        }
#endif
    }

#if USES_XUNIT_3
#pragma warning disable SA1402 // File may only contain a single type
    public abstract class LongLivedMarshalByRefObject : MarshalByRefObject
#pragma warning restore SA1402 // File may only contain a single type
    {
        private static ConcurrentBag<MarshalByRefObject> _remoteObjects = new ConcurrentBag<MarshalByRefObject>();

        protected LongLivedMarshalByRefObject()
        {
            _remoteObjects.Add(this);
        }

        [SecuritySafeCritical]
        public static void DisconnectAll()
        {
            foreach (MarshalByRefObject remoteObject in _remoteObjects)
            {
                RemotingServices.Disconnect(remoteObject);
            }

            _remoteObjects = new ConcurrentBag<MarshalByRefObject>();
        }

        [SecurityCritical]
        public sealed override object? InitializeLifetimeService()
        {
            return null;
        }
    }
#endif
}
