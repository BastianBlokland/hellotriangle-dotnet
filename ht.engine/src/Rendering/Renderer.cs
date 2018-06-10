using System;

using VulkanCore;
using VulkanCore.Khr;

namespace HT.Engine.Rendering
{
    public sealed class Renderer : IDisposable
    {
        public GraphicsDevice Device => device;

        private readonly GraphicsDevice device;
        private readonly Surface surface;
        private readonly SurfaceCapabilitiesKhr capabilities;
        private readonly SwapchainKhr swapchain;
        private readonly Image[] swapchainImages;
        private readonly CommandBuffer[] commandBuffers;
        private readonly Fence[] submitFences;
        private bool disposed;

        public Renderer(GraphicsDevice device, Surface surface)
        {
            //Initialize the device (if it wasn't allready)
            device.Initialize();

            this.device = device;
            this.surface = surface;

            //Get capabilities of this surface / device combo
            capabilities = device.VulkanPhysicalDevice.GetSurfaceCapabilitiesKhr(surface.KhrSurface);
            int imageCount = System.Math.Min(capabilities.MinImageCount + 1, capabilities.MaxImageCount);
            var surfaceFormat = surface.GetFormat(device);
            var presentMode = surface.GetPresentMode(device);

            SwapchainCreateInfoKhr info = new SwapchainCreateInfoKhr
            (
                surface.KhrSurface,
                surfaceFormat.imageFormat,
                capabilities.CurrentExtent,
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

            for (int i = 0; i < commandBuffers.Length; i++)
            {
                commandBuffers[i].Begin(new CommandBufferBeginInfo(CommandBufferUsages.None));

                commandBuffers[i].End();
            }
        }

        public void Dispose()
        {
            if(disposed)
            {
                for (int i = 0; i < swapchainImages.Length; i++)
                    swapchainImages[i].Dispose();
                for (int i = 0; i < commandBuffers.Length; i++)
                    commandBuffers[i].Dispose();
                for (int i = 0; i < submitFences.Length; i++)
                    submitFences[i].Dispose();
                swapchain.Dispose();
                disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if(disposed)
                throw new Exception($"[{nameof(Renderer)}] Allready disposed");
        }
    }
}