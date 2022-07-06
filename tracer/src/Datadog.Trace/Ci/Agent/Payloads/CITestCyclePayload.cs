// <copyright file="CITestCyclePayload.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using Datadog.Trace.Ci.EventModel;

namespace Datadog.Trace.Ci.Agent.Payloads
{
    internal class CITestCyclePayload : EventsPayload
    {
        public CITestCyclePayload()
        {
            EvpSubdomain = "citestcycle-intake";
            EvpPath = "api/v2/citestcycle";

            if (CIVisibility.Settings.Agentless)
            {
                if (!string.IsNullOrWhiteSpace(CIVisibility.Settings.AgentlessUrl))
                {
                    var builder = new UriBuilder(CIVisibility.Settings.AgentlessUrl);
                    builder.Path = EvpPath;
                    Url = builder.Uri;
                }
                else
                {
                    var builder = new UriBuilder("https://datadog.host.com/api/v2/citestcycle");
                    builder.Host = $"{EvpSubdomain}.{CIVisibility.Settings.Site}";
                    builder.Path = EvpPath;
                    Url = builder.Uri;
                }
            }
            else
            {
                var builder = new UriBuilder("http://localhost:8126");
                builder.Path = $"/evp_proxy/v1/{EvpPath}";
                Url = builder.Uri;
            }
        }

        public override Uri Url { get; }

        public override string EvpSubdomain { get; }

        public override string EvpPath { get; }

        public override bool CanProcessEvent(IEvent @event)
        {
            // This intake accepts both Span and Test events
            if (@event is SpanEvent or TestEvent)
            {
                return true;
            }

            return false;
        }
    }
}
