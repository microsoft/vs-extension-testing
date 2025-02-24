// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.OutOfProcess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Remoting;
#if !USES_XUNIT_3
    using Xunit.Abstractions;
#endif
    using Xunit.Harness;
    using Xunit.InProcess;
    using Xunit.Sdk;
#if USES_XUNIT_3
    using Xunit.v3;
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

        public InProcessIdeTestAssemblyRunner CreateTestAssemblyRunner(ITestAssembly testAssembly, IXunitTestCase[] testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
        {
            return TestInvokerInProc.CreateTestAssemblyRunner(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions);
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
