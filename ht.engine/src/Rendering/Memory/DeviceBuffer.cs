using System;

using VulkanCore;

namespace HT.Engine.Rendering.Memory
{
    internal sealed class DeviceBuffer : IBuffer, IDisposable
    {
        //Properties
        public VulkanCore.Buffer VulkanBuffer => buffer;
        public long Size => size;

        //Data
        private long size;
        private readonly VulkanCore.Buffer buffer;
        private readonly Block memory;
        private bool disposed;

        internal DeviceBuffer(
            Device logicalDevice,
            Pool memoryPool,
            BufferUsages usages,
            long size)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (memoryPool == null)
                throw new ArgumentNullException(nameof(memoryPool));
            
            this.size = size;

            //Create the buffer
            buffer = logicalDevice.CreateBuffer(new BufferCreateInfo(
                size: size,
                //Adding 'TransferDst' otherwise we can never copy data to it
                usages: usages | BufferUsages.TransferDst,
                flags: BufferCreateFlags.None,
                sharingMode: SharingMode.Exclusive
            ));
            //Bind memory from our pool to this buffer
            memory = memoryPool.AllocateAndBind(buffer, Chunk.Location.Device);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            buffer.Dispose();
            memory.Free();
            disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(DeviceBuffer)}] Allready disposed");
        }
    }
}