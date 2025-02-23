// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit
{
    using System;
    using Xunit.Sdk;
    using Xunit.Threading;
#if USES_XUNIT_3
    using Xunit.v3;
#endif

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
#if USES_XUNIT_3
    [XunitTestCaseDiscoverer(typeof(IdeTheoryDiscoverer))]
#else
    [XunitTestCaseDiscoverer("Xunit.Threading.IdeTheoryDiscoverer", "Microsoft.VisualStudio.Extensibility.Testing.Xunit")]
#endif
    public class IdeTheoryAttribute : TheoryAttribute, IIdeSettingsAttribute
    {
        public IdeTheoryAttribute()
        {
            MinVersion = VisualStudioVersion.Unspecified;
            MaxVersion = VisualStudioVersion.Unspecified;
            RootSuffix = null;
            MaxAttempts = 0;
            EnvironmentVariables = new string[0];
        }

        public VisualStudioVersion MinVersion
        {
            get;
            set;
        }

        public VisualStudioVersion MaxVersion
        {
            get;
            set;
        }

        public string? RootSuffix
        {
            get;
            set;
        }

        public int MaxAttempts
        {
            get;
            set;
        }

        public string[] EnvironmentVariables
        {
            get;
            set;
        }
    }
}
