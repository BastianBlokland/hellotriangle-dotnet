using System;

using HT.Engine.Math;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class RenderScene : IDisposable
    {
        internal readonly HostDeviceRequirements DeviceRequirements = new HostDeviceRequirements(
            samplerAnisotropy: true);

        private readonly Float4 clearColor;
        private readonly RenderObject[] renderobjects;

        private bool initialized;
        private Device logicalDevice;
        private Memory.Copier copier;
        private Memory.Pool memoryPool;
        private Memory.StagingBuffer stagingBuffer;
        private DescriptorPool descriptorPool;
        private RenderPass renderpass;
        private Int2 swapchainSize;
        private Format depthFormat;
        private Image depthImage;
        private ImageView depthImageView;
        
        public RenderScene(Float4 clearColor, RenderObject[] renderobjects)
        {
            if (renderobjects == null)
                throw new ArgumentNullException(nameof(renderobjects));
                
            this.clearColor = clearColor;
            this.renderobjects = renderobjects;
        }

        public void Dispose()
        {
            if (initialized)
                Deinitialize();
        }

        internal void Initialize(
            Device logicalDevice,
            HostDevice hostDevice,
            Format surfaceFormat,
            int transferQueueFamilyIndex)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (hostDevice == null)
                throw new ArgumentNullException(nameof(hostDevice));
            if (initialized)
                throw new Exception(
                    $"[{nameof(RenderScene)}] Allready initialized");

            this.logicalDevice = logicalDevice;

            //Allocate gpu memory
            copier = new Memory.Copier(logicalDevice, transferQueueFamilyIndex);
            memoryPool = new Memory.Pool(logicalDevice, hostDevice);
            stagingBuffer = new Memory.StagingBuffer(logicalDevice, hostDevice, copier);

            //Create a descriptor pool for the render-objects to create descriptor-sets from
            descriptorPool = logicalDevice.CreateDescriptorPool(new DescriptorPoolCreateInfo(
                maxSets: renderobjects.Length,
                poolSizes: new []
                {
                    new DescriptorPoolSize(DescriptorType.UniformBuffer, renderobjects.Length),
                    new DescriptorPoolSize(DescriptorType.CombinedImageSampler, renderobjects.Length)
                },
                flags: DescriptorPoolCreateFlags.None));

            //Pick a depth format
            depthFormat = Format.D32SFloat;
            if (!hostDevice.IsFormatSupported(depthFormat, ImageTiling.Optimal, FormatFeatures.DepthStencilAttachment))
                throw new Exception($"[{nameof(RenderScene)}] Device does not support target depth format");

            //Create the renderpass
            CreateRenderpass(logicalDevice, surfaceFormat);

            //Initialize all the renderobjects
            for (int i = 0; i < renderobjects.Length; i++)
                renderobjects[i].Initialize(
                    logicalDevice,
                    hostDevice,
                    descriptorPool,
                    renderpass,
                    memoryPool,
                    stagingBuffer);
        
            initialized = true;
        }

        internal void SetupSwapchain(Int2 swapchainSize)
        {
            this.swapchainSize = swapchainSize;

            //If we had allready setup depth images then we dispose those
            //This happens during resizing
            depthImageView?.Dispose();
            depthImage?.Dispose();

            //Create depth image
            depthImage = logicalDevice.CreateImage(new ImageCreateInfo {
                Flags = ImageCreateFlags.None,
                ImageType = ImageType.Image2D,
                Format = depthFormat,
                Extent = new Extent3D(swapchainSize.X, swapchainSize.Y, 1),
                MipLevels = 1,
                ArrayLayers = 1,
                Samples = SampleCounts.Count1,
                Tiling = ImageTiling.Optimal,
                Usage = ImageUsages.DepthStencilAttachment,
                SharingMode = SharingMode.Exclusive,
                InitialLayout = ImageLayout.Undefined});
            //Allocate memory for it
            memoryPool.AllocateAndBind(depthImage);
            //Create view on top of it
            depthImageView = depthImage.CreateView(new ImageViewCreateInfo(
                format: depthFormat,
                subresourceRange: new ImageSubresourceRange(
                    aspectMask: ImageAspects.Depth, baseMipLevel: 0, levelCount: 1, baseArrayLayer: 0, layerCount: 1),
                viewType: ImageViewType.Image2D));
            //Transition the image to the depth layout
            copier.TransitionImageLayout(
                image: depthImage,
                subresource: new ImageSubresourceLayers(ImageAspects.Depth, mipLevel: 0, baseArrayLayer: 0, layerCount: 1),
                oldLayout: ImageLayout.Undefined,
                newLayout: ImageLayout.DepthStencilAttachmentOptimal);
        }

        internal Framebuffer CreateFramebuffer(ImageView swapchainImageView, Int2 swapchainSize)
        {
            ThrowIfNotInitialized();
            return renderpass.CreateFramebuffer(new FramebufferCreateInfo(
                attachments: new [] { swapchainImageView, depthImageView },
                width: swapchainSize.X,
                height: swapchainSize.Y));
        }

        internal void Record(
            CommandBuffer commandbuffer,
            Framebuffer framebuffer)
        {
            ThrowIfNotInitialized();

            commandbuffer.CmdBeginRenderPass(new RenderPassBeginInfo(
                renderPass: renderpass,
                framebuffer: framebuffer,
                renderArea: new Rect2D(x: 0, y: 0, width: swapchainSize.X, height: swapchainSize.Y),
                clearValues: new [] 
                {
                    //Framebuffer color
                    new ClearValue(new ClearColorValue(new ColorF4(clearColor.R, clearColor.G, clearColor.B, clearColor.A))),
                    //Depthbuffer value
                    new ClearValue(new ClearDepthStencilValue(depth: 1f, stencil: 0))
                }));

            //Set viewport and scissor-rect dynamically to avoid the pipelines depending on
            //swapchain size (and thus having to be recreated on resize)
            commandbuffer.CmdSetViewport(
                new Viewport(
                    x: 0f, y: 0f, width: swapchainSize.X, height: swapchainSize.Y,
                    minDepth: 0f, maxDepth: 1f));
            commandbuffer.CmdSetScissor(
                new Rect2D(x: 0, y: 0, width: swapchainSize.X, height: swapchainSize.Y));

            //Draw our pipeline
            for (int i = 0; i < renderobjects.Length; i++)
                renderobjects[i].Record(commandbuffer);

            commandbuffer.CmdEndRenderPass();
        }

        internal void Update()
        {
            ThrowIfNotInitialized();

            Float4x4 viewMatrix =   Float4x4.CreateRotationFromXAngle(FloatUtils.DegreesToRadians(15f)) * 
                                    Float4x4.CreateTranslation((x: 0f, y: -.5f, z: -2));
            Float4x4 projectionMatrix = Float4x4.CreatePerspectiveProjection(
                Frustum.CreateFromVerticalAngleAndAspect(
                    verticalAngle: FloatUtils.DegreesToRadians(45f),
                    aspect: (float)swapchainSize.X / swapchainSize.Y,
                    nearDistance: .1f,
                    farDistance: 100f));
            
            for (int i = 0; i < renderobjects.Length; i++)
                renderobjects[i].Update(viewMatrix, projectionMatrix);
        }

        internal void Deinitialize()
        {
            if (!initialized)
                throw new Exception(
                    $"[{nameof(RenderScene)}] Unable to deinitialize as we haven't initialized");

            depthImageView.Dispose();
            depthImage.Dispose();
            renderobjects.DisposeAll();
            renderpass.Dispose();
            descriptorPool.Dispose();
            stagingBuffer.Dispose();
            memoryPool.Dispose();
            copier.Dispose();
            initialized = false;
        }

        private void CreateRenderpass(Device logicalDevice, Format surfaceFormat)
        {
            //Description of our frame-buffer attachment
            var colorAttachment = new AttachmentDescription(
                flags: AttachmentDescriptions.MayAlias,
                format: surfaceFormat,
                samples: SampleCounts.Count1,
                loadOp: AttachmentLoadOp.Clear,
                storeOp: AttachmentStoreOp.Store,
                stencilLoadOp: AttachmentLoadOp.DontCare,
                stencilStoreOp: AttachmentStoreOp.DontCare,
                initialLayout: ImageLayout.Undefined,
                finalLayout: ImageLayout.PresentSrcKhr
            );
            //Description of our depth-buffer attachment
            var depthAttachment = new AttachmentDescription(
                flags: AttachmentDescriptions.MayAlias,
                format: depthFormat,
                samples: SampleCounts.Count1,
                loadOp: AttachmentLoadOp.Clear,
                storeOp: AttachmentStoreOp.DontCare,
                stencilLoadOp: AttachmentLoadOp.DontCare,
                stencilStoreOp: AttachmentStoreOp.DontCare,
                initialLayout: ImageLayout.Undefined,
                finalLayout: ImageLayout.DepthStencilAttachmentOptimal
            );
            //Dependency to wait on the framebuffer being loaded before we write to it
            var framebufferAvailableDependency = new SubpassDependency(
                srcSubpass: Constant.SubpassExternal, //Source is the implicit 'load' subpass
                dstSubpass: 0, //Dest is our subpass
                srcStageMask: PipelineStages.ColorAttachmentOutput,
                srcAccessMask: 0,
                dstStageMask: PipelineStages.ColorAttachmentOutput,
                dstAccessMask: Accesses.ColorAttachmentRead | Accesses.ColorAttachmentWrite
            );
            //Create the renderpass with a single sub-pass that references the color-attachment
            renderpass = logicalDevice.CreateRenderPass(new RenderPassCreateInfo(
                subpasses: new [] 
                {
                    new SubpassDescription
                    (
                        flags: SubpassDescriptionFlags.None,
                        colorAttachments: new []
                        {
                            new AttachmentReference(
                                attachment: 0,
                                layout: ImageLayout.ColorAttachmentOptimal)
                        },
                        depthStencilAttachment: new AttachmentReference(
                            attachment: 1,
                            layout: ImageLayout.DepthStencilAttachmentOptimal)
                    )
                },
                attachments: new [] { colorAttachment, depthAttachment },
                dependencies: new [] { framebufferAvailableDependency }
            ));
        }

        private void ThrowIfNotInitialized()
        {
            if (!initialized)
                throw new Exception($"[{nameof(RenderScene)}] Not yet initialized");
        }
    }
}