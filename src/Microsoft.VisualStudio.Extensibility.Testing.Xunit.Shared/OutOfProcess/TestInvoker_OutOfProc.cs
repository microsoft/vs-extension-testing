// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.OutOfProcess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Remoting;
    using System.Threading.Tasks;
#if !USES_XUNIT_3
    using Xunit.Abstractions;
#endif
    using Xunit.Harness;
    using Xunit.InProcess;
    using Xunit.Sdk;
#if USES_XUNIT_3
    using Xunit.v3;
#endif

#if USES_XUNIT_3
    // xUnit v3 implementation of ITestFrameworkExecutionOptions isn't serializable.
    // Wrapping in a MarshalByRefObject to allow it to be passed in process.
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name
    internal sealed class ExecutionOptionsWrapper : LongLivedMarshalByRefObject, ITestFrameworkExecutionOptions
#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
    {
        private readonly ITestFrameworkExecutionOptions _executionOptions;

        public ExecutionOptionsWrapper(ITestFrameworkExecutionOptions executionOptions)
            => _executionOptions = executionOptions;

        public TValue? GetValue<TValue>(string name)
            => _executionOptions.GetValue<TValue>(name);

        public void SetValue<TValue>(string name, TValue value)
            => _executionOptions.SetValue(name, value);

        public string ToJson()
            => _executionOptions.ToJson();
    }

#pragma warning disable SA1402 // File may only contain a single type
    // xUnit v3 implementation of XunitTestCase isn't serializable.
    // Wrapping in a MarshalByRefObject to allow it to be passed in process.
    internal sealed class XunitTestCaseWrapper : LongLivedMarshalByRefObject, IXunitTestCase
#pragma warning restore SA1402 // File may only contain a single type
    {
        private readonly IXunitTestCase _testCase;

        public XunitTestCaseWrapper(IXunitTestCase testCase)
            => _testCase = testCase;

        public string? SkipReason
            => _testCase.SkipReason;

        public Type? SkipType
            => _testCase.SkipType;

        public string? SkipUnless
            => _testCase.SkipUnless;

        public string? SkipWhen
            => _testCase.SkipWhen;

        public IXunitTestClass TestClass
            => _testCase.TestClass;

        public int TestClassMetadataToken
            => _testCase.TestClassMetadataToken;

        public string TestClassName
            => _testCase.TestClassName;

        public string TestClassSimpleName
            => _testCase.TestClassSimpleName;

        public IXunitTestCollection TestCollection
            => _testCase.TestCollection;

        public IXunitTestMethod TestMethod
            => _testCase.TestMethod;

        public int TestMethodMetadataToken
            => _testCase.TestMethodMetadataToken;

        public string TestMethodName
            => _testCase.TestMethodName;

        public string[] TestMethodParameterTypesVSTest
            => _testCase.TestMethodParameterTypesVSTest;

        public string TestMethodReturnTypeVSTest
            => _testCase.TestMethodReturnTypeVSTest;

        public int Timeout
            => _testCase.Timeout;

        public bool Explicit
            => _testCase.Explicit;

        public string? SourceFilePath
            => _testCase.SourceFilePath;

        public int? SourceLineNumber
            => _testCase.SourceLineNumber;

        public string TestCaseDisplayName
            => _testCase.TestCaseDisplayName;

        public string? TestClassNamespace
            => _testCase.TestClassNamespace;

        public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits
            => _testCase.Traits;

        public string UniqueID
            => _testCase.UniqueID;

        ITestClass? ITestCase.TestClass
            => ((ITestCase)_testCase).TestClass;

        ITestCollection ITestCase.TestCollection
            => ((ITestCase)_testCase).TestCollection;

        ITestMethod? ITestCase.TestMethod
            => ((ITestCase)_testCase).TestMethod;

        int? ITestCaseMetadata.TestClassMetadataToken
            => ((ITestCaseMetadata)_testCase).TestClassMetadataToken;

        int? ITestCaseMetadata.TestMethodMetadataToken
            => ((ITestCaseMetadata)_testCase).TestMethodMetadataToken;

        public ValueTask<IReadOnlyCollection<IXunitTest>> CreateTests()
            => _testCase.CreateTests();

        public void PostInvoke()
            => _testCase.PostInvoke();

        public void PreInvoke()
            => _testCase.PreInvoke();
    }
#endif

    internal class TestInvoker_OutOfProc : OutOfProcComponent
    {
        internal TestInvoker_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            TestInvokerInProc = CreateInProcComponent<TestInvoker_InProc>(visualStudioInstance);
        }

        internal TestInvoker_InProc TestInvokerInProc
        {
            get;
        }

        public void LoadAssembly(string codeBase)
        {
            TestInvokerInProc.LoadAssembly(codeBase);
        }

        public InProcessIdeTestAssemblyRunner CreateTestAssemblyRunner(
#if USES_XUNIT_3
            IXunitTestAssembly testAssembly,
#else
            ITestAssembly testAssembly,
#endif
            IXunitTestCase[] testCases,
#if !USES_XUNIT_3
            IMessageSink diagnosticMessageSink,
#endif
            IMessageSink executionMessageSink,
            ITestFrameworkExecutionOptions executionOptions)
        {
#if USES_XUNIT_3
            executionOptions = new ExecutionOptionsWrapper(executionOptions);
            testCases = testCases.Select(testCase => new XunitTestCaseWrapper(testCase)).ToArray();
#endif
            return TestInvokerInProc.CreateTestAssemblyRunner(
                testAssembly,
                testCases,
#if !USES_XUNIT_3
                diagnosticMessageSink,
#endif
                executionMessageSink,
                executionOptions);
        }

#if !USES_XUNIT_3 // potentially dead code, even for xUnit 2? - https://github.com/microsoft/vs-extension-testing/pull/177
        public Tuple<decimal, Exception> InvokeTest(
#if USES_XUNIT_3
            IXunitTest test,
#else
            ITest test,
#endif
            IMessageBus messageBus,
            Type testClass,
            object?[]? constructorArguments,
            MethodInfo testMethod,
            object?[]? testMethodArguments)
        {
            using var marshalledObjects = new MarshalledObjects();
            if (constructorArguments != null)
            {
                if (constructorArguments.OfType<ITestOutputHelper>().Any())
                {
                    constructorArguments = (object?[])constructorArguments.Clone();
                    for (int i = 0; i < constructorArguments.Length; i++)
                    {
                        if (constructorArguments[i] is ITestOutputHelper testOutputHelper)
                        {
                            var wrapper = new TestOutputHelperWrapper(testOutputHelper);
                            constructorArguments[i] = wrapper;
                            marshalledObjects.Add(wrapper);
                        }
                    }
                }
            }

            return TestInvokerInProc.InvokeTest(
                test,
                messageBus,
                testClass,
                constructorArguments,
                testMethod,
                testMethodArguments);
        }
#endif

        private class TestOutputHelperWrapper : MarshalByRefObject, ITestOutputHelper
        {
            private readonly ITestOutputHelper _testOutputHelper;

            public TestOutputHelperWrapper(ITestOutputHelper testOutputHelper)
            {
                _testOutputHelper = testOutputHelper;
            }

#if USES_XUNIT_3
            public string Output => _testOutputHelper.Output;

            public void Write(string message)
            {
                throw new NotImplementedException();
            }

            public void Write(string format, params object[] args)
            {
                throw new NotImplementedException();
            }
#endif

            public void WriteLine(string message)
            {
                _testOutputHelper.WriteLine(message);
            }

#if USES_XUNIT_3
            public void WriteLine(string format, params object[] args)
#else
            public void WriteLine(string format, params object?[] args)
#endif
            {
                _testOutputHelper.WriteLine(format, args);
            }

            // The life of this object is managed explicitly
            public override object? InitializeLifetimeService()
            {
                return null;
            }
        }
    }
}
