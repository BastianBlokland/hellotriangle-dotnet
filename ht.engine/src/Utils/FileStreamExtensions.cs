using System;
using System.IO;

namespace HT.Engine.Utils
{
    public static class FileStreamExtensions
    {
        public static byte[] ReadToEnd(this FileStream stream)
        {
            if(!stream.CanRead)
                throw new IOException($"[{nameof(FileStreamExtensions)}] Stream '{stream.Name}' does not support reading!");
            byte[] data = new byte[stream.Length];
            if(stream.Read(data, 0, data.Length) != data.Length)
                throw new IOException($"[{nameof(FileStreamExtensions)}] Could not read to end from stream '{stream.Name}'");
            return data;
        }
    }
}