using System;

using VulkanCore;

namespace HT.Engine.Rendering.Memory
{
    internal sealed class DeviceBuffer : IDisposable
    {
        //Helper properties
        public VulkanCore.Buffer Buffer => buffer;
        public long Size => size;

        //Data
        private long size;
        private readonly VulkanCore.Buffer buffer;
        private bool disposed;

        public DeviceBuffer(
            Device logicalDevice,
            PoolGroup memoryGroup,
            long size,
            BufferUsages usages)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (memoryGroup == null)
                throw new ArgumentNullException(nameof(memoryGroup));
            
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
            memoryGroup.AllocateAndBind(buffer);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            buffer.Dispose();
            disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(DeviceBuffer)}] Allready disposed");
        }
    }
}