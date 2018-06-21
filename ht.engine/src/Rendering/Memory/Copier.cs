using System;

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
            ThrowIfDisposed();

            //Reset and record the copy instruction into the commandbuffer
            copyCommandBuffer.Reset(flags: CommandBufferResetFlags.None);

            copyCommandBuffer.Begin(new CommandBufferBeginInfo(
                flags: CommandBufferUsages.OneTimeSubmit));

            copyCommandBuffer.CmdCopyBuffer(source, destination, new BufferCopy(
                size, sourceOffset, destinationOffset));

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
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            copyCommandBuffer.Dispose();
            commandPool.Dispose();
            copyFence.Dispose();
            disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(Pool)}] Allready disposed");
        }
    }
}