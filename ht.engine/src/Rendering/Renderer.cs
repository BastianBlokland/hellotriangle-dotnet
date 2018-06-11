using System;

using HT.Engine.Math;
using VulkanCore;
using VulkanCore.Khr;

namespace HT.Engine.Rendering
{
    public sealed class Renderer : IDisposable
    {
        private int PREFERRED_SWAPCHAIN_IMAGE_COUNT = 2;

        public GraphicsDevice Device => device;
        public Int2 SurfaceSize => surfaceSize;

        private readonly GraphicsDevice device;
        private readonly Surface surface;
        private Int2 surfaceSize;
        private SurfaceCapabilitiesKhr capabilities;
        private SwapchainKhr swapchain;
        private Image[] swapchainImages;
        private CommandBuffer[] commandBuffers;
        private Fence[] submitFences;
        private Semaphore imageAcquiredSemaphore; 
        private Semaphore renderingFinishedSemaphore;
        private bool disposed;

        public Renderer(GraphicsDevice device, Surface surface, Int2 surfaceSize)
        {
            //Initialize the device (if it wasn't allready)
            device.Initialize();

            this.device = device;
            this.surface = surface;

            SetupSwapchain(surfaceSize);
        }

        public void SetupSwapchain(Int2 surfaceSize)
        {
            ThrowIfDisposed();
            this.surfaceSize = surfaceSize;

            device.GraphicsQueue.WaitIdle();
            device.CommandPool.Reset(CommandPoolResetFlags.None);

            DisposeSwapchain();

            //Get capabilities of this surface / device combo
            capabilities = device.VulkanPhysicalDevice.GetSurfaceCapabilitiesKhr(surface.KhrSurface);

            int imageCount = PREFERRED_SWAPCHAIN_IMAGE_COUNT.Clamp(capabilities.MinImageCount, capabilities.MaxImageCount);
            var surfaceFormat = surface.GetFormat(device);
            var presentMode = surface.GetPresentMode(device);

            SwapchainCreateInfoKhr info = new SwapchainCreateInfoKhr
            (
                surface.KhrSurface,
                surfaceFormat.imageFormat,
                new Extent2D(surfaceSize.X, surfaceSize.Y),
                capabilities.CurrentTransform,
                presentMode,
                SwapchainCreateFlagsKhr.None,
                imageCount,
                ImageUsages.ColorAttachment
            );
            swapchain = device.VulkanDevice.CreateSwapchainKhr(info);

            swapchainImages = swapchain.GetImages();
            commandBuffers = device.VulkanCommandPool.AllocateBuffers(new CommandBufferAllocateInfo(CommandBufferLevel.Primary, imageCount));

            submitFences = new Fence[imageCount];
            for (int i = 0; i < submitFences.Length; i++)
                submitFences[i] = device.VulkanDevice.CreateFence(new FenceCreateInfo(FenceCreateFlags.Signaled));

            imageAcquiredSemaphore = device.VulkanDevice.CreateSemaphore();
            renderingFinishedSemaphore = device.VulkanDevice.CreateSemaphore();
            
            for (int i = 0; i < commandBuffers.Length; i++)
            {
                commandBuffers[i].Begin(new CommandBufferBeginInfo(CommandBufferUsages.None));

                var imageSubresourceRange = new ImageSubresourceRange(ImageAspects.Color, baseMipLevel: 0, levelCount: 1, baseArrayLayer: 0, layerCount: 1);
                
                //Load the image
                commandBuffers[i].CmdPipelineBarrier
                (
                    srcStageMask: PipelineStages.Transfer, 
                    dstStageMask: PipelineStages.Transfer, 
                    imageMemoryBarriers: new [] 
                    { 
                        new ImageMemoryBarrier
                        (
                            swapchainImages[i],
                            imageSubresourceRange,
                            srcAccessMask: Accesses.None,
                            dstAccessMask: Accesses.TransferWrite,
                            oldLayout: ImageLayout.Undefined,
                            newLayout: ImageLayout.TransferDstOptimal
                        ) 
                    }
                );

                //Clear the image
                commandBuffers[i].CmdClearColorImage
                (
                    image: swapchainImages[i],
                    imageLayout: ImageLayout.TransferDstOptimal,
                    color: new ClearColorValue(new ColorF4(0f, 1f, 0f, 1f)),
                    ranges: imageSubresourceRange
                );

                //Convert to present mode
                commandBuffers[i].CmdPipelineBarrier
                (
                    srcStageMask: PipelineStages.Transfer, 
                    dstStageMask: PipelineStages.Transfer, 
                    imageMemoryBarriers: new [] 
                    { 
                        new ImageMemoryBarrier
                        (
                            swapchainImages[i],
                            imageSubresourceRange,
                            srcAccessMask: Accesses.TransferWrite,
                            dstAccessMask: Accesses.MemoryRead,
                            oldLayout: ImageLayout.TransferDstOptimal,
                            newLayout: ImageLayout.PresentSrcKhr
                        ) 
                    }
                );

                commandBuffers[i].End();
            }
        }

        public void Draw()
        {
            ThrowIfDisposed();

            int imageIndex = swapchain.AcquireNextImage(semaphore: imageAcquiredSemaphore);

            submitFences[imageIndex].Wait();
            submitFences[imageIndex].Reset();

            // Submit recorded commands to graphics queue for execution.
            device.GraphicsQueue.Submit
            (
                waitSemaphore: imageAcquiredSemaphore, 
                waitDstStageMask: PipelineStages.Transfer, 
                commandBuffer: commandBuffers[imageIndex],
                signalSemaphore: renderingFinishedSemaphore,
                fence: submitFences[imageIndex]
            );

            // Present the color output to screen.
            device.GraphicsQueue.PresentKhr(renderingFinishedSemaphore, swapchain, imageIndex);
        }

        public void Dispose()
        {
            if(disposed)
            {
                DisposeSwapchain();
                disposed = true;
            }
        }

        private void DisposeSwapchain()
        {
            if(commandBuffers != null)
            {
                for (int i = 0; i < commandBuffers.Length; i++)
                    commandBuffers[i].Dispose();
            }
            if(submitFences != null)
            {
                for (int i = 0; i < submitFences.Length; i++)
                    submitFences[i].Dispose();
            }
            imageAcquiredSemaphore?.Dispose();
            renderingFinishedSemaphore?.Dispose();
            swapchain?.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if(disposed)
                throw new Exception($"[{nameof(Renderer)}] Allready disposed");
        }
    }
}