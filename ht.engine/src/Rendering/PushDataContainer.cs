using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using HT.Engine.Math;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering
{
    internal class PushDataContainer : IDisposable
    {
        private readonly struct Entry
        {
            public readonly long Offset;
            public readonly long Size;

            public Entry(long offset, long size)
            {
                Offset = offset;
                Size = size;
            }
        }

        private readonly ShaderStages stages;
        private readonly UnmanagedMemory memory;
        private readonly ResizeArray<Entry> entries = new ResizeArray<Entry>();

        private bool disposed;
        private long currentSize = 0;

        public PushDataContainer(ShaderStages stages, Logger logger = null, long maxByteSize = 128)
        {
            this.stages = stages;
            memory = new UnmanagedMemory(maxByteSize, logger);
        }

        public void Clear()
        {
            ThrowIfDisposed();

            currentSize = 0;
            entries.Clear();
        }

        public int Add<T>() where T : struct
        {
            ThrowIfDisposed();

            int size = Unsafe.SizeOf<T>();
            int binding = entries.Count;

            entries.Add(new Entry(currentSize, size));
            currentSize += size;
            return binding;
        }

        public void Set<T>(int binding, T data) where T : struct
        {
            ThrowIfDisposed();

            Entry? entry = GetEntry(binding);
            if (entry == null)
                throw new Exception(
                    $"[{nameof(PushDataContainer)}] Binding '{binding}' unknown");
            
            #if DEBUG
            int size = Unsafe.SizeOf<T>();
            if (size != entry.Value.Size)
                throw new Exception(
                    $"[{nameof(PushDataContainer)}] Binding '{binding}' has different size then provided data");
            #endif

            memory.Write(data, entry.Value.Offset);
        }

        public PushConstantRange[] GetRanges() =>
            entries.Count == 0 ?
                null :
                new [] { new PushConstantRange(stages, offset: 0, size: (int)currentSize) };

        public void Push(CommandBuffer commandBuffer, PipelineLayout pipelineLayout)
        {
            //If there is no data added then there is no need to push
            if (entries.Count == 0)
                return;

            commandBuffer.CmdPushConstants(
                pipelineLayout,
                stages,
                offset: 0,
                size: (int)currentSize,
                values: memory.Pointer);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            memory.Dispose();
            disposed = true;
        }

        private Entry? GetEntry(int binding)
        {
            if (binding < 0 || binding >= entries.Count)
                return null;
            return entries.Data[binding];
        }

        [Conditional("DEBUG")]
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(PushDataContainer)}] Allready disposed");
        }
    }
}