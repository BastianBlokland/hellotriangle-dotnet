using System;
using System.Runtime.CompilerServices;

using HT.Engine.Math;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering.Memory
{
    internal sealed class StagingBuffer : IDisposable
    {
        private readonly Copier copier;
        private readonly long size;
        private readonly VulkanCore.Buffer buffer;
        private readonly DeviceMemory memory;

        private bool disposed;

        internal StagingBuffer(
            Device logicalDevice,
            HostDevice hostDevice,
            Copier copier,
            long size = 1_048_576)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (hostDevice == null)
                throw new ArgumentNullException(nameof(hostDevice));
            if (copier == null)
                throw new ArgumentNullException(nameof(copier));
            this.copier = copier;
            this.size = size;

            //Create the buffer
            buffer = logicalDevice.CreateBuffer(new BufferCreateInfo(
                size: size,
                usages: BufferUsages.TransferSrc,
                flags: BufferCreateFlags.None,
                sharingMode: SharingMode.Exclusive
            ));
            //Find the requirements for this buffer
            var memRequirements = buffer.GetMemoryRequirements();
            //Find a memory type that can support this buffer and is also host visible so we can
            //write to it from the cpu
            int memTypeIndex = hostDevice.GetMemoryType(
                properties: MemoryProperties.HostVisible | MemoryProperties.HostCoherent,
                supportedTypesFilter: memRequirements.MemoryTypeBits);
            //Allocate the memory
            memory = logicalDevice.AllocateMemory(new MemoryAllocateInfo(
                allocationSize: memRequirements.Size,
                memoryTypeIndex: memTypeIndex));
            //Bind the memory to the buffer
            buffer.BindMemory(memory);
        }

        internal void Upload<T>(T[] data, Image destination, ImageLayout destinationLayout,
            ImageSubresourceLayers subresource,
            Int2 imageExtents)
            where T : struct
        {
            ThrowIfDisposed();

            //Write to our staging buffer
            Write(data);

            //Copy our staging buffer to the destination
            copier.Copy(
                source: buffer,
                destination: destination,
                destinationLayout: destinationLayout,
                subresource: subresource,
                imageExtents: imageExtents);
        }

        internal void Upload<T>(T[] data, DeviceBuffer destination)
            where T : struct
        {
            ThrowIfDisposed();

            //Write to our staging buffer
            int dataSize = Write(data);

            //Copy our staging buffer to the destination
            copier.Copy(
                source: buffer,
                sourceOffset: 0,
                destination: destination.Buffer,
                destinationOffset: 0,
                size: dataSize);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            buffer.Dispose();
            memory.Dispose();
            disposed = true;
        }

        private int Write<T>(T[] data)
            where T : struct
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length == 0)
                throw new ArgumentException(
                    $"[{nameof(Pool)}] Given data-array is empty", nameof(data));

            int dataSize = data.GetSize();
            if (dataSize > size)
                throw new ArgumentException(
                    $"[{nameof(StagingBuffer)}] Data does not fit in given region", nameof(data));

            //Copy the data into the buffer
            unsafe
            {
                //Map the memory to a cpu pointer
                void* stagingPointer = memory.Map(offset: 0, size: dataSize).ToPointer();
                
                //Copy the data over
                void* dataPointer = Unsafe.AsPointer(ref data[0]);
                System.Buffer.MemoryCopy(dataPointer, stagingPointer, dataSize, dataSize);
                
                //Release the cpu memory
                memory.Unmap();
            }
            return dataSize;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(StagingBuffer)}] Allready disposed");
        }
    }
}