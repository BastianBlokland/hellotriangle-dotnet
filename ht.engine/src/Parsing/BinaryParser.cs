using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HT.Engine.Utils;

namespace HT.Engine.Parsing
{
    /// <summary>
    /// Base class for a single pass binary parser
    /// </summary>
    public abstract class BinaryParser<T> : IParser<T>, IDisposable
    {
        //Helper properties
        protected bool IsEndOfFile => inputReader.PeekChar() < 0;

        //Data
        private readonly BinaryReader inputReader;

        private byte[] readBuffer;
        private T result;
        private bool parsed;

        public BinaryParser(Stream inputStream) => inputReader = new BinaryReader(inputStream);

        public T Parse()
        {
            if (!parsed)
            {
                bool keepParsing = true;
                while (keepParsing && !IsEndOfFile)
                    keepParsing = ConsumeToken();
                result = Construct();
                parsed = true;
            }
            return result;
        }

        public void Dispose() => inputReader.Dispose();

        /// <summary>
        /// Parse a single token
        /// </summary>
        /// <returns>bool to indicate if you want to keep parsing</returns>
        protected abstract bool ConsumeToken();

        /// <summary>
        /// Construct the type from the consumed tokens
        /// </summary>
        protected abstract T Construct();

        /// <summary>
        /// Reads struct from the input-reader
        /// NOTE: Make sure your struct has a sequential layout without padding
        /// </summary>
        protected unsafe ST Consume<ST>()
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

        protected void ConsumeIgnore(int bytes)
            => inputReader.Read(readBuffer, index: 0, count: bytes);

        protected Exception CreateError(string errorMessage)
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