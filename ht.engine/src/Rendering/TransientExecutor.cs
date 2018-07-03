using System;

using HT.Engine.Math;
using VulkanCore;

namespace HT.Engine.Rendering
{
    internal sealed class TransientExecutor : IDisposable
    {
        //Data
        private readonly Queue queue;
        private readonly CommandPool commandPool;
        private readonly CommandBuffer transientBuffer;
        private readonly Fence fence;
        private bool disposed;

        internal TransientExecutor(Device logicalDevice, int queueFamilyIndex)
        {
            queue = logicalDevice.GetQueue(
                queueFamilyIndex: queueFamilyIndex,
                queueIndex: 0);

            commandPool = logicalDevice.CreateCommandPool(new CommandPoolCreateInfo(
                queueFamilyIndex: queueFamilyIndex,
                flags: CommandPoolCreateFlags.ResetCommandBuffer));

            transientBuffer = commandPool.AllocateBuffers(new CommandBufferAllocateInfo(
                level: CommandBufferLevel.Primary,
                count: 1))[0];
            
            fence = logicalDevice.CreateFence(new FenceCreateInfo(flags: FenceCreateFlags.None));
        }

        internal void ExecuteBlocking(Action<CommandBuffer> record)
        {
            if (record == null)
                throw new NullReferenceException(nameof(record));
            ThrowIfDisposed();

            //Reset and record the copy instruction into the commandbuffer
            transientBuffer.Reset(flags: CommandBufferResetFlags.None);

            //Begin the buffer
            transientBuffer.Begin(new CommandBufferBeginInfo(
                flags: CommandBufferUsages.OneTimeSubmit));

            record(transientBuffer);

            //End the buffer
            transientBuffer.End();

            //Submit the copying to the queue
            queue.Submit(
                waitSemaphore: null,
                waitDstStageMask: 0,
                commandBuffer: transientBuffer,
                signalSemaphore: null,
                fence: fence);

            //Wait for it to execute
            fence.Wait();
            fence.Reset();
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            transientBuffer.Dispose();
            commandPool.Dispose();
            fence.Dispose();
            disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(TransientExecutor)}] Allready disposed");
        }
    }
}