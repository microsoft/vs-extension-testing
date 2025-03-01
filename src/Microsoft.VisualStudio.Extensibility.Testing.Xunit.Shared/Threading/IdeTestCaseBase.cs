﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Microsoft.Win32;
#if !USES_XUNIT_3
    using Xunit.Abstractions;
#endif
    using Xunit.Harness;
    using Xunit.Sdk;
#if USES_XUNIT_3
    using Xunit.v3;
#endif

    public abstract class IdeTestCaseBase : XunitTestCase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the deserializer; should only be called by deriving classes for deserialization purposes", error: true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        protected IdeTestCaseBase()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
        }

#if USES_XUNIT_3
        protected IdeTestCaseBase(
            IXunitTestMethod testMethod,
            string testCaseDisplayName,
            string uniqueID,
            bool @explicit,
            VisualStudioInstanceKey visualStudioInstanceKey,
            string? skipReason = null,
            Type? skipType = null,
            string? skipUnless = null,
            string? skipWhen = null,
            Dictionary<string, HashSet<string>>? traits = null,
            object?[]? testMethodArguments = null,
            string? sourceFilePath = null,
            int? sourceLineNumber = null,
            int? timeout = null)
            : base(testMethod, testCaseDisplayName, uniqueID, @explicit, skipReason, skipType, skipUnless, skipWhen, traits, testMethodArguments, sourceFilePath, sourceLineNumber, timeout)
        {
            SharedData = WpfTestSharedData.Instance;
            VisualStudioInstanceKey = visualStudioInstanceKey;

            if (!IsInstalled(visualStudioInstanceKey.Version))
            {
                SkipReason = $"{visualStudioInstanceKey.Version} is not installed";
            }
        }
#else
        protected IdeTestCaseBase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, TestMethodDisplayOptions defaultMethodDisplayOptions, ITestMethod testMethod, VisualStudioInstanceKey visualStudioInstanceKey, object?[]? testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, testMethodArguments)
        {
            SharedData = WpfTestSharedData.Instance;
            VisualStudioInstanceKey = visualStudioInstanceKey;

            if (!IsInstalled(visualStudioInstanceKey.Version))
            {
                SkipReason = $"{visualStudioInstanceKey.Version} is not installed";
            }
        }
#endif

        public VisualStudioInstanceKey VisualStudioInstanceKey
        {
            get;
            private set;
        }

#if !USES_XUNIT_3
        public new TestMethodDisplay DefaultMethodDisplay => base.DefaultMethodDisplay;

        public new TestMethodDisplayOptions DefaultMethodDisplayOptions => base.DefaultMethodDisplayOptions;
#endif

        public WpfTestSharedData SharedData
        {
            get;
            private set;
        }

        protected virtual bool IncludeRootSuffixInDisplayName => false;

        protected override string GetDisplayName(IAttributeInfo factAttribute, string displayName)
        {
            var baseName = base.GetDisplayName(factAttribute, displayName);
            if (!IncludeRootSuffixInDisplayName || string.IsNullOrEmpty(VisualStudioInstanceKey.RootSuffix))
            {
                return $"{baseName} ({VisualStudioInstanceKey.Version})";
            }
            else
            {
                return $"{baseName} ({VisualStudioInstanceKey.Version}, {VisualStudioInstanceKey.RootSuffix})";
            }
        }

        protected override string GetUniqueID()
        {
            if (string.IsNullOrEmpty(VisualStudioInstanceKey.RootSuffix))
            {
                return $"{base.GetUniqueID()}_{VisualStudioInstanceKey.Version}";
            }
            else
            {
                return $"{base.GetUniqueID()}_{VisualStudioInstanceKey.RootSuffix}_{VisualStudioInstanceKey.Version}";
            }
        }

#if USES_XUNIT_3
        protected
#else
        public
#endif
        override void Serialize(IXunitSerializationInfo data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            base.Serialize(data);
            data.AddValue(nameof(VisualStudioInstanceKey), VisualStudioInstanceKey.SerializeToString());
            data.AddValue(nameof(SkipReason), SkipReason);
        }

#if USES_XUNIT_3
        protected
#else
        public
#endif
        override void Deserialize(IXunitSerializationInfo data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            VisualStudioInstanceKey = VisualStudioInstanceKey.DeserializeFromString(data.GetValue<string>(nameof(VisualStudioInstanceKey))!);
            base.Deserialize(data);
            SkipReason = data.GetValue<string>(nameof(SkipReason));
            SharedData = WpfTestSharedData.Instance;
        }

        internal static bool IsInstalled(VisualStudioVersion visualStudioVersion)
        {
            string dteKey;

            switch (visualStudioVersion)
            {
            case VisualStudioVersion.VS2012:
                dteKey = "VisualStudio.DTE.11.0";
                break;

            case VisualStudioVersion.VS2013:
                dteKey = "VisualStudio.DTE.12.0";
                break;

            case VisualStudioVersion.VS2015:
                dteKey = "VisualStudio.DTE.14.0";
                break;

            case VisualStudioVersion.VS2017:
                dteKey = "VisualStudio.DTE.15.0";
                break;

            case VisualStudioVersion.VS2019:
                dteKey = "VisualStudio.DTE.16.0";
                break;

            case VisualStudioVersion.VS2022:
                dteKey = "VisualStudio.DTE.17.0";
                break;

            default:
                throw new ArgumentException();
            }

            using (var key = Registry.ClassesRoot.OpenSubKey(dteKey))
            {
                return key != null;
            }
        }
    }
}
