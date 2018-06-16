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
        private Int2 swapchainSize;
        private Image[] swapchainImages;
        private ImageView[] swapchainImageViews;
        private RenderPass renderpass;
        private Framebuffer[] framebuffers;
        private CommandPool commandPool;
        private CommandBuffer[] commandbuffers;
        private Semaphore imageAvailableSemaphore;
        private Semaphore renderFinishedSemaphore;
        private Fence[] waitFences;
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

            //Setup swapchain
            CreateSwapChain(nativeWindow.ClientRect.Size);
            CreateRenderPass();
            CreateFrameBuffers();
            CreateCommandPool();
            CreateCommandBuffers();
            CreateSynchronizationObjects();
        }

        public void Draw()
        {
            int nextImage = swapchain.AcquireNextImage(semaphore: imageAvailableSemaphore);

            //Wait for the previous submit of this buffer to be done
            waitFences[nextImage].Wait();
            waitFences[nextImage].Reset();

            //Once we have acquired an image we submit a commandbuffer for writing to it
            graphicsQueue.Submit
            (
                waitSemaphore: imageAvailableSemaphore,
                waitDstStageMask: PipelineStages.ColorAttachmentOutput,
                commandBuffer: commandbuffers[nextImage],
                signalSemaphore: renderFinishedSemaphore,
                fence: waitFences[nextImage]
            );

            //Once rendering to the framebuffer is done we can present it
            presentQueue.PresentKhr(waitSemaphore: renderFinishedSemaphore, swapchain: swapchain, imageIndex: nextImage);
        }

        public void Dispose()
        {
            if(!disposed)
            {
                //Wait for all rendering to stop before we start disposing of resources
                logicalDevice.WaitIdle();

                imageAvailableSemaphore.Dispose();
                renderFinishedSemaphore.Dispose();
                waitFences.DisposeAll();

                commandPool.Dispose();
                framebuffers.DisposeAll();
                renderpass.Dispose();
                swapchainImageViews.DisposeAll();
                swapchain.Dispose();

                logicalDevice.Dispose();
                nativeWindow.Dispose();
                disposed = true;
            }
        }

        private void CreateSwapChain(Int2 size)
        {
            SurfaceCapabilitiesKhr capabilities = hostDevice.GetCurrentCapabilities(surface);
            //Clamp the size to within the min and max extends reported by the surface capabilities
            swapchainSize = size.Clamp(new Int2(capabilities.MinImageExtent.Width, capabilities.MinImageExtent.Height), new Int2(capabilities.MaxImageExtent.Width, capabilities.MaxImageExtent.Height));

            //Choose the amount of swap-chain images (try 1 more then min to support triple buffering)
            int desiredImageCount = capabilities.MinImageCount + 1;
            if(capabilities.MaxImageCount > 0 && desiredImageCount > capabilities.MaxImageCount) //'MaxImageCount' of 0 means no limit
                desiredImageCount = capabilities.MaxImageCount;

            //Gather info about the swapchain
            var createInfo = new SwapchainCreateInfoKhr();
            createInfo.Surface = surface;
            createInfo.MinImageCount = desiredImageCount;
            createInfo.ImageFormat = surfaceFormat;
            createInfo.ImageColorSpace = surfaceColorspace;
            createInfo.ImageExtent = new Extent2D(swapchainSize.X, swapchainSize.Y);
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
            swapchainImages = swapchain.GetImages();

            //Create the image views
            swapchainImageViews = new ImageView[swapchainImages.Length];
            for (int i = 0; i < swapchainImageViews.Length; i++)
                swapchainImageViews[i] = swapchainImages[i].CreateView(new ImageViewCreateInfo
                (
                    format: surfaceFormat,
                    viewType: ImageViewType.Image2D,
                    components: new ComponentMapping(r: ComponentSwizzle.R, g: ComponentSwizzle.G, b: ComponentSwizzle.B, a: ComponentSwizzle.A),
                    subresourceRange: new ImageSubresourceRange(ImageAspects.Color, baseMipLevel: 0, levelCount: 1, baseArrayLayer: 0, layerCount: 1)
                ));
            
            logger?.Log(nameof(Window), $"Swapchain created (size: {swapchainSize}, imgCount: {swapchainImages.Length}, mode: {presentMode}, format: {surfaceFormat}, colorSpace: {surfaceColorspace})");
        }

        private void CreateRenderPass()
        {
            //Description of our frame-buffer attachment
            AttachmentDescription colorAttachment = new AttachmentDescription();
            colorAttachment.Format = surfaceFormat;
            colorAttachment.Samples = SampleCounts.Count1;
            colorAttachment.LoadOp = AttachmentLoadOp.Clear;
            colorAttachment.StoreOp = AttachmentStoreOp.Store;
            colorAttachment.StencilLoadOp = AttachmentLoadOp.DontCare;
            colorAttachment.StencilStoreOp = AttachmentStoreOp.DontCare;
            colorAttachment.InitialLayout = ImageLayout.Undefined;
            colorAttachment.FinalLayout = ImageLayout.PresentSrcKhr;

            //Dependency to wait on the framebuffer being loaded before we write to it
            SubpassDependency attachmentAvailableDependency = new SubpassDependency();
            attachmentAvailableDependency.SrcSubpass = Constant.SubpassExternal; //Source is the implicit 'load' subpass
            attachmentAvailableDependency.DstSubpass = 0; //Dest is our subpass
            attachmentAvailableDependency.SrcStageMask = PipelineStages.ColorAttachmentOutput;
            attachmentAvailableDependency.SrcAccessMask = 0;
            attachmentAvailableDependency.DstStageMask = PipelineStages.ColorAttachmentOutput;
            attachmentAvailableDependency.DstAccessMask = Accesses.ColorAttachmentRead | Accesses.ColorAttachmentWrite;

            //Create the renderpass with a single sub-pass that references the color-attachment
            renderpass = logicalDevice.CreateRenderPass(new RenderPassCreateInfo
            (
                subpasses: new [] 
                {
                    new SubpassDescription
                    (
                        flags: SubpassDescriptionFlags.None,
                        colorAttachments: new [] { new AttachmentReference(attachment: 0, layout: ImageLayout.ColorAttachmentOptimal) }
                    )
                },
                attachments: new [] { colorAttachment },
                dependencies: new [] { attachmentAvailableDependency }
            ));
            logger?.Log(nameof(Window), $"Renderpass created");
        }

        private void CreateFrameBuffers()
        {
            framebuffers = new Framebuffer[swapchainImages.Length];
            for (int i = 0; i < framebuffers.Length; i++)
            {
                framebuffers[i] = renderpass.CreateFramebuffer(new FramebufferCreateInfo
                (
                    attachments: new [] { swapchainImageViews[i] },
                    width: swapchainSize.X,
                    height: swapchainSize.Y,
                    layers: 1
                ));
            }
            logger?.Log(nameof(Window), $"Created {framebuffers.Length} framebuffers");
        }

        private void CreateCommandPool()
        {
            commandPool = logicalDevice.CreateCommandPool(new CommandPoolCreateInfo
            (
                queueFamilyIndex: graphicsQueue.FamilyIndex,
                flags: CommandPoolCreateFlags.None
            ));
            logger?.Log(nameof(Window), $"Created commandpool");
        }

        private void CreateCommandBuffers()
        {
            commandbuffers = commandPool.AllocateBuffers(new CommandBufferAllocateInfo
            (
                level: CommandBufferLevel.Primary,
                count: framebuffers.Length
            ));

            //Record the command-buffers
            for (int i = 0; i < commandbuffers.Length; i++)
            {
                commandbuffers[i].Begin(new CommandBufferBeginInfo(flags: CommandBufferUsages.SimultaneousUse));

                commandbuffers[i].CmdBeginRenderPass(new RenderPassBeginInfo
                (
                    framebuffer: framebuffers[i],
                    renderArea: new Rect2D(x: 0, y: 0, width: swapchainSize.X, height: swapchainSize.Y),
                    clearValues: new ClearValue(new ClearColorValue(new ColorF4(0f, 1f, 0f, 1f)))
                ));

                commandbuffers[i].End();
            }
            logger?.Log(nameof(Window), $"Created and recorded {commandbuffers.Length} commandbuffers");
        }

        private void CreateSynchronizationObjects()
        {
            imageAvailableSemaphore = logicalDevice.CreateSemaphore();
            renderFinishedSemaphore = logicalDevice.CreateSemaphore();
            waitFences = new Fence[swapchainImages.Length];
            for (int i = 0; i < waitFences.Length; i++)
                waitFences[i] = logicalDevice.CreateFence(new FenceCreateInfo(FenceCreateFlags.Signaled));
        }

        private void OnNativeCloseRequested() => CloseRequested?.Invoke();
    }
}