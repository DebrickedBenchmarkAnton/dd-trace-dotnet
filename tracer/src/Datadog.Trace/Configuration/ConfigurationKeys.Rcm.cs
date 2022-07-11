// <copyright file="ConfigurationKeys.Rcm.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Datadog.Trace.Debugger;
using Datadog.Trace.RemoteConfigurationManagement;

namespace Datadog.Trace.Configuration
{
    internal static partial class ConfigurationKeys
    {
        internal static class Rcm
        {
            /// <summary>
            /// Configuration key for RCM poll interval (in seconds).
            /// Default and maximum value is 5000 ms
            /// </summary>
            /// <seealso cref="RemoteConfigurationSettings.PollInterval"/>
            public const string PollInterval = "DD_RCM_POLL_INTERVAL";

            /// <summary>
            /// Configuration key for RCM configuration file full path on the disk.
            /// For internal usage only.
            /// </summary>
            /// <seealso cref="RemoteConfigurationSettings.FilePath"/>
            public const string FilePath = "DD_INTERNAL_RCM_FILE";
        }
    }
}
