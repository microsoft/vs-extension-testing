// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit
{
    using System;
    using Xunit.Sdk;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer("Xunit.Threading.IdeFactDiscoverer", "Microsoft.VisualStudio.Extensibility.Testing.Xunit")]
    public class IdeFactAttribute : FactAttribute, IIdeSettingsAttribute
    {
        public IdeFactAttribute()
        {
            MinVersion = VisualStudioVersion.Unspecified;
            MaxVersion = VisualStudioVersion.Unspecified;
            RootSuffix = null;
            MaxAttempts = 0;
            EnvironmentVariables = new string[0];
        }

        public IdeFactAttribute(Type conditionType)
        {
            if (!typeof(ITestCondition).IsAssignableFrom(conditionType))
            {
                throw new ArgumentException($"The condition type '{conditionType.FullName}' must implement ITestCondition.", nameof(conditionType));
            }
        
            var condition = (ITestCondition)Activator.CreateInstance(conditionType)!;
        
            if (!condition.ShouldRun)
            {
                Skip = condition.SkipReason ?? "Test condition not met.";
            }
        
            MinVersion = VisualStudioVersion.Unspecified;
            MaxVersion = VisualStudioVersion.Unspecified;
            RootSuffix = null;
            MaxAttempts = 0;
            EnvironmentVariables = Array.Empty<string>();
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
