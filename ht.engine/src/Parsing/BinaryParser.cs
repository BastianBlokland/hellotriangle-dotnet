using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HT.Engine.Utils;

namespace HT.Engine.Parsing
{
    /// <summary>
    /// Utility for parsing a binary stream
    /// </summary>
    public sealed class BinaryParser : IDisposable
    {
        //Properties
        public bool IsEndOfFile => inputReader.PeekChar() < 0;

        //Data
        private readonly BinaryReader inputReader;
        private byte[] readBuffer;

        public BinaryParser(Stream inputStream) => inputReader = new BinaryReader(inputStream);

        public void Dispose() => inputReader.Dispose();

        /// <summary>
        /// Reads struct from the input-reader
        /// NOTE: Make sure your struct has a sequential layout without padding
        /// </summary>
        public unsafe ST Consume<ST>()
            where ST : struct
        {
            int size = Unsafe.SizeOf<ST>();

            //Read the required amount of bytes
            EnsureReadBuffer(size);
            inputReader.Read(readBuffer, index: 0, count: size);

            return readBuffer.Parse<ST>();
        }

        public byte Consume() => inputReader.ReadByte();

        public Int16 ConsumeInt16() => inputReader.ReadInt16();

        public Int32 ConsumeInt32() => inputReader.ReadInt32();

        public UInt16 ConsumeUInt16() => inputReader.ReadUInt16();

        public UInt32 ConsumeUInt32() => inputReader.ReadUInt32();

        public float ConsumeFloat() => inputReader.ReadSingle();

        public void ConsumeIgnore(int bytes)
            => inputReader.Read(readBuffer, index: 0, count: bytes);

        public Exception CreateError(string errorMessage)
            => throw new Exception($"[{GetType().Name}] {errorMessage}");

        private void EnsureReadBuffer(int requiredSize)
        {
            if (readBuffer == null)
                readBuffer = new byte[requiredSize];
            else
            if (readBuffer.Length < requiredSize)
            {
                var doubleSize = readBuffer.Length * 2;
                Array.Resize(ref readBuffer, System.Math.Max(doubleSize, requiredSize));
            }
        }
    }
}