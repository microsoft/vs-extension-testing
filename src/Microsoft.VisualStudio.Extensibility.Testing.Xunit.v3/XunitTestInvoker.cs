// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#if USES_XUNIT_3

namespace Xunit.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit.v3;

#if USES_XUNIT_3
    using BeforeAfterTestAttributeType = Xunit.v3.IBeforeAfterTestAttribute;
#else
    using BeforeAfterTestAttributeType = Xunit.Sdk.BeforeAfterTestAttribute;
#endif

    public class XunitTestInvoker
    {
        private static MethodInfo? _startAsTaskOpenGenericMethod;

        public XunitTestInvoker(IXunitTest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod, object[] testMethodArguments, IReadOnlyList<BeforeAfterTestAttributeType> beforeAfterAttributes, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        {
            Test = test;
            MessageBus = messageBus;
            TestClass = testClass;
            ConstructorArguments = constructorArguments;
            TestMethod = testMethod;
            TestMethodArguments = testMethodArguments;
            BeforeAfterAttributes = beforeAfterAttributes;
            Aggregator = aggregator;
            CancellationTokenSource = cancellationTokenSource;
        }

        public IXunitTest Test { get; }

        public IMessageBus MessageBus { get; }

        public Type TestClass { get; }

        public object[] ConstructorArguments { get; }

        public MethodInfo TestMethod { get; }

        public object[] TestMethodArguments { get; }

        public IReadOnlyList<BeforeAfterTestAttributeType> BeforeAfterAttributes { get; }

        public ExceptionAggregator Aggregator { get; }

        public CancellationTokenSource CancellationTokenSource { get; }

        protected virtual object CreateTestClass()
        {
            object? testClass = null;

            if (!TestMethod.IsStatic && !Aggregator.HasExceptions)
            {
                testClass = Test.CreateTestClass(TestClass, ConstructorArguments, MessageBus, CancellationTokenSource);
            }

            return testClass!;
        }

        protected virtual object CallTestMethod(object testClassInstance)
            => TestMethod.Invoke(testClassInstance, TestMethodArguments);

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        public static Task? GetTaskFromResult(object obj)
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        {
            if (obj == null)
            {
                return null;
            }

            var task = obj as Task;
            if (task != null)
            {
                return task;
            }

            var type = obj.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition().FullName == "Microsoft.FSharp.Control.FSharpAsync`1")
            {
                if (_startAsTaskOpenGenericMethod == null)
                {
                    _startAsTaskOpenGenericMethod = type.Assembly.GetType("Microsoft.FSharp.Control.FSharpAsync")
                                                                     .GetRuntimeMethods()
                                                                     .FirstOrDefault(m => m.Name == "StartAsTask");
                }

                return _startAsTaskOpenGenericMethod.MakeGenericMethod(type.GetGenericArguments()[0])
                                                   .Invoke(null, new[] { obj, null, null }) as Task;
            }

            return null;
        }
    }
}

#endif
