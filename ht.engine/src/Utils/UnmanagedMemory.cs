using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.Engine.Utils
{
    /// <summary>
    /// Wraps a region of unmanaged-memory in a safe-ish way. Can be usefull if you want to store
    /// a 'array' of elements of different types, or a 'dynamic' struct or something similar
    /// </summary>
    public unsafe class UnmanagedMemory : IDisposable
    {
        //Properties
        public IntPtr Pointer => unmanagedMemory;
        public long MaxSize => size;

        //Data
        private readonly Logger logger;
        private readonly int size;
        private readonly IntPtr unmanagedMemory;

        private bool disposed;

        public UnmanagedMemory(long size = 128, Logger logger = null)
        {
            //Note: Using long in the constructor to make is consistent with the rest of the api
            if (size > Int32.MaxValue)
                throw new Exception(
                    $"[{nameof(UnmanagedMemory)}] More then {Int32.MaxValue} bytes of unmanaged memory is unsupported");

            this.logger = logger;

            logger?.Log(nameof(UnmanagedMemory), $"Allocating {size} bytes of unmanaged memory");

            this.size = (int)size;
            unmanagedMemory = Marshal.AllocHGlobal(this.size);
        }

        public void Write<T>(T data, long offset) where T : struct
        {
            ThrowIfDisposed();

            int dataSize = Unsafe.SizeOf<T>();

            if (offset + dataSize > size)
                throw new ArgumentException(
                    $"[{nameof(UnmanagedMemory)}] Data does not fit in memory", nameof(data));

            void* dataPointer = Unsafe.AsPointer(ref data);
            System.Buffer.MemoryCopy(
                source: dataPointer,
                destination: (byte*)unmanagedMemory.ToPointer() + offset,
                destinationSizeInBytes: dataSize,
                sourceBytesToCopy: dataSize);
        }

        public T Read<T>(long offset) where T : struct
        {
            ThrowIfDisposed();

            int dataSize = Unsafe.SizeOf<T>();
            ReadOnlySpan<byte> region = GetRegion(offset, dataSize);
            return MemoryMarshal.Read<T>(region);
        }

        //Conversions
        public Span<byte> GetSpan()
        {
            ThrowIfDisposed();
            return new Span<byte>(Pointer.ToPointer(), size);
        }

        public Span<byte> GetRegion(long offset, long size)
        {
            ThrowIfDisposed();
            if (offset + size > this.size)
                throw new Exception($"[{nameof(UnmanagedMemory)}] Out of memory bounds");
            return new Span<byte>(
                pointer: (byte*)Pointer.ToPointer() + offset,
                length: (int)size);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            logger?.Log(nameof(UnmanagedMemory), $"Freeing {size} bytes of unmanaged memory");

            Marshal.FreeHGlobal(unmanagedMemory);
            disposed = true;
        }

        [Conditional("DEBUG")]
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(UnmanagedMemory)}] Allready disposed");
        }
    }
}