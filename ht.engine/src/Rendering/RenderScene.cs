using System;

using HT.Engine.Math;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class RenderScene : IDisposable
    {
        private readonly Window window;
        private readonly Byte4 clearColor;
        private readonly Logger logger;
        private readonly Memory.Copier copier;
        private readonly Memory.Pool memoryPool;
        private readonly Memory.StagingBuffer stagingBuffer;
        private readonly Format depthFormat;

        private RenderPass renderpass;
        private Int2 swapchainSize;
        private Image depthImage;
        private ImageView depthImageView;
        private bool disposed;
        
        public RenderScene(Window window, Byte4 clearColor, Logger logger = null)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));
            this.window = window;
            this.clearColor = clearColor;
            this.logger = logger;

            //Create memory resources
            copier = new Memory.Copier(window.LogicalDevice, window.GraphicsFamilyIndex);
            memoryPool = new Memory.Pool(window.LogicalDevice, window.HostDevice, logger);
            stagingBuffer = new Memory.StagingBuffer(window.LogicalDevice, window.HostDevice, copier);

            //TODO: Create descriptor pool

            //Pick a depth format
            depthFormat = Format.D32SFloat;
            if (!window.HostDevice.IsFormatSupported(depthFormat, ImageTiling.Optimal, FormatFeatures.DepthStencilAttachment))
                throw new Exception($"[{nameof(RenderScene)}] Device does not support target depth format");

            //Create the renderpass
            CreateRenderpass(window.LogicalDevice, window.SurfaceFormat);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            depthImageView?.Dispose();
            depthImage?.Dispose();
            renderpass.Dispose();
            stagingBuffer.Dispose();
            memoryPool.Dispose();
            copier.Dispose();
            disposed = true;
        }

        internal Framebuffer CreateFramebuffer(ImageView swapchainImageView, Int2 swapchainSize)
        {
            ThrowIfDisposed();

            if (this.swapchainSize != swapchainSize)
            {
                //Dispose of the old depth image
                depthImageView?.Dispose();
                depthImage?.Dispose();

                //Create depth image
                depthImage = window.LogicalDevice.CreateImage(new ImageCreateInfo {
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
                
                this.swapchainSize = swapchainSize;
            }

            return renderpass.CreateFramebuffer(new FramebufferCreateInfo(
                attachments: new [] { swapchainImageView, depthImageView },
                width: swapchainSize.X,
                height: swapchainSize.Y));
        }

        internal void Record(
            CommandBuffer commandbuffer,
            Framebuffer framebuffer)
        {
            ThrowIfDisposed();

            Float4 normalizedClearColor = clearColor.Normalized;
            commandbuffer.CmdBeginRenderPass(new RenderPassBeginInfo(
                renderPass: renderpass,
                framebuffer: framebuffer,
                renderArea: new Rect2D(x: 0, y: 0, width: swapchainSize.X, height: swapchainSize.Y),
                clearValues: new [] 
                {
                    //Framebuffer color
                    new ClearValue(new ClearColorValue(new ColorF4(
                        normalizedClearColor.R,
                        normalizedClearColor.G,
                        normalizedClearColor.B,
                        normalizedClearColor.A))),
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

            //TODO: Draw all the renderobjects
            
            commandbuffer.CmdEndRenderPass();
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

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(RenderScene)}] Allready disposed");
        }
    }
}