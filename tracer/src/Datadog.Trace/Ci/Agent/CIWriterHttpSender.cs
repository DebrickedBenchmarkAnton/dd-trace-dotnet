// <copyright file="CIWriterHttpSender.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Datadog.Trace.Agent;
using Datadog.Trace.Agent.Transports;
using Datadog.Trace.Ci.Agent.Payloads;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;

namespace Datadog.Trace.Ci.Agent
{
    internal sealed class CIWriterHttpSender : ICIAgentlessWriterSender
    {
        private const string ApiKeyHeader = "dd-api-key";
        private const string EvpSubdomainHeader = "X-Datadog-EVP-Subdomain";
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<CIWriterHttpSender>();

        private readonly IApiRequestFactory _apiRequestFactory;
        private readonly GlobalSettings _globalSettings;

        public CIWriterHttpSender(IApiRequestFactory apiRequestFactory)
        {
            _apiRequestFactory = apiRequestFactory;
            _globalSettings = GlobalSettings.FromDefaultSources();
            Log.Information("CIWriterHttpSender Initialized.");
        }

        public async Task SendPayloadAsync(CIVisibilityProtocolPayload payload)
        {
            var numberOfTraces = payload.Count;
            var payloadMimeType = MimeTypes.MsgPack;
            var payloadBytes = payload.ToArray();
            Log.Information<int, string>("Sending ({numberOfTraces} events) {bytesValue} bytes...", numberOfTraces, payloadBytes.Length.ToString("N0"));
            await SendPayloadAsync(payload.Url, payload, req => req.PostAsync(new ArraySegment<byte>(payloadBytes), payloadMimeType)).ConfigureAwait(false);
        }

        public async Task SendPayloadAsync(CIVisibilityMultipartPayload payload)
        {
            Log.Information<int>("Sending {count} coverages...", payload.Count);
            var payloadArray = payload.ToArray();
            await SendPayloadAsync(
                payload.Url,
                payload,
                req =>
                {
                    if (req is IMultipartApiRequest mReq)
                    {
                        return mReq.PostAsync(payloadArray);
                    }

                    return Task.FromResult<IApiResponse>(null);
                }).ConfigureAwait(false);
        }

        private static async Task<bool> SendPayloadAsync(Func<IApiRequest, Task<IApiResponse>> senderFunc, IApiRequest request, bool finalTry)
        {
            IApiResponse response = null;

            try
            {
                try
                {
                    response = await senderFunc(request).ConfigureAwait(false);
                }
                catch
                {
                    // count only network/infrastructure errors, not valid responses with error status codes
                    // (which are handled below)
                    throw;
                }

                // Attempt a retry if the status code is not SUCCESS
                if (response.StatusCode < 200 || response.StatusCode >= 300)
                {
                    if (finalTry)
                    {
                        try
                        {
                            string responseContent = await response.ReadAsStringAsync().ConfigureAwait(false);
                            Log.Error<int, string>("Failed to submit events with status code {StatusCode} and message: {ResponseContent}", response.StatusCode, responseContent);
                        }
                        catch (Exception ex)
                        {
                            Log.Error<int>(ex, "Unable to read response for failed request with status code {StatusCode}", response.StatusCode);
                        }
                    }

                    return false;
                }
            }
            finally
            {
                response?.Dispose();
            }

            return true;
        }

        private async Task SendPayloadAsync(Uri url, EvpPayload evpPayload, Func<IApiRequest, Task<IApiResponse>> senderFunc)
        {
            // retry up to 5 times with exponential back-off
            const int retryLimit = 5;
            var retryCount = 1;
            var sleepDuration = 100; // in milliseconds

            while (true)
            {
                IApiRequest request;

                try
                {
                    request = _apiRequestFactory.Create(url);
                    request.AddHeader(ApiKeyHeader, CIVisibility.Settings.ApiKey);
                    request.AddHeader(EvpSubdomainHeader, evpPayload.EvpSubdomain);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An error occurred while generating http request to send events to {AgentEndpoint}", _apiRequestFactory.Info(url));
                    return;
                }

                bool success = false;
                Exception exception = null;
                bool isFinalTry = retryCount >= retryLimit;

                try
                {
                    success = await SendPayloadAsync(senderFunc, request, isFinalTry).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exception = ex;

                    if (_globalSettings.DebugEnabled)
                    {
                        if (ex.InnerException is InvalidOperationException ioe)
                        {
                            Log.Error<string>(ex, "An error occurred while sending events to {AgentEndpoint}", _apiRequestFactory.Info(url));
                            return;
                        }
                    }
                }

                // Error handling block
                if (!success)
                {
                    if (isFinalTry)
                    {
                        // stop retrying
                        Log.Error<int, string>(exception, "An error occurred while sending events after {Retries} retries to {AgentEndpoint}", retryCount, _apiRequestFactory.Info(url));
                        return;
                    }

                    // Before retry delay
                    bool isSocketException = false;
                    Exception innerException = exception;

                    while (innerException != null)
                    {
                        if (innerException is SocketException)
                        {
                            isSocketException = true;
                            break;
                        }

                        innerException = innerException.InnerException;
                    }

                    if (isSocketException)
                    {
                        Log.Debug(exception, "Unable to communicate with {AgentEndpoint}", _apiRequestFactory.Info(url));
                    }

                    // Execute retry delay
                    await Task.Delay(sleepDuration).ConfigureAwait(false);
                    retryCount++;
                    sleepDuration *= 2;

                    continue;
                }

                Log.Debug<string>("Successfully sent events to {AgentEndpoint}", _apiRequestFactory.Info(url));
                return;
            }
        }
    }
}
