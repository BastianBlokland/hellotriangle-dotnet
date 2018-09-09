using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using HT.Engine.Math;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering
{
    internal class SpecializationContainer : IDisposable
    {
        private readonly UnmanagedMemory memory;
        private readonly ResizeArray<SpecializationMapEntry> entries = new ResizeArray<SpecializationMapEntry>();

        private bool disposed;
        private long currentSize = 0;

        public SpecializationContainer(Logger logger = null, long maxByteSize = 128)
            => memory = new UnmanagedMemory(maxByteSize, logger);

        public void Clear()
        {
            ThrowIfDisposed();

            currentSize = 0;
            entries.Clear();
        }

        public void Add<T>(T data) where T : struct
        {
            ThrowIfDisposed();

            int dataSize = Unsafe.SizeOf<T>();
            memory.Write(data, currentSize);

            entries.Add(new SpecializationMapEntry(
                constantId: entries.Count,
                offset: (int)currentSize,
                size: new Size(dataSize)));

            currentSize += dataSize;
        }

        public SpecializationInfo GetInfo() => new SpecializationInfo(
            mapEntries: entries.ToArray(),
            dataSize: new Size(currentSize),
            data: memory.Pointer);

        public void Dispose()
        {
            ThrowIfDisposed();

            memory.Dispose();
            disposed = true;
        }

        [Conditional("DEBUG")]
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(SpecializationContainer)}] Allready disposed");
        }
    }
}