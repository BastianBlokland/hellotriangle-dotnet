using System;
using System.Runtime.CompilerServices;

using VulkanCore;

namespace HT.Engine.Rendering.Memory
{
    internal sealed class Pool : IDisposable
    {
        public VulkanCore.Buffer Buffer => poolBuffer;

        private readonly Device logicalDevice;
        private readonly HostDevice hostDevice;
        private readonly Copier copier;
        private readonly BufferUsages usages;
        private readonly long maxPoolSize;
        private readonly long maxStagingSize;

        private readonly VulkanCore.Buffer poolBuffer;
        private readonly DeviceMemory poolMemory;
        private readonly VulkanCore.Buffer stagingBuffer;
        private readonly DeviceMemory stagingMemory;

        private long currentOffset;
        private bool disposed;

        internal Pool(
            Device logicalDevice,
            HostDevice hostDevice,
            Copier copier,
            BufferUsages usages,
            long poolSize = 67_108_864,
            long stagingSize = 1_048_576)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (hostDevice == null)
                throw new ArgumentNullException(nameof(hostDevice));
            if (copier == null)
                throw new ArgumentNullException(nameof(copier));
            
            this.logicalDevice = logicalDevice;
            this.hostDevice = hostDevice;
            this.copier = copier;
            this.usages = usages;
            maxPoolSize = poolSize;
            maxStagingSize = stagingSize;
            
            //Create a device-local buffer for use a a pool (need transferDest to copy into it)
            (poolBuffer, poolMemory) = CreateBuffer(
                size: poolSize, 
                usages: usages | BufferUsages.TransferDst,
                memoryProperties: MemoryProperties.DeviceLocal);

            //Create a host-visible staging buffer we can use to transfer to the pool
            (stagingBuffer, stagingMemory) = CreateBuffer(
                size: stagingSize, 
                usages: usages | BufferUsages.TransferSrc,
                memoryProperties: MemoryProperties.HostVisible | MemoryProperties.HostCoherent);
        }

        public Region Allocate<T>(int count)
            where T : struct
        {
            ThrowIfDisposed();

            long size = Unsafe.SizeOf<T>() * count;
            if (size > maxStagingSize)
                throw new Exception(
                    $"[{nameof(Pool)}] Data of size '{size}' is too big for the staging-buffer");
            if ((maxPoolSize - currentOffset) < size)
                throw new Exception($"[{nameof(Pool)}] Not enough space left in the pool");

            //Mark the space as used
            currentOffset += size;

            return new Region(offset: currentOffset - size, size: size);
        }

        public void Write<T>(T[] data, Region region)
            where T : struct
        {
            ThrowIfDisposed();

            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length == 0)
                throw new ArgumentException(
                    $"[{nameof(Pool)}] Given data-array is empty", nameof(data));

            int dataSize = Unsafe.SizeOf<T>() * data.Length;
            if(dataSize > region.Size)
                throw new ArgumentException(
                    $"[{nameof(Pool)}] Data does not fit in given region", nameof(data));

            //Copy the data into the staging buffer
            unsafe
            {
                void* stagingPointer = stagingMemory.Map(offset: 0, size: dataSize).ToPointer();
                void* dataPointer = Unsafe.AsPointer(ref data[0]);
                System.Buffer.MemoryCopy(dataPointer, stagingPointer, dataSize, dataSize);
                stagingMemory.Unmap();
            }

            //Copy the staging buffer to the pool
            copier.Copy(
                source: stagingBuffer, 
                sourceOffset: 0,
                destination: poolBuffer,
                destinationOffset: region.Offset,
                size: dataSize);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            poolBuffer.Dispose();
            poolMemory.Dispose();
            stagingBuffer.Dispose();
            stagingMemory.Dispose();
            disposed = true;
        }

        private (VulkanCore.Buffer, DeviceMemory) CreateBuffer(
            long size,
            BufferUsages usages,
            MemoryProperties memoryProperties)
        {
            var buffer = logicalDevice.CreateBuffer(new BufferCreateInfo(
                size: size,
                usages: usages | BufferUsages.TransferSrc,
                flags: BufferCreateFlags.None,
                sharingMode: SharingMode.Exclusive
            ));
            var memRequirements = buffer.GetMemoryRequirements();
            var deviceMemory = logicalDevice.AllocateMemory(new MemoryAllocateInfo(
                allocationSize: memRequirements.Size,
                memoryTypeIndex: hostDevice.GetMemoryType(
                    supportedTypesBits: memRequirements.MemoryTypeBits,
                    properties: memoryProperties)
            ));
            buffer.BindMemory(deviceMemory, memoryOffset: 0);
            return (buffer, deviceMemory);
        } 

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(Pool)}] Allready disposed");
        }
    }
}