//------------------------------------------------------------------------------
// <auto-generated />
// This file was automatically generated by the UpdateVendors tool.
//------------------------------------------------------------------------------

namespace Datadog.Trace.Vendors.MessagePack.Formatters
{
    // marker
    internal interface IMessagePackFormatter
    {

    }

    internal interface IMessagePackFormatter<T> : IMessagePackFormatter
    {
        int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver);
    }
}
