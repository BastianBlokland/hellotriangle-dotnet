using System;

using HT.Engine.Math;
using HT.Engine.Platform;
using HT.Engine.Utils;
using VulkanCore;
using VulkanCore.Khr;

namespace HT.Engine.Rendering
{
    public sealed class Window : IDisposable
    {
        public event Action CloseRequested;

        private readonly INativeWindow nativeWindow;
        private readonly SurfaceKhr surface;
        private readonly HostDevice hostDevice;
        private readonly Logger logger;
        private readonly Device logicalDevice;
        private readonly Queue graphicsQueue;
        private readonly Queue presentQueue;
        private readonly PresentModeKhr presentMode;
        private readonly Format surfaceFormat;
        private readonly ColorSpaceKhr surfaceColorspace;

        private SwapchainKhr swapchain;
        private bool disposed;

        internal Window(INativeWindow nativeWindow, SurfaceKhr surface, HostDevice hostDevice, Logger logger = null)
        {
            this.nativeWindow = nativeWindow;
            this.surface = surface;
            this.hostDevice = hostDevice;
            this.logger = logger;

            //Subscribe to callbacks for the native window
            nativeWindow.CloseRequested += OnNativeCloseRequested;

            //Create a logical device (and queues on the device)
            (logicalDevice, graphicsQueue, presentQueue) = hostDevice.CreateLogicalDevice(surface);
            presentMode = hostDevice.GetPresentMode(surface);
            (surfaceFormat, surfaceColorspace) = hostDevice.GetSurfaceFormat(surface);

            //Create the swapchain
            CreateSwapChain(nativeWindow.ClientRect.Size);
        }

        public void Dispose()
        {
            if(!disposed)
            {
                logicalDevice.Dispose();
                nativeWindow.Dispose();
                disposed = true;
            }
        }

        private void CreateSwapChain(Int2 size)
        {
            SurfaceCapabilitiesKhr capabilities = hostDevice.GetCurrentCapabilities(surface);
            //Clamp the size to within the min and max extends reported by the surface capabilities
            size = size.Clamp(new Int2(capabilities.MinImageExtent.Width, capabilities.MinImageExtent.Height), new Int2(capabilities.MaxImageExtent.Width, capabilities.MaxImageExtent.Height));

            //Choose the amount of swap-chain images (try 1 more then min to support triple buffering)
            int imageCount = capabilities.MinImageCount + 1;
            if(capabilities.MaxImageCount > 0 && imageCount > capabilities.MaxImageCount) //'MaxImageCount' of 0 means no limit
                imageCount = capabilities.MaxImageCount;

            //Gather info about the swapchain
            var createInfo = new SwapchainCreateInfoKhr();
            createInfo.Surface = surface;
            createInfo.MinImageCount = imageCount;
            createInfo.ImageFormat = surfaceFormat;
            createInfo.ImageColorSpace = surfaceColorspace;
            createInfo.ImageExtent = new Extent2D(size.X, size.Y);
            createInfo.ImageArrayLayers = 1;
            createInfo.ImageUsage = ImageUsages.ColorAttachment;
            //If we have a different present-queue graphics-queue then the graphics-queue then we need to allow sharing of 
            //the swapchain images
            if(graphicsQueue.FamilyIndex != presentQueue.FamilyIndex)
            {
                createInfo.ImageSharingMode = SharingMode.Concurrent;
                createInfo.QueueFamilyIndices = new [] { graphicsQueue.FamilyIndex, presentQueue.FamilyIndex };
            }
            else
                createInfo.ImageSharingMode = SharingMode.Exclusive;
            createInfo.PreTransform = capabilities.CurrentTransform;
            createInfo.CompositeAlpha = CompositeAlphasKhr.Opaque;
            createInfo.PresentMode = presentMode;
            createInfo.Clipped = true;
            createInfo.OldSwapchain = swapchain; //Reference to the previous swapchain so when we recreate it can reuse some resources

            //Create the swapchain
            swapchain = logicalDevice.CreateSwapchainKhr(createInfo);

            logger?.Log(nameof(Window), $"Swapchain created (size: {size}, imgCount: {imageCount}, mode: {presentMode}, format: {surfaceFormat}, colorSpace: {surfaceColorspace})");
        }

        private void OnNativeCloseRequested() => CloseRequested?.Invoke();
    }
}