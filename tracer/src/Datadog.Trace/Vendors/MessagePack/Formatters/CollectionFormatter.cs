//------------------------------------------------------------------------------
// <auto-generated />
// This file was automatically generated by the UpdateVendors tool.
//------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Datadog.Trace.Vendors.MessagePack.Formatters
{
    internal sealed class ArraySegmentFormatter<T> : IMessagePackFormatter<ArraySegment<T>>
    {
        public int Serialize(ref byte[] bytes, int offset, ArraySegment<T> value, IFormatterResolver formatterResolver)
        {
            if (value.Array == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                var formatter = formatterResolver.GetFormatterWithVerify<T>();

                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Count);

                var array = value.Array;
                for (int i = 0; i < value.Count; i++)
                {
                    var item = array[value.Offset + i];
                    offset += formatter.Serialize(ref bytes, offset, item, formatterResolver);
                }

                return offset - startOffset;
            }
        }
    }
}
