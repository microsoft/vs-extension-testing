// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
#if !USES_XUNIT_3
    using Xunit.Abstractions;
#endif
    using Xunit.Harness;
#if USES_XUNIT_3
    using Xunit.Internal;
#endif
    using Xunit.Sdk;
#if USES_XUNIT_3
    using Xunit.v3;
#endif

#if USES_XUNIT_3
    using IdeSettingsAttributeAbstractedType = Xunit.IdeSettingsAttribute;
    using IFactAttributeType = Xunit.v3.IFactAttribute;
    using ITestMethodType = Xunit.v3.IXunitTestMethod;
#else
    using IdeSettingsAttributeAbstractedType = Xunit.Abstractions.IAttributeInfo;
    using IFactAttributeType = Xunit.Abstractions.IAttributeInfo;
    using ITestMethodType = Xunit.Abstractions.ITestMethod;
#endif

    public class IdeFactDiscoverer : IXunitTestCaseDiscoverer
    {
#if !USES_XUNIT_3
        private readonly IMessageSink _diagnosticMessageSink;

        public IdeFactDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }
#endif

        public
#if USES_XUNIT_3
            ValueTask<IReadOnlyCollection<IXunitTestCase>>
#else
            IEnumerable<IXunitTestCase>
#endif
            Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethodType testMethod, IFactAttributeType factAttribute)
        {
            if (testMethod is null)
            {
                throw new ArgumentNullException(nameof(testMethod));
            }

            var testCases = new List<IXunitTestCase>();

#if USES_XUNIT_3
            var details = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, factAttribute);
#endif
            if (!testMethod.Method.GetParameters().Any())
            {
                if (!testMethod.Method.IsGenericMethodDefinition)
                {
                    foreach (var supportedInstance in GetSupportedInstances(testMethod, factAttribute))
                    {
#if USES_XUNIT_3
                        testCases.Add(new IdeTestCase(
                            details.ResolvedTestMethod,
                            details.TestCaseDisplayName,
                            details.UniqueID,
                            details.Explicit,
                            supportedInstance,
                            details.SkipExceptions,
                            details.SkipReason,
                            details.SkipType,
                            details.SkipUnless,
                            details.SkipWhen,
                            testMethod.Traits.ToReadWrite(StringComparer.OrdinalIgnoreCase),
                            timeout: details.Timeout));
#else
                        testCases.Add(new IdeTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, supportedInstance));
#endif
#if USES_XUNIT_3
                        if (IdeInstanceTestCase.TryCreateNewInstanceForFramework(discoveryOptions, supportedInstance) is { } instanceTestCase)
#else
                        if (IdeInstanceTestCase.TryCreateNewInstanceForFramework(discoveryOptions, _diagnosticMessageSink, supportedInstance) is { } instanceTestCase)
#endif
                        {
                            testCases.Add(instanceTestCase);
                        }
                    }
                }
                else
                {
#if USES_XUNIT_3
                    testCases.Add(new ExecutionErrorTestCase(testMethod, details.TestCaseDisplayName, details.UniqueID, "[IdeFact] methods are not allowed to be generic."));
#else
                    testCases.Add(new ExecutionErrorTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, "[IdeFact] methods are not allowed to be generic."));
#endif
                }
            }
            else
            {
#if USES_XUNIT_3
                testCases.Add(new ExecutionErrorTestCase(testMethod, details.TestCaseDisplayName, details.UniqueID, "[IdeFact] methods are not allowed to have parameters. Did you mean to use [IdeTheory]?"));
#else
                testCases.Add(new ExecutionErrorTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, "[IdeFact] methods are not allowed to have parameters. Did you mean to use [IdeTheory]?"));
#endif
            }

#if USES_XUNIT_3
            return new ValueTask<IReadOnlyCollection<IXunitTestCase>>(testCases);
#else
            return testCases;
#endif
        }

        internal static ITestMethodType CreateVisualStudioTestMethod(VisualStudioInstanceKey supportedInstance)
        {
#if USES_XUNIT_3
            var testAssembly = new XunitTestAssembly(typeof(Instances).Assembly);
            var testCollection = new XunitTestCollection(testAssembly, collectionDefinition: null, disableParallelization: true, nameof(Instances));
            var testClass = new XunitTestClass(typeof(Instances), testCollection);
            var testMethod = testClass.Methods.Single(method => method.Name == nameof(Instances.VisualStudio));
            return new XunitTestMethod(testClass, testMethod, Array.Empty<object?>());
#else
            var testAssembly = new TestAssembly(new ReflectionAssemblyInfo(typeof(Instances).Assembly));
            var testCollection = new TestCollection(testAssembly, collectionDefinition: null, nameof(Instances));
            var testClass = new TestClass(testCollection, new ReflectionTypeInfo(typeof(Instances)));
            var testMethod = testClass.Class.GetMethods(false).Single(method => method.Name == nameof(Instances.VisualStudio));
            return new TestMethod(testClass, testMethod);
#endif
        }

        internal static IEnumerable<VisualStudioInstanceKey> GetSupportedInstances(ITestMethodType testMethod, IFactAttributeType factAttribute)
        {
            var rootSuffix = GetRootSuffix(testMethod, factAttribute);
            var maxAttempts = GetMaxAttempts(testMethod, factAttribute);
            var environmentVariables = GetEnvironmentVariables(testMethod, factAttribute);
            return GetSupportedVersions(factAttribute, GetSettingsAttributes(testMethod).ToArray())
                .Select(version => new VisualStudioInstanceKey(version, rootSuffix, maxAttempts, environmentVariables));
        }

        private static string GetRootSuffix(ITestMethodType testMethod, IFactAttributeType factAttribute)
        {
            return GetRootSuffix(factAttribute, GetSettingsAttributes(testMethod).ToArray());
        }

        private static int GetMaxAttempts(ITestMethodType testMethod, IFactAttributeType factAttribute)
        {
            return GetMaxAttempts(factAttribute, GetSettingsAttributes(testMethod).ToArray());
        }

        private static string[] GetEnvironmentVariables(ITestMethodType testMethod, IFactAttributeType factAttribute)
        {
            return GetEnvironmentVariables(factAttribute, GetSettingsAttributes(testMethod).ToArray());
        }

        private static IEnumerable<IdeSettingsAttributeAbstractedType> GetSettingsAttributes(ITestMethodType testMethod)
        {
#if USES_XUNIT_3
            foreach (var attributeData in testMethod.Method.GetCustomAttributes(true).OfType<IdeSettingsAttributeAbstractedType>())
#else
            foreach (var attributeData in testMethod.Method.GetCustomAttributes(typeof(IdeSettingsAttribute)))
#endif
            {
                yield return attributeData;
            }

#if USES_XUNIT_3
            foreach (var attributeData in testMethod.TestClass.Class.GetCustomAttributes(true).OfType<IdeSettingsAttributeAbstractedType>())
#else
            foreach (var attributeData in testMethod.TestClass.Class.GetCustomAttributes(typeof(IdeSettingsAttribute)))
#endif
            {
                yield return attributeData;
            }
        }

        private static IEnumerable<VisualStudioVersion> GetSupportedVersions(IFactAttributeType factAttribute, IdeSettingsAttributeAbstractedType[] settingsAttributes)
        {
            var minVersion = GetNamedArgument(
                factAttribute,
                settingsAttributes,
                nameof(IIdeSettingsAttribute.MinVersion),
                static value => value is not VisualStudioVersion.Unspecified,
                defaultValue: VisualStudioVersion.VS2012);

            var maxVersion = GetNamedArgument(
                factAttribute,
                settingsAttributes,
                nameof(IIdeSettingsAttribute.MaxVersion),
                static value => value is not VisualStudioVersion.Unspecified,
                defaultValue: VisualStudioVersion.VS2022);

            for (var version = minVersion; version <= maxVersion; version++)
            {
#if MERGED_PIA
                if (version >= VisualStudioVersion.VS2012 && version < VisualStudioVersion.VS2022)
                {
                    continue;
                }
#else
                if (version >= VisualStudioVersion.VS2022)
                {
                    continue;
                }
#endif

                yield return version;
            }
        }

        private static string GetRootSuffix(IFactAttributeType factAttribute, IdeSettingsAttributeAbstractedType[] settingsAttributes)
        {
            return GetNamedArgument(
                factAttribute,
                settingsAttributes,
                nameof(IIdeSettingsAttribute.RootSuffix),
                static value => value is not null,
                defaultValue: "Exp");
        }

        private static int GetMaxAttempts(IFactAttributeType factAttribute, IdeSettingsAttributeAbstractedType[] settingsAttributes)
        {
            return GetNamedArgument(
                factAttribute,
                settingsAttributes,
                nameof(IIdeSettingsAttribute.MaxAttempts),
                static value => value > 0,
                defaultValue: 1);
        }

        private static string[] GetEnvironmentVariables(IFactAttributeType factAttribute, IdeSettingsAttributeAbstractedType[] settingsAttributes)
        {
            return GetNamedArgument(
                factAttribute,
                settingsAttributes,
                nameof(IIdeSettingsAttribute.EnvironmentVariables),
                static value => value != null,
                (inherited, current) => MergeEnvironmentVariables(inherited, current),
                defaultValue: new string[0]);
        }

        private static string[] MergeEnvironmentVariables(string[] inherited, string[] current)
        {
            if (inherited.Length == 0)
            {
                return current;
            }
            else if (current.Length == 0)
            {
                return inherited;
            }

            var set = new HashSet<string>(KeyOnlyComparerIgnoreCase.Instance);
            foreach (var value in current)
            {
                set.Add(value);
            }

            foreach (var value in inherited)
            {
                set.Add(value);
            }

            return set.ToArray();
        }

        private static TValue GetNamedArgument<TValue>(IFactAttributeType factAttribute, IdeSettingsAttributeAbstractedType[] settingsAttributes, string argumentName, Func<TValue, bool> isValidValue, TValue defaultValue)
        {
            return GetNamedArgument(
                factAttribute,
                settingsAttributes,
                argumentName,
                isValidValue,
                merge: null,
                defaultValue);
        }

        private static TValue GetNamedArgument<TValue>(IFactAttributeType factAttribute, IdeSettingsAttributeAbstractedType[] settingsAttributes, string argumentName, Func<TValue, bool> isValidValue, Func<TValue, TValue, TValue>? merge, TValue defaultValue)
        {
            StrongBox<TValue>? result = null;
#pragma warning disable SA1114 // Parameter list should follow declaration
#pragma warning disable SA1003 // Symbols should be spaced correctly
            if (TryGetNamedArgument(
#if USES_XUNIT_3
                (Attribute)factAttribute,
#else
                factAttribute,
#endif
                argumentName,
                isValidValue,
                out var value))
            {
                if (merge is null)
                {
                    return value;
                }

                result = new StrongBox<TValue>(value);
            }
#pragma warning restore SA1003 // Symbols should be spaced correctly
#pragma warning restore SA1114 // Parameter list should follow declaration

            foreach (var attribute in settingsAttributes)
            {
                if (TryGetNamedArgument(attribute, argumentName, isValidValue, out value))
                {
                    if (merge is null)
                    {
                        return value;
                    }
                    else if (result is null)
                    {
                        result = new StrongBox<TValue>(value);
                    }
                    else
                    {
                        result.Value = merge(value, result.Value);
                    }

                    return value;
                }
            }

            if (result is not null)
            {
                return result.Value;
            }

            return defaultValue;

#if USES_XUNIT_3
            static bool TryGetNamedArgument(Attribute attribute, string argumentName, Func<TValue, bool> isValidValue, out TValue value)
            {
                foreach (var propInfo in attribute.GetType().GetRuntimeProperties())
                {
                    if (propInfo.Name == argumentName)
                    {
                        value = (TValue)propInfo.GetValue(attribute);
                        return isValidValue(value);
                    }
                }

                foreach (var fieldInfo in attribute.GetType().GetRuntimeFields())
                {
                    if (fieldInfo.Name == argumentName)
                    {
                        value = (TValue)fieldInfo.GetValue(attribute);
                        return isValidValue(value);
                    }
                }

                throw new ArgumentException($"Could not find property or field named '{argumentName}' on instance of '{attribute.GetType().FullName}'", nameof(argumentName));
            }
#else
            static bool TryGetNamedArgument(IAttributeInfo attribute, string argumentName, Func<TValue, bool> isValidValue, out TValue value)
            {
                value = attribute.GetNamedArgument<TValue>(argumentName);
                return isValidValue(value);
            }
#endif
        }

        private class KeyOnlyComparerIgnoreCase : IEqualityComparer<string?>
        {
            public static readonly KeyOnlyComparerIgnoreCase Instance = new KeyOnlyComparerIgnoreCase();

            private KeyOnlyComparerIgnoreCase()
            {
            }

            public bool Equals(string? x, string? y)
            {
                if (x is null)
                {
                    return y is null;
                }
                else if (y is null)
                {
                    return false;
                }

                return StringComparer.OrdinalIgnoreCase.Equals(GetKey(x), GetKey(y));
            }

            public int GetHashCode(string? obj)
            {
                if (obj is null)
                {
                    return 0;
                }

                return StringComparer.OrdinalIgnoreCase.GetHashCode(GetKey(obj));
            }

            private static string GetKey(string s)
            {
                var keyEnd = s.IndexOf('=');
                if (keyEnd < 0)
                {
                    return s;
                }

                return s.Substring(0, keyEnd);
            }
        }
    }
}
