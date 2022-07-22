// <copyright file="TraceContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Diagnostics;
using Datadog.Trace.ClrProfiler;
using Datadog.Trace.Logging;
using Datadog.Trace.PlatformHelpers;
using Datadog.Trace.Sampling;
using Datadog.Trace.Tagging;
using Datadog.Trace.Util;

namespace Datadog.Trace
{
    internal class TraceContext
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<TraceContext>();

        private readonly DateTimeOffset _utcStart = DateTimeOffset.UtcNow;
        private readonly long _timestamp = Stopwatch.GetTimestamp();
        private ArrayBuilder<Span> _spans;

        private int _openSpans;
        private SamplingDecision? _samplingDecision;

        public TraceContext(IDatadogTracer tracer, TraceTagCollection tags = null)
        {
            Tracer = tracer;
            Tags = tags ?? new TraceTagCollection();
        }

        public Span RootSpan { get; private set; }

        public DateTimeOffset UtcNow => _utcStart.Add(Elapsed);

        public IDatadogTracer Tracer { get; }

        /// <summary>
        /// Gets the collection of trace-level tags.
        /// </summary>
        public TraceTagCollection Tags { get; }

        /// <summary>
        /// Gets the trace's sampling decision, which includes
        /// priority, mechanism, and rate (if used).
        /// </summary>
        public SamplingDecision? SamplingDecision => _samplingDecision;

        private TimeSpan Elapsed => StopwatchHelpers.GetElapsed(Stopwatch.GetTimestamp() - _timestamp);

        public void AddSpan(Span span)
        {
            lock (this)
            {
                if (RootSpan == null)
                {
                    // first span added is the root span
                    RootSpan = span;
                    DecorateWithAASMetadata(span);

                    if (_samplingDecision == null)
                    {
                        if (span.Context.Parent is SpanContext { SamplingPriority: { } samplingPriority })
                        {
                            // this is a root span created from a propagated context that contains a sampling priority.
                            // no need to track sampling mechanism since we can't override the propagated tag/header.
                            var decision = new SamplingDecision(samplingPriority, SamplingMechanism.Unknown);
                            _samplingDecision = decision;
                        }
                        else if (Tracer.Sampler is not null)
                        {
                            // this is a local root span (i.e. not propagated).
                            // determine an initial sampling priority for this trace, but don't lock it yet
                            _samplingDecision = Tracer.Sampler.MakeSamplingDecision(span);
                        }
                    }
                }

                _openSpans++;
            }
        }

        public void CloseSpan(Span span)
        {
            bool ShouldTriggerPartialFlush() => Tracer.Settings.Exporter.PartialFlushEnabled && _spans.Count >= Tracer.Settings.Exporter.PartialFlushMinSpans;

            if (span == RootSpan)
            {
                if (_samplingDecision == null)
                {
                    Log.Warning("Cannot set span metric for sampling priority before it has been set.");
                }
                else
                {
                    AddSamplingDecisionTags(span, _samplingDecision.Value);
                }
            }

            ArraySegment<Span> spansToWrite = default;

            bool shouldPropagateMetadata = false;

            lock (this)
            {
                _spans.Add(span);
                _openSpans--;

                if (_openSpans == 0)
                {
                    spansToWrite = _spans.GetArray();
                    _spans = default;
                }
                else if (ShouldTriggerPartialFlush())
                {
                    Log.Debug<ulong, ulong, int>(
                        "Closing span {spanId} triggered a partial flush of trace {traceId} with {spanCount} pending spans",
                        span.SpanId,
                        span.TraceId,
                        _spans.Count);

                    // We may not be sending the root span, so we need to propagate the metadata to other spans of the partial trace
                    // There's no point in doing that inside of the lock, so we set a flag for later
                    shouldPropagateMetadata = true;

                    spansToWrite = _spans.GetArray();

                    // Making the assumption that, if the number of closed spans was big enough to trigger partial flush,
                    // the number of remaining spans is probably big as well.
                    // Therefore, we bypass the resize logic and immediately allocate the array to its maximum size
                    _spans = new ArrayBuilder<Span>(spansToWrite.Count);
                }
            }

            if (shouldPropagateMetadata && _samplingDecision != null)
            {
                AddSamplingDecisionTags(spansToWrite, _samplingDecision.Value);
            }

            if (spansToWrite.Count > 0)
            {
                Tracer.Write(spansToWrite);
            }
        }

        public void SetSamplingDecision(int? priority, int mechanism, double? rate = null, bool notifyDistributedTracer = true)
        {
            if (priority == null)
            {
                // remove any previous sampling decision
                SetSamplingDecision(null, notifyDistributedTracer);
            }
            else
            {
                var decision = new SamplingDecision(priority.Value, mechanism, rate);
                SetSamplingDecision(decision, notifyDistributedTracer);
            }
        }

        public void SetSamplingDecision(SamplingDecision? samplingDecision, bool notifyDistributedTracer = true)
        {
            _samplingDecision = samplingDecision;

            if (notifyDistributedTracer)
            {
                DistributedTracer.Instance.SetSamplingPriority(samplingDecision?.Priority);
            }
        }

        public TimeSpan ElapsedSince(DateTimeOffset date)
        {
            return Elapsed + (_utcStart - date);
        }

        private static void AddSamplingDecisionTags(Span span, SamplingDecision samplingDecision)
        {
            // set sampling priority tag directly on this span...
            if (span.Tags is CommonTags tags)
            {
                tags.SamplingPriority = samplingDecision.Priority;
            }
            else
            {
                span.Tags.SetMetric(Metrics.SamplingPriority, samplingDecision.Priority);
            }

            // ...and add the sampling decision trace tag if not already set
            // * we should not overwrite an existing value propagated from upstream service
            // * the "-" prefix is not a typo, it's a left-over separator from a previous iteration of this feature
            // * don't set the tag is sampling mechanism is unknown (-1 is not a valid value, we only use it internally in the tracer)
            if (samplingDecision.Mechanism != SamplingMechanism.Unknown)
            {
                span.Context.TraceContext?.Tags.SetTag(Trace.Tags.Propagated.DecisionMaker, $"-{samplingDecision.Mechanism}", replaceIfExists: false);
            }
        }

        private static void AddSamplingDecisionTags(ArraySegment<Span> spans, SamplingDecision samplingDecision)
        {
            // Needs to be done for chunks as well, any span can contain the tags.
            DecorateWithAASMetadata(spans.Array[0]);

            // The agent looks for the sampling priority on the first span that has no parent
            // Finding those spans is not trivial, so instead we apply the priority to every span

            if (spans.Array != null)
            {
                // Using a for loop to avoid the boxing allocation on ArraySegment.GetEnumerator
                for (int i = 0; i < spans.Count; i++)
                {
                    AddSamplingDecisionTags(spans.Array[i + spans.Offset], samplingDecision);
                }
            }
        }

        /// <summary>
        /// When receiving chunks of spans, the backend checks whether the aas.resource.id tag is present on any of the
        /// span to decide which metric to emit (datadog.apm.host.instance or datadog.apm.azure_resource_instance one).
        /// </summary>
        private static void DecorateWithAASMetadata(Span span)
        {
            if (AzureAppServices.Metadata.IsRelevant)
            {
                span.SetTag(Datadog.Trace.Tags.AzureAppServicesSiteName, AzureAppServices.Metadata.SiteName);
                span.SetTag(Datadog.Trace.Tags.AzureAppServicesSiteKind, AzureAppServices.Metadata.SiteKind);
                span.SetTag(Datadog.Trace.Tags.AzureAppServicesSiteType, AzureAppServices.Metadata.SiteType);
                span.SetTag(Datadog.Trace.Tags.AzureAppServicesResourceGroup, AzureAppServices.Metadata.ResourceGroup);
                span.SetTag(Datadog.Trace.Tags.AzureAppServicesSubscriptionId, AzureAppServices.Metadata.SubscriptionId);
                span.SetTag(Datadog.Trace.Tags.AzureAppServicesResourceId, AzureAppServices.Metadata.ResourceId);
                span.SetTag(Datadog.Trace.Tags.AzureAppServicesInstanceId, AzureAppServices.Metadata.InstanceId);
                span.SetTag(Datadog.Trace.Tags.AzureAppServicesInstanceName, AzureAppServices.Metadata.InstanceName);
                span.SetTag(Datadog.Trace.Tags.AzureAppServicesOperatingSystem, AzureAppServices.Metadata.OperatingSystem);
                span.SetTag(Datadog.Trace.Tags.AzureAppServicesRuntime, AzureAppServices.Metadata.Runtime);
                span.SetTag(Datadog.Trace.Tags.AzureAppServicesExtensionVersion, AzureAppServices.Metadata.SiteExtensionVersion);
            }
        }
    }
}
