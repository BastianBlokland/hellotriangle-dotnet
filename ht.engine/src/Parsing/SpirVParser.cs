using System;
using System.IO;
using System.Runtime.InteropServices;

using HT.Engine.Math;
using HT.Engine.Resources;

namespace HT.Engine.Parsing
{
    //Simplest parser ever just reads the raw Spir-V byte-code and load it into a 'ShaderProgram'
    public sealed class SpirVParser : IParser<ShaderProgram>, IParser
    {
        private readonly Stream inputStream;
        private readonly bool leaveStreamOpen;

        public SpirVParser(Stream inputStream, bool leaveStreamOpen = false)
        {
            if (inputStream == null)
                throw new ArgumentNullException(nameof(inputStream));
            this.inputStream = inputStream;
            this.leaveStreamOpen = leaveStreamOpen;
        }

        public ShaderProgram Parse()
        {
            byte[] data = new byte[inputStream.Length];
            if (inputStream.Read(data, 0, data.Length) != data.Length)
                throw new IOException(
                    $"[{nameof(SpirVParser)}] Could not read to end from stream");
            return new ShaderProgram(data);
        }

        public void Dispose()
        {
            if (!leaveStreamOpen)
                inputStream.Dispose();
        }

        object IParser.Parse() => Parse();
    }
}