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
            ImageSubresourceLayers subresource,
            Int2 imageExtents)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            BeginCopyRecord();
            {
                //Transition the image into the transfer-destination layout
                RecordImageLayoutBarrier(
                    image: destination,
                    subresource: subresource,
                    oldLayout: ImageLayout.Undefined,
                    newLayout: ImageLayout.TransferDstOptimal);
                
                //Copy the image
                copyCommandBuffer.CmdCopyBufferToImage(
                    srcBuffer: source,
                    dstImage: destination,
                    dstImageLayout: ImageLayout.TransferDstOptimal,
                    regions: new BufferImageCopy {
                        BufferOffset = 0,
                        BufferRowLength = 0,
                        BufferImageHeight = 0,
                        ImageSubresource = subresource,
                        ImageOffset = new Offset3D(x: 0, y: 0, z: 0),
                        ImageExtent = new Extent3D(
                            width: imageExtents.X,
                            height: imageExtents.Y,
                            depth: 1)});
                
                //Transition the image to the target layout
                RecordImageLayoutBarrier(
                    image: destination,
                    subresource: subresource,
                    oldLayout: ImageLayout.TransferDstOptimal,
                    newLayout: destinationLayout);
            }
            EndCopyRecord();
        }

        public void TransitionImageLayout(
            Image image, 
            ImageSubresourceLayers subresource,
            ImageLayout oldLayout,
            ImageLayout newLayout)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            BeginCopyRecord();
            {
                RecordImageLayoutBarrier(image, subresource, oldLayout, newLayout);
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

        public void RecordImageLayoutBarrier(
            Image image, 
            ImageSubresourceLayers subresource,
            ImageLayout oldLayout,
            ImageLayout newLayout)
        {
            //Get where this transition has to wait and what has to wait for this transition
            Accesses sourceAccess, destinationAccess;
            PipelineStages sourcePipelineStages, destinationPipelineStages;
            if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
            {
                sourceAccess = Accesses.None;
                destinationAccess = Accesses.TransferWrite;
                sourcePipelineStages = PipelineStages.TopOfPipe;
                destinationPipelineStages = PipelineStages.Transfer;
            }
            else
            if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.DepthStencilAttachmentOptimal)
            {
                sourceAccess = Accesses.None;
                destinationAccess = Accesses.DepthStencilAttachmentRead | Accesses.DepthStencilAttachmentWrite;
                sourcePipelineStages = PipelineStages.TopOfPipe;
                destinationPipelineStages = PipelineStages.EarlyFragmentTests;
            }
            else
            if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
            {
                sourceAccess = Accesses.TransferWrite;
                destinationAccess = Accesses.ShaderRead;
                sourcePipelineStages = PipelineStages.Transfer;
                destinationPipelineStages = PipelineStages.FragmentShader;
            }
            else
                throw new Exception(
                    $"[{nameof(Copier)}] Unsupported image transition: from: {oldLayout} to: {newLayout}");
            
            //Create the transition barrier
            var imageMemoryBarrier = new ImageMemoryBarrier(
                image: image,
                subresourceRange: new ImageSubresourceRange(
                    aspectMask: subresource.AspectMask,
                    baseMipLevel: subresource.MipLevel,
                    levelCount: 1,
                    baseArrayLayer: subresource.BaseArrayLayer,
                    layerCount: subresource.LayerCount),
                srcAccessMask: sourceAccess,
                dstAccessMask: destinationAccess,
                oldLayout: oldLayout,
                newLayout: newLayout);
            //Record the transition barrier
            copyCommandBuffer.CmdPipelineBarrier(
                srcStageMask: sourcePipelineStages,
                dstStageMask: destinationPipelineStages,
                dependencyFlags: Dependencies.None,
                memoryBarriers: null,
                bufferMemoryBarriers: null,
                imageMemoryBarriers: new [] { imageMemoryBarrier });
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(Copier)}] Allready disposed");
        }
    }
}