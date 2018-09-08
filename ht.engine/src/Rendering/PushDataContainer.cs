using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using HT.Engine.Math;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering
{
    internal class PushDataContainer : IDisposable
    {
        private readonly ShaderStages stages;
        private readonly UnmanagedMemory memory;
        private readonly ResizeArray<PushConstantRange> entries = new ResizeArray<PushConstantRange>();

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

        public PushConstantRange? GetRange(int binding)
        {
            ThrowIfDisposed();

            if (binding < 0 || binding >= entries.Count)
                return null;
            return entries.Data[binding];
        }

        public int Add<T>() where T : struct
        {
            ThrowIfDisposed();

            int size = Unsafe.SizeOf<T>();
            int binding = entries.Count;

            entries.Add(new PushConstantRange(
                stageFlags: stages,
                offset: (int)currentSize,
                size: size));

            currentSize += size;
            return binding;
        }

        public void Set<T>(int binding, T data) where T : struct
        {
            ThrowIfDisposed();

            PushConstantRange? range = GetRange(binding);
            if (range == null)
                throw new Exception(
                    $"[{nameof(PushDataContainer)}] Binding '{binding}' unknown");
            
            #if DEBUG
            int size = Unsafe.SizeOf<T>();
            if (size != range.Value.Size)
                throw new Exception(
                    $"[{nameof(PushDataContainer)}] Binding '{binding}' has different size then provided data");
            #endif

            memory.Write(data, range.Value.Offset);
        }

        public PushConstantRange[] GetRanges() => entries.ToArray();

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

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(PushDataContainer)}] Allready disposed");
        }
    }
}