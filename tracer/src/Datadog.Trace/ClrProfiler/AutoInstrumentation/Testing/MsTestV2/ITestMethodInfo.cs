// <copyright file="ITestMethodInfo.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Reflection;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.MsTestV2
{
    /// <summary>
    /// TestMethodInfo ducktype interface
    /// </summary>
    internal interface ITestMethodInfo : ITestMethod
    {
        /// <summary>
        /// Gets or sets testMethod referred by this object
        /// </summary>
        MethodInfo TestMethod { get; set; }
    }
}
