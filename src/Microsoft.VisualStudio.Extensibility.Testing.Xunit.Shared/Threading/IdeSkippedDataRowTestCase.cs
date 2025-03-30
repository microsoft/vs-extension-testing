// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
#if !USES_XUNIT_3
    using Xunit.Abstractions;
#endif
    using Xunit.Harness;
    using Xunit.Sdk;
#if USES_XUNIT_3
    using Xunit.v3;
#endif

    public sealed class IdeSkippedDataRowTestCase
#if USES_XUNIT_3
        : XunitTestCase
#else
        : XunitSkippedDataRowTestCase
#endif
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the deserializer; should only be called by deriving classes for deserialization purposes", error: true)]
        public IdeSkippedDataRowTestCase()
        {
        }

#if USES_XUNIT_3
        public IdeSkippedDataRowTestCase(
            IXunitTestMethod testMethod,
            string testCaseDisplayName,
            string uniqueID,
            bool @explicit,
            VisualStudioInstanceKey visualStudioInstanceKey,
            Type[]? skipExceptions = null,
            string? skipReason = null,
            Type? skipType = null,
            string? skipUnless = null,
            string? skipWhen = null,
            Dictionary<string, HashSet<string>>? traits = null,
            object?[]? testMethodArguments = null,
            string? sourceFilePath = null,
            int? sourceLineNumber = null,
            int? timeout = null)
            : base(testMethod, $"{testCaseDisplayName} ({visualStudioInstanceKey.Version})", $"{uniqueID}_{visualStudioInstanceKey.Version}", @explicit, skipExceptions, skipReason, skipType, skipUnless, skipWhen, traits, testMethodArguments, sourceFilePath, sourceLineNumber, timeout)
        {
        }
#else
        public IdeSkippedDataRowTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, TestMethodDisplayOptions defaultMethodDisplayOptions, ITestMethod testMethod, VisualStudioInstanceKey visualStudioInstanceKey, string skipReason, object?[]? testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, skipReason, testMethodArguments)
        {
            VisualStudioInstanceKey = visualStudioInstanceKey;
        }
#endif

#if !USES_XUNIT_3

        public VisualStudioInstanceKey VisualStudioInstanceKey
        {
            get;
            private set;
        }

        protected override string GetDisplayName(IAttributeInfo factAttribute, string displayName)
        {
            var baseName = base.GetDisplayName(factAttribute, displayName);
            return $"{baseName} ({VisualStudioInstanceKey.Version})";
        }

        protected override string GetUniqueID()
        {
            return $"{base.GetUniqueID()}_{VisualStudioInstanceKey.Version}";
        }

        public override void Serialize(IXunitSerializationInfo data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            base.Serialize(data);
            data.AddValue(nameof(VisualStudioInstanceKey), VisualStudioInstanceKey.SerializeToString());
        }

        public override void Deserialize(IXunitSerializationInfo data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            VisualStudioInstanceKey = VisualStudioInstanceKey.DeserializeFromString(data.GetValue<string>(nameof(VisualStudioInstanceKey)));
            base.Deserialize(data);
        }
#endif
    }
}
