using System;
using System.Collections.Generic;

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

        internal bool DebugMarkerIsSupported => hasDebugMarkerExtension;
        internal int SwapchainCount => swapchainCount;
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
        private readonly bool hasDebugMarkerExtension;
        private readonly PresentModeKhr presentMode;
        private readonly Format surfaceFormat;
        private readonly ColorSpaceKhr surfaceColorspace;
        private readonly int swapchainCount;

        private RenderScene scene;
        private SwapchainKhr swapchain;
        private Int2 swapchainSize;
        private DeviceTexture[] swapchainTextures;
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
            IList<string> enabledExtensions;
            (logicalDevice, graphicsQueue, presentQueue, enabledExtensions) = hostDevice.CreateLogicalDevice(
                surface: surface, 
                deviceRequirements: deviceRequirements);
            hasDebugMarkerExtension = enabledExtensions.Contains("VK_EXT_debug_marker");
            if (hasDebugMarkerExtension)
                logger?.Log(nameof(Window), "Enabled debug-markers");

            //Get a presentmode to use
            presentMode = hostDevice.GetPresentMode(surface);
            //Get the surfaceformat to use
            (surfaceFormat, surfaceColorspace) = hostDevice.GetSurfaceFormat(surface);
            //Gets how many swapchain images we should use
            swapchainCount = hostDevice.GetSwapchainCount(surface);

            //Create a command-pool attached to this device
            commandPool = logicalDevice.CreateCommandPool(new CommandPoolCreateInfo(
                queueFamilyIndex: graphicsQueue.FamilyIndex,
                flags: CommandPoolCreateFlags.None
            ));

            //Create the swapchain (images to present to the screen)
            CreateSwapchain(swapchainCount);
            //Synchronization objects are used to sync the rendering and presenting
            CreateSynchronizationObjects(); 
        }

        public void AttachScene(RenderScene scene)
        {
            ThrowIfDisposed();

            //Release resources that are tied to the previous scene
            this.scene?.Dispose();

            this.scene = scene;
            CreateRenderCommands(scene);
        }

        public void Draw(FrameTracker frameTracker)
        {
            if (commandbuffers == null || commandbuffers.Length == 0)
                throw new Exception($"[{nameof(Window)}] No command buffers have been created yet");
            ThrowIfDisposed();

            //If the scene is dirty we re-record the command-buffers
            if (scene?.Dirty == true)
            {
                logicalDevice.WaitIdle();
                CreateRenderCommands(scene);
            }

            int swapchainIndex = swapchain.AcquireNextImage(semaphore: imageAvailableSemaphore);

            //Wait for the previous submit of this buffer to be done
            waitFences[swapchainIndex].Wait();
            waitFences[swapchainIndex].Reset();

            //Give the scene a chance to prepare some data for drawing
            scene?.PreDraw(frameTracker, swapchainIndex);

            //Once we have acquired an image we submit a commandbuffer for writing to it
            graphicsQueue.Submit(
                waitSemaphore: imageAvailableSemaphore,
                waitDstStageMask: PipelineStages.ColorAttachmentOutput,
                commandBuffer: commandbuffers[swapchainIndex],
                signalSemaphore: renderFinishedSemaphore,
                fence: waitFences[swapchainIndex]
            );

            //Once rendering to the framebuffer is done we can present it
            presentQueue.PresentKhr(
                waitSemaphore: renderFinishedSemaphore,
                swapchain: swapchain,
                imageIndex: swapchainIndex);

            //Set the window title mostly for debugging purposes
            nativeWindow.Title = 
                $"{title} - {hostDevice.Name} - {nativeWindow.ClientRect.Size} - Fps: {frameTracker.SmoothedFps:N1}";
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

                //Dispose of the swapchain
                swapchainTextures.DisposeAll();
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
            scene.CreateResources(swapchainSize, swapchainTextures);

            //Reset the pool (to release any previously created buffers)
            commandPool.Reset(CommandPoolResetFlags.ReleaseResources);

            //Allocate new command buffers
            commandbuffers = commandPool.AllocateBuffers(new CommandBufferAllocateInfo(
                level: CommandBufferLevel.Primary,
                count: swapchainTextures.Length
            ));

            //Record the primary command-buffers
            for (int i = 0; i < commandbuffers.Length; i++)
            {
                commandbuffers[i].Begin(new CommandBufferBeginInfo(flags: CommandBufferUsages.None));
                scene.Record(commandbuffers[i], i);
                commandbuffers[i].End();
            }

            logger?.Log(nameof(Window), $"Rendercommands recorded ({commandbuffers.Length} command-buffers)");
        }

        private void CreateSwapchain(int swapchainCount)
        {
            SurfaceCapabilitiesKhr capabilities = hostDevice.GetCurrentCapabilities(surface);
            //Clamp the size to within the min and max extends reported by the surface capabilities
            swapchainSize = nativeWindow.ClientRect.Size.Clamp(
                new Int2(capabilities.MinImageExtent.Width, capabilities.MinImageExtent.Height),
                new Int2(capabilities.MaxImageExtent.Width, capabilities.MaxImageExtent.Height));

            //Gather info about the swapchain
            var createInfo = new SwapchainCreateInfoKhr
            (
                surface: surface,
                minImageCount: swapchainCount,
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
            var swapchainImages = swapchain.GetImages();

            //Verify that we got the amount of images we expected
            if (swapchainImages.Length != swapchainCount)
                throw new Exception(
                    $"[{nameof(Window)}] Incorrect number of swapchain images acquired, expected: {swapchainCount}, got: {swapchainImages.Length}");

            //Create the image targets
            swapchainTextures = new DeviceTexture[swapchainCount];
            for (int i = 0; i < swapchainTextures.Length; i++)
                swapchainTextures[i] = DeviceTexture.CreateSwapchainTarget(
                    swapchainSize, surfaceFormat, swapchainImages[i]);

            logger?.Log(nameof(Window), 
$@"Swapchain created:
{{
    size: {swapchainSize},
    texCount: {swapchainTextures.Length},
    mode: {presentMode},
    format: {surfaceFormat},
    colorSpace: {surfaceColorspace}
}}");
        }

        private void CreateSynchronizationObjects()
        {
            imageAvailableSemaphore = logicalDevice.CreateSemaphore();
            renderFinishedSemaphore = logicalDevice.CreateSemaphore();
            waitFences = new Fence[swapchainCount];
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
                swapchainTextures.DisposeAll();
                swapchain.Dispose();

                //Recreate the swapchain
                CreateSwapchain(swapchainCount);
                CreateSynchronizationObjects();

                if (scene != null)
                {
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