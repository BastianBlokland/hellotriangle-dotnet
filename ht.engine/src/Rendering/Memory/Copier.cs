using System;

using HT.Engine.Math;
using VulkanCore;

namespace HT.Engine.Rendering.Memory
{
    internal sealed class Copier : IDisposable
    {
        private readonly Queue transferQueue;
        private readonly CommandPool commandPool;
        private readonly CommandBuffer copyCommandBuffer;
        private readonly Fence copyFence;

        private bool disposed;

        public Copier(Device logicalDevice, int transferQueueFamilyIndex)
        {
            transferQueue = logicalDevice.GetQueue(
                queueFamilyIndex: transferQueueFamilyIndex,
                queueIndex: 0);

            commandPool = logicalDevice.CreateCommandPool(new CommandPoolCreateInfo(
                queueFamilyIndex: transferQueueFamilyIndex,
                flags: CommandPoolCreateFlags.ResetCommandBuffer));

            copyCommandBuffer = commandPool.AllocateBuffers(new CommandBufferAllocateInfo(
                level: CommandBufferLevel.Primary,
                count: 1))[0];
            
            copyFence = logicalDevice.CreateFence(new FenceCreateInfo(flags: FenceCreateFlags.None));
        }

        public void Copy(
            VulkanCore.Buffer source,
            long sourceOffset,
            VulkanCore.Buffer destination,
            long destinationOffset,
            long size)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            BeginCopyRecord();
            {
                copyCommandBuffer.CmdCopyBuffer(source, destination, new BufferCopy(
                    size, sourceOffset, destinationOffset));
            }
            EndCopyRecord();
        }

        public void Copy(
            VulkanCore.Buffer source,
            Image destination,
            ImageLayout destinationLayout,
            ImageSubresourceLayers subResource,
            Int2 imageExtents)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            BeginCopyRecord();
            {
                BufferImageCopy copyRegion = new BufferImageCopy();
                copyRegion.BufferOffset = 0;
                copyRegion.BufferRowLength = 0;
                copyRegion.BufferImageHeight = 0;
                copyRegion.ImageSubresource = subResource;
                copyRegion.ImageOffset = new Offset3D(x: 0, y: 0, z: 0);
                copyRegion.ImageExtent = new Extent3D(
                    width: imageExtents.X,
                    height: imageExtents.Y,
                    depth: 1);

                copyCommandBuffer.CmdCopyBufferToImage(
                    srcBuffer: source,
                    dstImage: destination,
                    dstImageLayout: destinationLayout,
                    regions: copyRegion);
            }
            EndCopyRecord();
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            copyCommandBuffer.Dispose();
            commandPool.Dispose();
            copyFence.Dispose();
            disposed = true;
        }

        private void BeginCopyRecord()
        {
            ThrowIfDisposed();

            //Reset and record the copy instruction into the commandbuffer
            copyCommandBuffer.Reset(flags: CommandBufferResetFlags.None);

            copyCommandBuffer.Begin(new CommandBufferBeginInfo(
                flags: CommandBufferUsages.OneTimeSubmit));
        }

        private void EndCopyRecord()
        {
            copyCommandBuffer.End();

            //Submit the copying to the transfer-queue
            transferQueue.Submit(
                waitSemaphore: null,
                waitDstStageMask: 0,
                commandBuffer: copyCommandBuffer,
                signalSemaphore: null,
                fence: copyFence);

            //Wait for transfer to be complete
            copyFence.Wait();
            copyFence.Reset();
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(Copier)}] Allready disposed");
        }
    }
}