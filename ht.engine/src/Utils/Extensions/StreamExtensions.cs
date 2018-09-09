using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HT.Engine.Utils
{
    public static class StreamExtensions
    {
        public static void ReadToEnd<T>(this Stream stream, Span<T> array)
            where T : struct
        {
            if (!stream.CanRead)
                throw new IOException(
                    $"[{nameof(StreamExtensions)}] Given stream does not support reading!");
            
            Span<byte> byteArray = MemoryMarshal.AsBytes<T>(array);
            if (byteArray.Length != stream.Length)
                throw new ArgumentException(
                    $"[{nameof(StreamExtensions)}] Array size does not match stream length", nameof(array));
            
            int bytesRead = stream.Read(byteArray);
            if (bytesRead != byteArray.Length)
                throw new IOException(
                    $"[{nameof(StreamExtensions)}] Could not read to end of the stream");
        }
    }
}