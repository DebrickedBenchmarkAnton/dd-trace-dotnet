//------------------------------------------------------------------------------
// <auto-generated />
// This file was automatically generated by the UpdateVendors tool.
//------------------------------------------------------------------------------
#pragma warning disable CS0618, CS0649, CS1574, CS1580, CS1581, CS1584, CS1591, CS1573, CS8018, SYSLIB0011, SYSLIB0032
#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS8604, CS8618, CS8620, CS8714, CS8762, CS8765, CS8766, CS8767, CS8768, CS8769, CS8612, CS8629, CS8774
#nullable enable
#if NETCOREAPP3_1_OR_GREATER
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Serialization;

namespace Datadog.Trace.Vendors.IndieSystem.Text.RegularExpressions
{
    /// <summary>
    /// An exception as a result of a parse error in a regular expression <see cref="RegularExpressions"/>, with
    /// detailed information in the <see cref="Error"/> and <see cref="Offset"/> properties.
    /// </summary>
    [Serializable]
    internal sealed class RegexParseException : ArgumentException
    {
        /// <summary>Gets the error that happened during parsing.</summary>
        public RegexParseError Error { get; }

        /// <summary>Gets the zero-based character offset in the regular expression pattern where the parse error occurs.</summary>
        public int Offset { get; }

        internal RegexParseException(RegexParseError error, int offset, string message) : base(message)
        {
            Error = error;
            Offset = offset;
        }

        private RegexParseException(SerializationInfo info, StreamingContext context)
        {
            // It means someone modified the payload.
            throw new NotImplementedException();
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.SetType(typeof(ArgumentException)); // To maintain serialization support with .NET Framework.
        }
    }
}

#endif