using System;
using System.Collections.Generic;

using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering.Memory
{
    internal sealed class Pool : IDisposable
    {
        private readonly Device logicalDevice;
        private readonly HostDevice hostDevice;
        private readonly List<Chunk> chunks = new List<Chunk>();
        private bool disposed;

        internal Pool(Device logicalDevice, HostDevice hostDevice)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (hostDevice == null)
                throw new ArgumentNullException(nameof(hostDevice));
            this.logicalDevice = logicalDevice;
            this.hostDevice = hostDevice;
        }

        internal Block AllocateAndBind(VulkanCore.Image image)
        {
            ThrowIfDisposed();

            var memRequirements = image.GetMemoryRequirements();
            Block block = Allocate(memRequirements);
            image.BindMemory(block.Container.Memory, block.Offset);
            return block;
        }

        internal Block AllocateAndBind(VulkanCore.Buffer buffer)
        {
            ThrowIfDisposed();

            var memRequirements = buffer.GetMemoryRequirements();
            Block block = Allocate(memRequirements);
            buffer.BindMemory(block.Container.Memory, block.Offset);
            return block;
        }

        internal Block Allocate(MemoryRequirements requirements)
        {
            //Allocate in the first chunk that supports this requirement
            for (int i = 0; i < chunks.Count; i++)
            {
                if (chunks[i].IsSupported(requirements))
                {
                    Block? block = chunks[i].TryAllocate(requirements);
                    if (block != null)
                        return block.Value;
                }
            }

            //If non supported the requirements then create a new chunk
            Chunk newChunk = new Chunk(logicalDevice, hostDevice, requirements.MemoryTypeBits);
            chunks.Add(newChunk);
    
            //Allocate from the new chunk
            Block? newBlock = newChunk.TryAllocate(requirements);
            if (newBlock == null)
                throw new Exception(
                    $"[{nameof(Pool)}] New chunk could not allocate this requirements, is it insanely big?");
            return newBlock.Value;
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            chunks.DisposeAll();
            disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(Pool)}] Allready disposed");
        }
    }
}