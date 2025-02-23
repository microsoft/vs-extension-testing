// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#if USES_XUNIT_3

namespace Xunit.Sdk
{
    using System;
    using System.Threading;
    using Xunit.v3;

    internal static class PolyfillExtensions
    {
        public static object? CreateTestClass(
            this ITest test,
            Type testClassType,
            object[] constructorArguments,
            IMessageBus messageBus,
            CancellationTokenSource cancellationTokenSource)
        {
            object? testClass = null;

            if (!messageBus.QueueMessage(new TestClassConstructionStarting()
            {
                AssemblyUniqueID = test.TestCase.TestCollection.TestAssembly.UniqueID,
                TestCaseUniqueID = test.TestCase.UniqueID,
                TestClassUniqueID = test.TestCase.TestClass?.UniqueID,
                TestCollectionUniqueID = test.TestCase.TestCollection.UniqueID,
                TestMethodUniqueID = test.TestCase.TestMethod?.UniqueID,
                TestUniqueID = test.UniqueID,
            }))
            {
                cancellationTokenSource.Cancel();
            }
            else
            {
                try
                {
                    if (!cancellationTokenSource.IsCancellationRequested)
                    {
                        testClass = Activator.CreateInstance(testClassType, constructorArguments);
                    }
                }
                finally
                {
                    if (!messageBus.QueueMessage(new TestClassConstructionFinished()
                    {
                        AssemblyUniqueID = test.TestCase.TestCollection.TestAssembly.UniqueID,
                        TestCaseUniqueID = test.TestCase.UniqueID,
                        TestClassUniqueID = test.TestCase.TestClass?.UniqueID,
                        TestCollectionUniqueID = test.TestCase.TestCollection.UniqueID,
                        TestMethodUniqueID = test.TestCase.TestMethod?.UniqueID,
                        TestUniqueID = test.UniqueID,
                    }))
                    {
                        cancellationTokenSource.Cancel();
                    }
                }
            }

            return testClass;
        }

        public static void DisposeTestClass(this ITest test, object testClass, IMessageBus messageBus, CancellationTokenSource cancellationTokenSource)
        {
            if (!(testClass is IDisposable disposable))
            {
                return;
            }

            if (!messageBus.QueueMessage(new TestClassDisposeStarting()
            {
                AssemblyUniqueID = test.TestCase.TestCollection.TestAssembly.UniqueID,
                TestCaseUniqueID = test.TestCase.UniqueID,
                TestClassUniqueID = test.TestCase.TestClass?.UniqueID,
                TestCollectionUniqueID = test.TestCase.TestCollection.UniqueID,
                TestMethodUniqueID = test.TestCase.TestMethod?.UniqueID,
                TestUniqueID = test.UniqueID,
            }))
            {
                cancellationTokenSource.Cancel();
                return;
            }

            try
            {
                disposable.Dispose();
            }
            finally
            {
                if (!messageBus.QueueMessage(new TestClassDisposeFinished()
                {
                    AssemblyUniqueID = test.TestCase.TestCollection.TestAssembly.UniqueID,
                    TestCaseUniqueID = test.TestCase.UniqueID,
                    TestClassUniqueID = test.TestCase.TestClass?.UniqueID,
                    TestCollectionUniqueID = test.TestCase.TestCollection.UniqueID,
                    TestMethodUniqueID = test.TestCase.TestMethod?.UniqueID,
                    TestUniqueID = test.UniqueID,
                }))
                {
                    cancellationTokenSource.Cancel();
                }
            }
        }
    }
}
#endif
