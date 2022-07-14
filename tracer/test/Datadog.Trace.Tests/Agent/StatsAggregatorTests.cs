// <copyright file="StatsAggregatorTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.Agent;
using Datadog.Trace.Configuration;
using Datadog.Trace.ExtensionMethods;
using FluentAssertions;
using Moq;
using Xunit;

namespace Datadog.Trace.Tests.Agent
{
    public class StatsAggregatorTests
    {
        [Fact]
        public async Task CallFlushAutomatically()
        {
            var bucketDuration = TimeSpan.FromSeconds(1);

            var mutex = new ManualResetEventSlim();

            int invocationCount = 0;

            var api = new Mock<IApi>();
            api.Setup(a => a.SendStatsAsync(It.IsAny<StatsBuffer>(), bucketDuration.ToNanoseconds()))
                .Callback(
                    () =>
                    {
                        if (Interlocked.Increment(ref invocationCount) == 2)
                        {
                            mutex.Set();
                        }
                    })
                .Returns(Task.FromResult(true));

            var aggregator = new StatsAggregator(api.Object, GetSettings(), bucketDuration);

            try
            {
                var stopwatch = Stopwatch.StartNew();

                bool success = false;

                while (stopwatch.Elapsed.Minutes < 1)
                {
                    // Flush is not called if no spans are processed
                    aggregator.Add(new Span(new SpanContext(1, 1), DateTime.UtcNow));

                    if (mutex.Wait(TimeSpan.FromMilliseconds(100)))
                    {
                        success = true;
                        break;
                    }
                }

                success.Should().BeTrue();
            }
            finally
            {
                await aggregator.DisposeAsync();
            }
        }

        [Fact]
        public async Task EmptyBuckets()
        {
            var api = new Mock<IApi>();

            // First, validate that Flush does call SendStatsAsync even if disposed
            // If this behavior change then the test needs to be rewritten
            var aggregator = new StatsAggregator(api.Object, GetSettings(), Timeout.InfiniteTimeSpan);

            // Dispose immediately to make Flush complete without delay
            await aggregator.DisposeAsync();

            aggregator.Add(new Span(new SpanContext(1, 2), DateTimeOffset.UtcNow));

            await aggregator.Flush();

            // Make sure that SendStatsAsync was called
            api.Verify(a => a.SendStatsAsync(It.IsAny<StatsBuffer>(), It.IsAny<long>()), Times.Once);
            api.Reset();

            // Now the actual test
            aggregator = new StatsAggregator(api.Object, GetSettings(), Timeout.InfiniteTimeSpan);
            await aggregator.DisposeAsync();

            await aggregator.Flush();

            // No span is pushed so SendStatsAsync shouldn't be called
            api.Verify(a => a.SendStatsAsync(It.IsAny<StatsBuffer>(), It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task OnlyCollectsTopLevelSpansAndMeasuredSpans()
        {
            const int millisecondsToNanoseconds = 1_000_000;

            // All spans should be recorded except childSpan and snapshotSpan
            const long expectedTotalDuration = (100 + 200 + 300 + 500) * millisecondsToNanoseconds;
            const long expectedOkDuration = (100 + 300 + 500) * millisecondsToNanoseconds;
            const long expectedErrorDuration = 200 * millisecondsToNanoseconds;

            const long expectedHttpClientTotalDuration = 700 * millisecondsToNanoseconds;
            const long expectedHttpClientOkDuration = 700 * millisecondsToNanoseconds;

            var aggregator = new StatsAggregator(Mock.Of<IApi>(), GetSettings(), Timeout.InfiniteTimeSpan);

            try
            {
                var start = DateTimeOffset.UtcNow;

                var simpleSpan = new Span(new SpanContext(1, 1, serviceName: "service"), start);
                simpleSpan.SetDuration(TimeSpan.FromMilliseconds(100));

                var errorSpan = new Span(new SpanContext(2, 2, serviceName: "service"), start);
                errorSpan.Error = true;
                errorSpan.SetDuration(TimeSpan.FromMilliseconds(200));

                var parentSpan = new Span(new SpanContext(3, 3, serviceName: "service"), start);
                parentSpan.SetDuration(TimeSpan.FromMilliseconds(300));

                // childSpan shouldn't be recorded, because it's not top-level and doesn't have the Measured tag
                var childSpan = new Span(new SpanContext(parentSpan.Context, new TraceContext(Mock.Of<IDatadogTracer>()), "service"), start);
                childSpan.SetDuration(TimeSpan.FromMilliseconds(400));

                var measuredChildSpan = new Span(new SpanContext(parentSpan.Context, new TraceContext(Mock.Of<IDatadogTracer>()), "service"), start);
                measuredChildSpan.SetTag(Tags.Measured, "1");
                measuredChildSpan.SetDuration(TimeSpan.FromMilliseconds(500));

                // snapshotSpan shouldn't be recorded, because it has the PartialSnapshot metric (even though it is top-level)
                var snapshotSpan = new Span(new SpanContext(4, 4, serviceName: "service"), start);
                snapshotSpan.SetMetric(Tags.PartialSnapshot, 1.0);
                snapshotSpan.SetDuration(TimeSpan.FromMilliseconds(600));

                // Create a new child span that is a service entry span, which means it will have stats computed for it
                var httpClientServiceSpan = new Span(new SpanContext(parentSpan.Context, new TraceContext(Mock.Of<IDatadogTracer>()), "service-http-client"), start);
                httpClientServiceSpan.SetDuration(TimeSpan.FromMilliseconds(700));

                aggregator.Add(simpleSpan, errorSpan, parentSpan, childSpan, measuredChildSpan, snapshotSpan, childServiceEntrySpan);

                var buffer = aggregator.CurrentBuffer;

                buffer.Buckets.Should().HaveCount(2);

                var serviceKey = StatsAggregator.BuildKey(simpleSpan);
                buffer.Buckets.Should().ContainKey(serviceKey);
                var serviceBucket = buffer.Buckets[serviceKey];

                serviceBucket.Duration.Should().Be(expectedTotalDuration);
                serviceBucket.Hits.Should().Be(4);
                serviceBucket.Errors.Should().Be(1);
                serviceBucket.TopLevelHits.Should().Be(3);
                serviceBucket.ErrorSummary.GetCount().Should().Be(1.0);
                serviceBucket.ErrorSummary.GetSum().Should().BeApproximately(
                    expectedErrorDuration, expectedErrorDuration * serviceBucket.ErrorSummary.IndexMapping.RelativeAccuracy);
                serviceBucket.OkSummary.GetCount().Should().Be(3.0);
                serviceBucket.OkSummary.GetSum().Should().BeApproximately(
                    expectedOkDuration, expectedOkDuration * serviceBucket.OkSummary.IndexMapping.RelativeAccuracy);

                var httpClientServiceKey = StatsAggregator.BuildKey(httpClientServiceSpan);
                buffer.Buckets.Should().ContainKey(httpClientServiceKey);
                var httpClientServiceBucket = buffer.Buckets[httpClientServiceKey];

                httpClientServiceBucket.Duration.Should().Be(expectedHttpClientTotalDuration);
                httpClientServiceBucket.Hits.Should().Be(1);
                httpClientServiceBucket.Errors.Should().Be(0);
                httpClientServiceBucket.TopLevelHits.Should().Be(1);
                httpClientServiceBucket.ErrorSummary.GetCount().Should().Be(0);
                httpClientServiceBucket.ErrorSummary.GetSum().Should().Be(0);
                httpClientServiceBucket.OkSummary.GetCount().Should().Be(1.0);
                httpClientServiceBucket.OkSummary.GetSum().Should().BeApproximately(
                    expectedHttpClientOkDuration, expectedHttpClientOkDuration * httpClientServiceBucket.OkSummary.IndexMapping.RelativeAccuracy);
            }
            finally
            {
                await aggregator.DisposeAsync();
            }
        }

        [Fact]
        public async Task CreatesDistinctBuckets()
        {
            const int millisecondsToNanoseconds = 1_000_000;
            const long durationMs = 100;
            const long duration = durationMs * millisecondsToNanoseconds;

            ulong id = 0;
            var start = DateTimeOffset.UtcNow;

            var aggregator = new StatsAggregator(Mock.Of<IApi>(), GetSettings(), Timeout.InfiniteTimeSpan);

            try
            {
                // Baseline
                var baselineSpan = CreateSpan(id++, start, durationMs);

                // Unique Name (Operation)
                var operationSpan = CreateSpan(id++, start, durationMs, operationName: "unique-name");

                // Unique Resource
                var resourceSpan = CreateSpan(id++, start, durationMs, resourceName: "unique-resource");

                // Unique Service
                var serviceSpan = CreateSpan(id++, start, durationMs, serviceName: "unique-service");

                // Unique Type
                var typeSpan = CreateSpan(id++, start, durationMs, type: "unique-type");

                // Unique Synthetics
                var syntheticsSpan = CreateSpan(id++, start, durationMs, origin: "synthetics");

                // Unique HTTP Status Code
                var httpSpan = CreateSpan(id++, start, durationMs, httpStatusCode: "400");

                var spans = new Span[] { baselineSpan, operationSpan, resourceSpan, serviceSpan, typeSpan, syntheticsSpan, httpSpan };
                aggregator.Add(spans);

                var buffer = aggregator.CurrentBuffer;
                buffer.Buckets.Should().HaveCount(7);

                foreach (var span in spans)
                {
                    var key = StatsAggregator.BuildKey(span);
                    buffer.Buckets.Should().ContainKey(key);

                    var bucket = buffer.Buckets[key];
                    bucket.Duration.Should().Be(duration);
                    bucket.Hits.Should().Be(1);
                    bucket.Errors.Should().Be(0);
                    bucket.TopLevelHits.Should().Be(1);
                    bucket.ErrorSummary.GetCount().Should().Be(0);
                    bucket.ErrorSummary.GetSum().Should().Be(0);
                    bucket.OkSummary.GetCount().Should().Be(1.0);
                    bucket.OkSummary.GetSum().Should().BeApproximately(
                        duration, duration * bucket.OkSummary.IndexMapping.RelativeAccuracy);
                }
            }
            finally
            {
                await aggregator.DisposeAsync();
            }

            Span CreateSpan(ulong id, DateTimeOffset start, long durationMs, string operationName = "name", string resourceName = "resource", string serviceName = "service", string type = "http", string httpStatusCode = "200", string origin = "rum")
            {
                var span = new Span(new SpanContext(id, id), start);
                span.SetDuration(TimeSpan.FromMilliseconds(durationMs));

                span.ResourceName = resourceName;
                span.ServiceName = serviceName;
                span.OperationName = operationName;
                span.Type = type;
                span.SetTag(Tags.HttpStatusCode, httpStatusCode);
                span.Context.Origin = origin;

                return span;
            }
        }

        private static ImmutableTracerSettings GetSettings() => new TracerSettings().Build();
    }
}
