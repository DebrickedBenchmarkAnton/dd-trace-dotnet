// <copyright file="TraceTag.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;

namespace Datadog.Trace.Tagging;

internal readonly struct TraceTag
{
    public readonly string Key;

    public readonly string? Value;

    public readonly TraceTagSerializationMode SerializationMode;

    public TraceTag(string name, string? value, TraceTagSerializationMode serializationMode)
    {
        Key = name;
        Value = value;
        SerializationMode = serializationMode;
    }

    // NOTE: will be used soon for tag propagation
    // public bool IsPropagated => Key.StartsWith(TagPropagation.PropagatedTagPrefix, StringComparison.Ordinal);
}