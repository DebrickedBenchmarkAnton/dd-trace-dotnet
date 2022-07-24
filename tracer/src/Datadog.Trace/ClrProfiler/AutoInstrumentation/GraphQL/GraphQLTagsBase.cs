// <copyright file="GraphQLTagsBase.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Datadog.Trace.SourceGenerators;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.GraphQL
{
    internal abstract partial class GraphQLTagsBase : InstrumentationTags
    {
        [Tag(Trace.Tags.SpanKind)]
        public override string SpanKind => SpanKinds.Server;

        [Tag(Trace.Tags.InstrumentationName)]
        public abstract string InstrumentationName { get; }

        [Tag(Trace.Tags.GraphQLSource)]
        public string Source { get; set; }

        [Tag(Trace.Tags.GraphQLOperationName)]
        public string OperationName { get; set; }

        [Tag(Trace.Tags.GraphQLOperationType)]
        public string OperationType { get; set; }
    }
}
