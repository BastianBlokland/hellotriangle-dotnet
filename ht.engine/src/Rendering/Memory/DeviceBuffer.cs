using System;
using System.Diagnostics;

using VulkanCore;

namespace HT.Engine.Rendering.Memory
{
    internal sealed class DeviceBuffer : IBuffer, IShaderInput, IDisposable
    {
        //Properties
        public DescriptorType DescriptorType => DescriptorType.UniformBuffer;
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

        internal static DeviceBuffer UploadData<T>(
            ReadOnlyMemory<T> data,
            RenderScene scene,
            BufferUsages usages) where T : struct
        {
            return UploadData<T>(data.Span, scene, usages);
        }

        internal static DeviceBuffer UploadData<T>(
            ReadOnlySpan<T> data,
            RenderScene scene,
            BufferUsages usages) where T : struct
        {
            return UploadData<T>(
                data,
                scene.LogicalDevice,
                scene.MemoryPool,
                usages,
                scene.StagingBuffer,
                scene.Executor);
        }

        internal static DeviceBuffer UploadData<T>(
            ReadOnlySpan<T> data,
            Device logicalDevice,
            Pool memoryPool,
            BufferUsages usages,
            HostBuffer stagingBuffer,
            TransientExecutor executor) where T : struct
        {
            //First write the data to the staging buffer
            int size = stagingBuffer.Write<T>(data, offset: 0);

            //Then create a device buffer with that size
            DeviceBuffer targetBuffer = new DeviceBuffer(logicalDevice, memoryPool, usages, size);

            //Then copy the data from the staging buffer to the devicebuffer
            executor.ExecuteBlocking(commandBuffer =>
            {
                commandBuffer.CmdCopyBuffer(
                    srcBuffer: stagingBuffer.VulkanBuffer,
                    dstBuffer: targetBuffer.VulkanBuffer,
                    new BufferCopy(size: size, srcOffset: 0, dstOffset: 0));
            });
            return targetBuffer;
        }

        public WriteDescriptorSet CreateDescriptorWrite(DescriptorSet set, int binding)
            => new WriteDescriptorSet(
                dstSet: set,
                dstBinding: binding,
                dstArrayElement: 0,
                descriptorCount: 1,
                descriptorType: DescriptorType,
                bufferInfo: new [] { new DescriptorBufferInfo(buffer, offset: 0, range: size) });

        public void Dispose()
        {
            ThrowIfDisposed();

            buffer.Dispose();
            memory.Free();
            disposed = true;
        }

        [Conditional("DEBUG")]
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(DeviceBuffer)}] Allready disposed");
        }
    }
}