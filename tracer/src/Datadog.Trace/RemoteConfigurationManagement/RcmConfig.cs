// <copyright file="RcmConfig.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;

namespace Datadog.Trace.RemoteConfigurationManagement;

internal class RcmConfig : IEquatable<RcmConfig>
{
    public RcmConfig(string id, string product, byte[] contents, Dictionary<string, string> hashes, int version)
    {
        Id = id;
        Product = product;
        Contents = contents;
        Hashes = hashes;
        Version = version;
    }

    public string Id { get; }

    public string Product { get; }

    public byte[] Contents { get; }

    public Dictionary<string, string> Hashes { get; }

    public int Version { get; }

    public override bool Equals(object o)
    {
        if (ReferenceEquals(null, o))
        {
            return false;
        }

        if (ReferenceEquals(this, o))
        {
            return true;
        }

        if (o.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((RcmConfig)o);
    }

    public bool Equals(RcmConfig other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return
            Version == other.Version &&
            Product.Equals(other.Product) &&
            Id.Equals(other.Id) &&
            ByteArrayCompare(Contents, other.Contents) &&
            DictionaryContentEquals(Hashes, other.Hashes);
    }

    // because https://stackoverflow.com/a/48599119
    private static bool ByteArrayCompare(byte[] a1, byte[] a2)
    {
        return a1.SequenceEqual(a2);
    }

    public static bool DictionaryContentEquals(Dictionary<string, string> dictionary, Dictionary<string, string> otherDictionary)
    {
        return (otherDictionary ?? new Dictionary<string, string>())
            .OrderBy(kvp => kvp.Key)
            .SequenceEqual((dictionary ?? new Dictionary<string, string>())
            .OrderBy(kvp => kvp.Key));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Product, Id, Contents, Hashes, Version);
    }
}
