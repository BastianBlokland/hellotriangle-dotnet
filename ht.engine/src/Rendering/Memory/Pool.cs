using System;

using HT.Engine.Math;
using VulkanCore;

namespace HT.Engine.Rendering.Memory
{
    internal sealed class Pool : IDisposable
    {
        private readonly long poolSize;
        private readonly int memoryTypeIndex;
        private readonly DeviceMemory memory;

        private long currentOffset;
        private bool disposed;

        internal Pool(Device logicalDevice, HostDevice hostDevice, long size = 67_108_864)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (hostDevice == null)
                throw new ArgumentNullException(nameof(hostDevice));
            this.poolSize = size;

            //Find the memory type on the gpu to place this pool in
            memoryTypeIndex = hostDevice.GetMemoryType(MemoryProperties.DeviceLocal);
            //Allocate the memory
            memory = logicalDevice.AllocateMemory(new MemoryAllocateInfo(
                allocationSize: size,
                memoryTypeIndex: memoryTypeIndex));
        }

        public void AllocateAndBind(VulkanCore.Buffer buffer)
        {
            ThrowIfDisposed();

            var memRequirements = buffer.GetMemoryRequirements();
            var memRegion = Allocate(memRequirements);
            buffer.BindMemory(memory, memRegion.Offset);
        }

        public Region Allocate(MemoryRequirements requirements)
        {
            ThrowIfDisposed();

            //Verify that the memory backing this pool is supported by the requirement
            if (!requirements.MemoryTypeBits.HasBitSet(memoryTypeIndex))
                throw new Exception(
                    $"[{nameof(Pool)}] Given requirements does not match the memory backing this pool");

            //Calculate padding to satisfy the alignment
            var padding = GetPadding(currentOffset, requirements.Alignment);
            var paddedSize = requirements.Size + padding;

            if ((poolSize - currentOffset) < paddedSize)
                throw new Exception($"[{nameof(Pool)}] Not enough space left in the pool");

            Region region = new Region(
                offset: currentOffset + padding,
                size: requirements.Size);

            //Mark the space as used
            currentOffset += paddedSize;

            return region;
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            memory.Dispose();
            disposed = true;
        }

        private long GetPadding(long offset, long alignment)
        {
            long remainder = offset % alignment;
            if (remainder == 0)
                return 0;
            return alignment - remainder;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(Pool)}] Allready disposed");
        }
    }
}