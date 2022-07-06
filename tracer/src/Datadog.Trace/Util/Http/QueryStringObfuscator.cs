// <copyright file="QueryStringObfuscator.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.Logging;

namespace Datadog.Trace.Util.Http
{
    internal class QueryStringObfuscator
    {
        /// <summary>
        /// Default obfuscation query string regex if none specified via env DD_OBFUSCATION_QUERY_STRING_REGEXP
        /// </summary>
        public const string DefaultObfuscationQueryStringRegex = @"((?i)(?:p(?:ass)?w(?:or)?d|pass(?:_?phrase)?|secret|(?:api_?|private_?|public_?|access_?|secret_?)key(?:_?id)?|token|consumer_?(?:id|key|secret)|sign(?:ed|ature)?|auth(?:entication|orization)?)(?:(?:\s|%20)*(?:=|%3D)[^&]+|(?:""|%22)(?:\s|%20)*(?::|%3A)(?:\s|%20)*(?:""|%22)(?:%2[^2]|%[^2]|[^""%])+(?:""|%22))|bearer(?:\s|%20)+[a-z0-9\._\-]|token(?::|%3A)[a-z0-9]{13}|gh[opsu]_[0-9a-zA-Z]{36}|ey[I-L](?:[\w=-]|%3D)+\.ey[I-L](?:[\w=-]|%3D)+(?:\.(?:[\w.+\/=-]|%3D|%2F|%2B)+)?|[\-]{5}BEGIN(?:[a-z\s]|%20)+PRIVATE(?:\s|%20)KEY[\-]{5}[^\-]+[\-]{5}END(?:[a-z\s]|%20)+PRIVATE(?:\s|%20)KEY|ssh-rsa(?:\s|%20)*(?:[a-z0-9\/\.+]|%2F|%5C|%2B){100,})";

        private static readonly IDatadogLogger _log = DatadogLogging.GetLoggerFor(typeof(QueryStringObfuscator));
        private static QueryStringObfuscator _instance;
        private static bool _globalInstanceInitialized;
        private static object _globalInstanceLock = new();
        private readonly Obfuscator _obfuscator;

        private QueryStringObfuscator(double timeout, string pattern = null)
        {
            _log.Warning($"Instantiation QueryStringObfuscator with timeout double {timeout}");
            pattern ??= Tracer.Instance.Settings.ObfuscationQueryStringRegex;
            _obfuscator = new(TimeSpan.FromMilliseconds(timeout), pattern);
        }

        internal string Obfuscate(string queryString) => _obfuscator.Obfuscate(queryString);

        /// <summary>
        /// Gets or sets the global <see cref="QueryStringObfuscator"/> instance.
        /// </summary>
        public static QueryStringObfuscator Instance(double timeout, string pattern = null) => LazyInitializer.EnsureInitialized(ref _instance, ref _globalInstanceInitialized, ref _globalInstanceLock, () => new(timeout, pattern));

        internal class Obfuscator
        {
            private const string ReplacementString = "<redacted>";
            private readonly Regex _regex;
            private readonly bool _disabled;
            private readonly TimeSpan _timeout;

            internal Obfuscator(TimeSpan timeout, string pattern = null)
            {
                _log.Warning($"Instantiation obfuscator with timeout {timeout}, {timeout.Milliseconds}");
                _timeout = timeout;
                if (string.IsNullOrEmpty(pattern))
                {
                    _disabled = true;
                }
                else
                {
                    _regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                }
            }

            internal string Obfuscate(string queryString)
            {
                if (_disabled || string.IsNullOrEmpty(queryString))
                {
                    return queryString;
                }

                var cancelationToken = new CancellationTokenSource();
                try
                {
                    _log.Warning($"task will run with timeout {_timeout.Milliseconds} ms");
                    var task = Task.Factory.StartNew(() => _regex.Replace(queryString, ReplacementString), cancelationToken.Token);
                    var timeoutTask = Task.Delay(_timeout, cancelationToken.Token);
                    var tasks = new[] { task, timeoutTask };
                    Task.WaitAny(tasks);
                    if (task.Status == TaskStatus.RanToCompletion)
                    {
                        return task.Result;
                    }

                    if (timeoutTask.Status == TaskStatus.RanToCompletion)
                    {
                        Log($"The timeout task of {_timeout.Milliseconds} ms ran to completion before the regex's task which status is {task.Status}", task.Exception);
                    }
                    else
                    {
                        Log();
                    }
                }
                catch (AggregateException e)
                {
                    Log(exception: e);
                }
                finally
                {
                    cancelationToken.Cancel();
                    cancelationToken.Dispose();
                }

                return string.Empty;
            }

            internal virtual void Log(string message = null, Exception exception = null)
            {
                var messageTemplate = string.Concat("Query string could not be redacted using regex {pattern}", message);
                if (exception != null)
                {
                    _log.Error(exception, messageTemplate, _regex.ToString());
                }
                else
                {
                    _log.Error(messageTemplate, _regex.ToString());
                }
            }
        }
    }
}