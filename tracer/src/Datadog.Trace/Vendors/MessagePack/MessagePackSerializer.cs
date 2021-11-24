//------------------------------------------------------------------------------
// <auto-generated />
// This file was automatically generated by the UpdateVendors tool.
//------------------------------------------------------------------------------
using Datadog.Trace.Vendors.MessagePack.Internal;
using System;
using System.IO;

namespace Datadog.Trace.Vendors.MessagePack
{
    /// <summary>
    /// High-Level API of MessagePack for C#.
    /// </summary>
    internal static partial class MessagePackSerializer
    {
        /// <summary>
        /// Serialize to binary with specified resolver.
        /// </summary>
        public static byte[] Serialize<T>(T obj, IFormatterResolver resolver)
        {
            var formatter = resolver.GetFormatterWithVerify<T>();

            var buffer = InternalMemoryPool.GetBuffer();

            var len = formatter.Serialize(ref buffer, 0, obj, resolver);

            // do not return MemoryPool.Buffer.
            return MessagePackBinary.FastCloneWithResize(buffer, len);
        }

        /// <summary>
        /// Serialize to binary with specified resolver. Get the raw memory pool byte[]. The result can not share across thread and can not hold, so use quickly.
        /// </summary>
        public static ArraySegment<byte> SerializeUnsafe<T>(T obj, IFormatterResolver resolver)
        {
            var formatter = resolver.GetFormatterWithVerify<T>();

            var buffer = InternalMemoryPool.GetBuffer();

            var len = formatter.Serialize(ref buffer, 0, obj, resolver);

            // return raw memory pool, unsafe!
            return new ArraySegment<byte>(buffer, 0, len);
        }

        /// <summary>
        /// Serialize to stream with specified resolver.
        /// </summary>
        public static void Serialize<T>(Stream stream, T obj, IFormatterResolver resolver)
        {
            var formatter = resolver.GetFormatterWithVerify<T>();

            var buffer = InternalMemoryPool.GetBuffer();

            var len = formatter.Serialize(ref buffer, 0, obj, resolver);

            // do not need resize.
            stream.Write(buffer, 0, len);
        }

        /// <summary>
        /// Reflect of resolver.GetFormatterWithVerify[T].Serialize.
        /// </summary>
        public static int Serialize<T>(ref byte[] bytes, int offset, T value, IFormatterResolver resolver)
        {
            return resolver.GetFormatterWithVerify<T>().Serialize(ref bytes, offset, value, resolver);
        }

        /// <summary>
        /// Serialize to stream(async) with specified resolver.
        /// </summary>
        public static async System.Threading.Tasks.Task SerializeAsync<T>(Stream stream, T obj, IFormatterResolver resolver)
        {
            var formatter = resolver.GetFormatterWithVerify<T>();
            var rentBuffer = BufferPool.Default.Rent();

            try
            {
                var buffer = rentBuffer;
                var len = formatter.Serialize(ref buffer, 0, obj, resolver);

                // do not need resize.
                await stream.WriteAsync(buffer, 0, len).ConfigureAwait(false);
            }
            finally
            {
                BufferPool.Default.Return(rentBuffer);
            }
        }
    }
}

namespace Datadog.Trace.Vendors.MessagePack.Internal
{
    internal static class InternalMemoryPool
    {
        [ThreadStatic]
        static byte[] buffer = null;

        public static byte[] GetBuffer()
        {
            if (buffer == null)
            {
                buffer = new byte[65536];
            }
            return buffer;
        }
    }
}
