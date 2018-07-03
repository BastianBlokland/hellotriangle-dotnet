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
        //Events
        public event Action CloseRequested;

        //Properties
        public bool IsCloseRequested => isCloseRequested;
        public bool IsMinimized => nativeWindow.IsMinimized;

        internal Device LogicalDevice => logicalDevice;
        internal HostDevice HostDevice => hostDevice;
        internal Format SurfaceFormat => surfaceFormat;
        internal int GraphicsFamilyIndex => graphicsQueue.FamilyIndex;

        //Data
        private readonly string title;
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

        private RenderScene scene;
        private SwapchainKhr swapchain;
        private Int2 swapchainSize;
        private VulkanCore.Image[] swapchainImages;
        private ImageView[] swapchainImageViews;
        private Framebuffer[] framebuffers;
        private CommandPool commandPool;
        private CommandBuffer[] commandbuffers;
        private Semaphore imageAvailableSemaphore;
        private Semaphore renderFinishedSemaphore;
        private Fence[] waitFences;
        private bool isCloseRequested;
        private bool disposed;

        internal Window(
            string title,
            INativeWindow nativeWindow,
            SurfaceKhr surface,
            HostDevice hostDevice,
            HostDeviceRequirements deviceRequirements,
            Logger logger = null)
        {
            if (nativeWindow == null)
                throw new ArgumentNullException(nameof(nativeWindow));
            if (surface == null)
                throw new ArgumentNullException(nameof(surface));
            if (hostDevice == null)
                throw new ArgumentNullException(nameof(hostDevice));
            this.title = title;
            this.nativeWindow = nativeWindow;
            this.surface = surface;
            this.hostDevice = hostDevice;
            this.logger = logger;

            //Subscribe to callbacks for the native window
            nativeWindow.CloseRequested += OnNativeWindowCloseRequested;
            nativeWindow.Resized += OnNativeWindowResized;

            //Create a logical device (and queues on the device)
            (logicalDevice, graphicsQueue, presentQueue) = hostDevice.CreateLogicalDevice(
                surface: surface, 
                deviceRequirements: deviceRequirements);
            //Get a presentmode to use
            presentMode = hostDevice.GetPresentMode(surface);
            //Get the surfaceformat to use
            (surfaceFormat, surfaceColorspace) = hostDevice.GetSurfaceFormat(surface);

            //Create a command-pool attached to this device
            commandPool = logicalDevice.CreateCommandPool(new CommandPoolCreateInfo(
                queueFamilyIndex: graphicsQueue.FamilyIndex,
                flags: CommandPoolCreateFlags.None
            ));

            //Create the swapchain (images to present to the screen)
            CreateSwapchain();
            //Synchronization objects are used to sync the rendering and presenting
            CreateSynchronizationObjects(); 
        }

        public void AttachScene(RenderScene scene)
        {
            ThrowIfDisposed();

            //Release resources that are tied to the previous scene
            this.scene?.Dispose();
            framebuffers?.DisposeAll();
            framebuffers = null;

            this.scene = scene;
            CreateRenderCommands(scene);
        }

        public void Draw()
        {
            if (commandbuffers == null || commandbuffers.Length == 0)
                throw new Exception($"[{nameof(Window)}] No command buffers have been created yet");
            ThrowIfDisposed();

            int nextImage = swapchain.AcquireNextImage(semaphore: imageAvailableSemaphore);

            //Wait for the previous submit of this buffer to be done
            waitFences[nextImage].Wait();
            waitFences[nextImage].Reset();

            //Once we have acquired an image we submit a commandbuffer for writing to it
            graphicsQueue.Submit(
                waitSemaphore: imageAvailableSemaphore,
                waitDstStageMask: PipelineStages.ColorAttachmentOutput,
                commandBuffer: commandbuffers[nextImage],
                signalSemaphore: renderFinishedSemaphore,
                fence: waitFences[nextImage]
            );

            //Once rendering to the framebuffer is done we can present it
            presentQueue.PresentKhr(
                waitSemaphore: renderFinishedSemaphore, swapchain: swapchain, imageIndex: nextImage);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                //Wait for all rendering to stop before we start disposing of resources
                logicalDevice.WaitIdle();

                //Dispose of the synchronization objects
                imageAvailableSemaphore.Dispose();
                renderFinishedSemaphore.Dispose();
                waitFences.DisposeAll();

                //Dispose of the scene resources
                scene?.Dispose();
                framebuffers?.DisposeAll();

                //Dispose of the swapchain
                swapchainImageViews.DisposeAll();
                swapchain.Dispose();

                //Dispose of command-pool (will automatically also dispose of the commandbuffers that
                //where recreated from it)
                commandPool.Dispose();

                //Dispose the Vulkan device and dispose of the os window
                logicalDevice.Dispose();
                nativeWindow.Dispose();
                disposed = true;
            }
        }

        private void CreateRenderCommands(RenderScene scene)
        {
            //If we have no framebuffers then first create those, can also happen during window resize
            if (framebuffers == null || framebuffers.Length == 0)
            {
                framebuffers = new Framebuffer[swapchainImages.Length];
                for (int i = 0; i < framebuffers.Length; i++)
                    framebuffers[i] = scene.CreateFramebuffer(swapchainImageViews[i], swapchainSize);
            }

            //Reset the pool (to release any previously created buffers)
            commandPool.Reset(CommandPoolResetFlags.ReleaseResources);

            //Allocate new command buffers
            commandbuffers = commandPool.AllocateBuffers(new CommandBufferAllocateInfo(
                level: CommandBufferLevel.Primary,
                count: framebuffers.Length
            ));

            //Record the primary command-buffers
            for (int i = 0; i < commandbuffers.Length; i++)
            {
                commandbuffers[i].Begin(new CommandBufferBeginInfo(flags: CommandBufferUsages.None));
                scene.Record(commandbuffers[i], framebuffers[i]);
                commandbuffers[i].End();
            }
        }

        private void CreateSwapchain()
        {
            SurfaceCapabilitiesKhr capabilities = hostDevice.GetCurrentCapabilities(surface);
            //Clamp the size to within the min and max extends reported by the surface capabilities
            swapchainSize = nativeWindow.ClientRect.Size.Clamp(
                new Int2(capabilities.MinImageExtent.Width, capabilities.MinImageExtent.Height),
                new Int2(capabilities.MaxImageExtent.Width, capabilities.MaxImageExtent.Height));

            //Choose the amount of swap-chain images (try 1 more then min to support triple
            //buffering) ('MaxImageCount' of 0 means no limit)
            int desiredImageCount = capabilities.MinImageCount + 1;
            if (capabilities.MaxImageCount > 0 && desiredImageCount > capabilities.MaxImageCount)
                desiredImageCount = capabilities.MaxImageCount;

            //Gather info about the swapchain
            var createInfo = new SwapchainCreateInfoKhr
            (
                surface: surface,
                minImageCount: desiredImageCount,
                imageFormat: surfaceFormat,
                imageColorSpace: surfaceColorspace,
                imageExtent: new Extent2D(swapchainSize.X, swapchainSize.Y),
                imageArrayLayers: 1,
                imageUsage: ImageUsages.ColorAttachment,

                //If the graphics and present queues are different we need to allow sharing the
                //swapchain images
                imageSharingMode: graphicsQueue.FamilyIndex == presentQueue.FamilyIndex ?
                    SharingMode.Exclusive : SharingMode.Concurrent,
                queueFamilyIndices: graphicsQueue.FamilyIndex == presentQueue.FamilyIndex ?
                    null : new [] { graphicsQueue.FamilyIndex, presentQueue.FamilyIndex },

                preTransform: capabilities.CurrentTransform,
                compositeAlpha: CompositeAlphasKhr.Opaque,
                presentMode: presentMode,
                clipped: true
            );

            //Create the swapchain
            swapchain = logicalDevice.CreateSwapchainKhr(createInfo);
            swapchainImages = swapchain.GetImages();

            //Create the image views
            swapchainImageViews = new ImageView[swapchainImages.Length];
            for (int i = 0; i < swapchainImageViews.Length; i++)
                swapchainImageViews[i] = swapchainImages[i].CreateView(new ImageViewCreateInfo(
                    format: surfaceFormat,
                    viewType: ImageViewType.Image2D,
                    components: new ComponentMapping(
                        r: ComponentSwizzle.R,
                        g: ComponentSwizzle.G,
                        b: ComponentSwizzle.B,
                        a: ComponentSwizzle.A),
                    subresourceRange: new ImageSubresourceRange(
                        aspectMask: ImageAspects.Color,
                        baseMipLevel: 0,
                        levelCount: 1,
                        baseArrayLayer: 0,
                        layerCount: 1)
                ));
            
            //Set the window title mostly for debugging purposes
            nativeWindow.Title = $"{title} - {hostDevice.Name} - {nativeWindow.ClientRect.Size}";

            logger?.Log(nameof(Window), 
$@"Swapchain created:
{{
    size: {swapchainSize},
    imgCount: {swapchainImages.Length},
    mode: {presentMode},
    format: {surfaceFormat},
    colorSpace: {surfaceColorspace}
}}");
        }

        private void CreateSynchronizationObjects()
        {
            imageAvailableSemaphore = logicalDevice.CreateSemaphore();
            renderFinishedSemaphore = logicalDevice.CreateSemaphore();
            waitFences = new Fence[swapchainImages.Length];
            for (int i = 0; i < waitFences.Length; i++)
                waitFences[i] = logicalDevice.CreateFence(
                    new FenceCreateInfo(FenceCreateFlags.Signaled));
        }

        private void OnNativeWindowResized()
        {
            if (!nativeWindow.IsMinimized)
            {
                //Wait for all rendering to stop
                logicalDevice.WaitIdle();

                //Dispose the previous synchronization objects, the only reason we do this is because
                //the number of wait-fences is tied to the number of swapchain images, and while
                //recreating the swapchain there is no promise that there will be the same amount of
                //swapchain images (altough i cannot think of a case where that would not be true)
                //luckily creating this is pretty cheap
                imageAvailableSemaphore.Dispose();
                renderFinishedSemaphore.Dispose();
                waitFences.DisposeAll();
            
                //Dispose the old swapchain setup
                swapchainImageViews.DisposeAll();
                swapchain.Dispose();

                //Recreate the swapchain
                CreateSwapchain();
                CreateSynchronizationObjects();

                if (scene != null)
                {
                    //We have to dispose the framebuffers because they are tied to the old swapchain
                    framebuffers.DisposeAll();
                    framebuffers = null;

                    //Create new render-commands on this new setup (will automatically create new 
                    //framebuffers too)
                    CreateRenderCommands(scene);
                }
            }
        }

        private void OnNativeWindowCloseRequested()
        {
            isCloseRequested = true;
            CloseRequested?.Invoke();
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(Window)}] Allready disposed");
        }
    }
}