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
        private readonly RenderScene scene;
        private readonly Logger logger;
        private readonly Device logicalDevice;
        private readonly Queue graphicsQueue;
        private readonly Queue presentQueue;
        private readonly PresentModeKhr presentMode;
        private readonly Format surfaceFormat;
        private readonly ColorSpaceKhr surfaceColorspace;

        private SwapchainKhr swapchain;
        private Int2 swapchainSize;
        private Image[] swapchainImages;
        private ImageView[] swapchainImageViews;
        private Framebuffer[] framebuffers;
        private CommandPool commandPool;
        private CommandBuffer[] commandbuffers;
        private Semaphore imageAvailableSemaphore;
        private Semaphore renderFinishedSemaphore;
        private Fence[] waitFences;
        private bool disposed;

        internal Window(
            INativeWindow nativeWindow,
            SurfaceKhr surface,
            HostDevice hostDevice,
            RenderScene scene,
            Logger logger = null)
        {
            if (nativeWindow == null)
                throw new ArgumentNullException(nameof(nativeWindow));
            if (surface == null)
                throw new ArgumentNullException(nameof(surface));
            if (hostDevice == null)
                throw new ArgumentNullException(nameof(hostDevice));
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));
            this.nativeWindow = nativeWindow;
            this.surface = surface;
            this.hostDevice = hostDevice;
            this.scene = scene;
            this.logger = logger;

            //Subscribe to callbacks for the native window
            nativeWindow.CloseRequested += OnNativeWindowCloseRequested;
            nativeWindow.Resized += OnNativeWindowResized;

            //Create a logical device (and queues on the device)
            (logicalDevice, graphicsQueue, presentQueue) = hostDevice.CreateLogicalDevice(surface);
            presentMode = hostDevice.GetPresentMode(surface);
            (surfaceFormat, surfaceColorspace) = hostDevice.GetSurfaceFormat(surface);

            //Create a command-pool attached to this device
            commandPool = logicalDevice.CreateCommandPool(new CommandPoolCreateInfo(
                queueFamilyIndex: graphicsQueue.FamilyIndex,
                flags: CommandPoolCreateFlags.None
            ));

            //Initialize the scene (so it can create its renderpass and pipelines etc)
            //Note: currently using the graphicsQueue also for transfering data
            scene.Initialize(logicalDevice, hostDevice, surfaceFormat, 
                transferQueueFamilyIndex: graphicsQueue.FamilyIndex);

            //Initialize the entire setup
            CreateSwapchainSetup();
        }

        public bool Draw()
        {
            if (nativeWindow.Minimized)
                return false;

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

            return true;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                //Wait for all rendering to stop before we start disposing of resources
                logicalDevice.WaitIdle();

                DisposeSwapchainSetup();

                scene.Deinitialize();
                commandPool.Dispose();
                logicalDevice.Dispose();
                nativeWindow.Dispose();
                disposed = true;
            }
        }

        private void CreateSwapchainSetup()
        {
            CreateSwapchain(nativeWindow.ClientRect.Size);
            CreateFramebuffers();
            CreateCommandbuffers();
            CreateSynchronizationObjects();

            nativeWindow.Title = $"{hostDevice.Name} - {nativeWindow.ClientRect.Size}";
        }

        private void DisposeSwapchainSetup()
        {
            imageAvailableSemaphore.Dispose();
            renderFinishedSemaphore.Dispose();
            waitFences.DisposeAll();
            commandPool.Reset(CommandPoolResetFlags.ReleaseResources);
            framebuffers.DisposeAll();
            swapchainImageViews.DisposeAll();
            swapchain.Dispose();
        }

        private void RecreateRenderSetup()
        {
            //Wait for all rendering to stop
            logicalDevice.WaitIdle();

            DisposeSwapchainSetup();
            CreateSwapchainSetup();
        }

        private void CreateSwapchain(Int2 size)
        {
            SurfaceCapabilitiesKhr capabilities = hostDevice.GetCurrentCapabilities(surface);
            //Clamp the size to within the min and max extends reported by the surface capabilities
            swapchainSize = size.Clamp(
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

        private void CreateFramebuffers()
        {
            framebuffers = new Framebuffer[swapchainImages.Length];
            for (int i = 0; i < framebuffers.Length; i++)
            {
                framebuffers[i] = scene.CreateFramebuffer(
                    new FramebufferCreateInfo(
                        attachments: new [] { swapchainImageViews[i] },
                        width: swapchainSize.X,
                        height: swapchainSize.Y,
                        layers: 1));
            }
        }

        private void CreateCommandbuffers()
        {
            commandbuffers = commandPool.AllocateBuffers(new CommandBufferAllocateInfo(
                level: CommandBufferLevel.Primary,
                count: framebuffers.Length
            ));

            //Record the primary command-buffers
            for (int i = 0; i < commandbuffers.Length; i++)
            {
                commandbuffers[i].Begin(new CommandBufferBeginInfo(flags: CommandBufferUsages.None));

                scene.Record(commandbuffers[i], framebuffers[i], swapchainSize);

                commandbuffers[i].End();
            }
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
            if (!nativeWindow.Minimized)
                RecreateRenderSetup();
        }
        private void OnNativeWindowCloseRequested() => CloseRequested?.Invoke();
    }
}