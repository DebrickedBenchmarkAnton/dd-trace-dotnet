// <copyright file="AspNetCore5RateLimiter.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NETCOREAPP3_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Datadog.Trace.TestHelpers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.Security.IntegrationTests
{
    public class AspNetCore5RateLimiter : AspNetBase, IDisposable
    {
        public AspNetCore5RateLimiter(ITestOutputHelper outputHelper)
            : base("AspNetCore5", outputHelper, "/shutdown")
        {
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [Trait("RunOnWindows", "True")]
        [Trait("Category", "ArmUnsupported")]
        public async Task TestRateLimiterSecurity(bool enableSecurity, string url = DefaultAttackUrl)
        {
            var agent = await RunOnSelfHosted(enableSecurity, false, traceRateLimit: new int?(30));
            var limit = 30;
            var totalRequests = 120;
            int excess = Math.Abs(totalRequests - limit);
            var spans = await SendRequestsAsync(agent, url, totalRequests, totalRequests);
            var spansWithUserKeep = spans.Where(s => s.Metrics["_sampling_priority_v1"] == 2.0);
            var spansWithoutUserKeep = spans.Where(s => s.Metrics["_sampling_priority_v1"] != 2.0);
            if (enableSecurity)
            {
                spansWithUserKeep.Count().Should().BeCloseTo(limit, (uint)(limit * 0.15), "can't be sure it's in the same second");
                int rest = totalRequests - spansWithUserKeep.Count();
                spansWithoutUserKeep.Count().Should().Be(rest);
                spansWithoutUserKeep.Should().Contain(s => s.Metrics.ContainsKey("_dd.appsec.rate_limit.dropped_traces"));
            }
            else
            {
                spansWithoutUserKeep.Count().Should().Be(totalRequests);
                spansWithoutUserKeep.Should().NotContain(s => s.Metrics.ContainsKey("_dd.appsec.rate_limit.dropped_traces"));
            }
        }
    }
}
#endif
