using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
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

        public BinaryParser(Stream inputStream, bool leaveStreamOpen) => 
            inputReader = new BinaryReader(inputStream, Encoding.ASCII, leaveOpen: leaveStreamOpen);

        /// <summary>
        /// Reads struct from the input-reader
        /// NOTE: Make sure your struct has a sequential layout without padding
        /// </summary>
        public unsafe ST Consume<ST>()
            where ST : struct
        {
            int size = Unsafe.SizeOf<ST>();
            Span<byte> data = stackalloc byte[size];
            inputReader.Read(data);
            return MemoryMarshal.Read<ST>(data);
        }

        public void Consume(Span<byte> data) => inputReader.Read(data);

        public byte Consume() => inputReader.ReadByte();

        public Int16 ConsumeInt16() => inputReader.ReadInt16();

        public Int32 ConsumeInt32() => inputReader.ReadInt32();

        public UInt16 ConsumeUInt16() => inputReader.ReadUInt16();

        public UInt32 ConsumeUInt32() => inputReader.ReadUInt32();

        public float ConsumeFloat() => inputReader.ReadSingle();

        public void ConsumeIgnore(int bytes)
        {
            //Depending on usage this might be a bad implementation, as it could create
            //a very big stack array, but the advantage is that we don't need to read byte for byte
            //the alternative would be just call 'Read' in a for loop
            Span<byte> data = stackalloc byte[bytes];
            inputReader.Read(data);
        }

        public Exception CreateError(string errorMessage)
            => throw new Exception($"[{GetType().Name}] {errorMessage}");

        public void Dispose() => inputReader.Dispose();
    }
}