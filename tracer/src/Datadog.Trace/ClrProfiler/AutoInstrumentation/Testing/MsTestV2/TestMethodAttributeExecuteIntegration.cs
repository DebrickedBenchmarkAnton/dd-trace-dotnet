// <copyright file="TestMethodAttributeExecuteIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using Datadog.Trace.Ci.Tags;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.MsTestV2
{
    /// <summary>
    /// Microsoft.VisualStudio.TestPlatform.TestFramework.Execute calltarget instrumentation
    /// </summary>
    [InstrumentMethod(
        AssemblyName = "Microsoft.VisualStudio.TestPlatform.TestFramework",
        TypeName = "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute",
        MethodName = "Execute",
        ReturnTypeName = "Microsoft.VisualStudio.TestTools.UnitTesting.TestResult",
        ParameterTypeNames = new[] { "Microsoft.VisualStudio.TestTools.UnitTesting.ITestMethod" },
        MinimumVersion = "14.0.0",
        MaximumVersion = "14.*.*",
        IntegrationName = MsTestIntegration.IntegrationName)]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class TestMethodAttributeExecuteIntegration
    {
        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TTestMethod">Type of the ITestMethod</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="testMethod">Test method instance</param>
        /// <returns>Calltarget state value</returns>
        internal static CallTargetState OnMethodBegin<TTarget, TTestMethod>(TTarget instance, TTestMethod testMethod)
            where TTestMethod : ITestMethod, IDuckType
        {
            if (!MsTestIntegration.IsEnabled)
            {
                return CallTargetState.GetDefault();
            }

            // ***
            // TODO: This is the way to skip tests on MSTest
            // ***
            // if (testMethod.Instance.TryDuckCast<ITestMethodInfo>(out var testMethodInfo))
            // {
            //     var originalMethodInfo = testMethodInfo.TestMethod;
            //     var typesArray = new Type[originalMethodInfo.GetParameters().Length];
            //     for (var i = 0; i < typesArray.Length; i++)
            //     {
            //         typesArray[i] = typeof(object);
            //     }
            //
            //     var replaceMethodInfo = new DynamicMethod(originalMethodInfo.Name, typeof(void), typesArray, originalMethodInfo.Module, true);
            //     var dynMethodIl = replaceMethodInfo.GetILGenerator();
            //     dynMethodIl.Emit(OpCodes.Ret);
            //
            //     var itrScope = MsTestIntegration.OnMethodBegin(testMethod, testMethod.Type);
            //     testMethodInfo.TestMethod = replaceMethodInfo;
            //     return new CallTargetState(itrScope, originalMethodInfo);
            // }

            var scope = MsTestIntegration.OnMethodBegin(testMethod, testMethod.Type);
            return new CallTargetState(scope);
        }

        /// <summary>
        /// OnMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TReturn">Type of the return value</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="returnValue">Return value</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A response value, in an async scenario will be T of Task of T</returns>
        internal static CallTargetReturn<TReturn> OnMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, in CallTargetState state)
        {
            if (MsTestIntegration.IsEnabled)
            {
                Scope scope = state.Scope;
                if (scope != null)
                {
                    bool useZeroTimeout = false;
                    if (returnValue is Array { Length: 1 } returnValueArray && returnValueArray.GetValue(0) is { } testResultObject)
                    {
                        string errorMessage = null;
                        string errorStackTrace = null;

                        // ***
                        // TODO: This is the way to skip tests on MSTest
                        // ***
                        // if (state.State is MethodInfo originalMethodInfo && testResultObject.TryDuckCast<ITestResult>(out var tResult))
                        // {
                        //     // This will appear as a Skipped in the dotnet test CLI
                        //     tResult.Outcome = UnitTestOutcome.Inconclusive;
                        //     useZeroTimeout = true;
                        //     errorMessage = "Skipped by ITR";
                        // }

                        if (testResultObject.TryDuckCast<TestResultStruct>(out var testResult))
                        {
                            if (testResult.TestFailureException != null)
                            {
                                Exception testException = testResult.TestFailureException.InnerException ?? testResult.TestFailureException;
                                string testExceptionName = testException.GetType().Name;
                                if (testExceptionName != "UnitTestAssertException" && testExceptionName != "AssertInconclusiveException")
                                {
                                    scope.Span.SetException(testException);
                                }

                                errorMessage = testException.Message;
                                errorStackTrace = testException.ToString();
                            }

                            switch (testResult.Outcome)
                            {
                                case UnitTestOutcome.Error:
                                case UnitTestOutcome.Failed:
                                case UnitTestOutcome.Timeout:
                                    scope.Span.SetTag(TestTags.Status, TestTags.StatusFail);
                                    scope.Span.Error = true;
                                    scope.Span.SetTag(Tags.ErrorMsg, errorMessage);
                                    scope.Span.SetTag(Tags.ErrorStack, errorStackTrace);
                                    break;
                                case UnitTestOutcome.Inconclusive:
                                    scope.Span.SetTag(TestTags.Status, TestTags.StatusSkip);
                                    scope.Span.SetTag(TestTags.SkipReason, errorMessage);
                                    break;
                                case UnitTestOutcome.NotRunnable:
                                    scope.Span.SetTag(TestTags.Status, TestTags.StatusSkip);
                                    scope.Span.SetTag(TestTags.SkipReason, errorMessage);
                                    useZeroTimeout = true;
                                    break;
                                case UnitTestOutcome.Passed:
                                    scope.Span.SetTag(TestTags.Status, TestTags.StatusPass);
                                    break;
                            }
                        }
                    }

                    if (exception != null)
                    {
                        scope.Span.SetException(exception);
                        scope.Span.SetTag(TestTags.Status, TestTags.StatusFail);
                    }

                    var coverageSession = Ci.Coverage.CoverageReporter.Handler.EndSession();
                    if (coverageSession is not null)
                    {
                        scope.Span.SetTag("test.coverage", Datadog.Trace.Vendors.Newtonsoft.Json.JsonConvert.SerializeObject(coverageSession));
                    }

                    if (useZeroTimeout)
                    {
                        scope.Span.Finish(TimeSpan.Zero);
                    }

                    scope.Dispose();
                }
            }

            return new CallTargetReturn<TReturn>(returnValue);
        }
    }
}
