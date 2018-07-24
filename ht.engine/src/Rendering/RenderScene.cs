using System;
using System.Collections.Generic;

using HT.Engine.Math;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class RenderScene : IDisposable
    {
        //Public properties
        public Camera Camera => camera;

        //Internal properties
        internal Device LogicalDevice => window.LogicalDevice;
        internal HostDevice HostDevice => window.HostDevice;
        internal DescriptorManager DescriptorManager => descriptorManager;
        internal RenderPass RenderPass => renderpass;
        internal Memory.Pool MemoryPool => memoryPool;
        internal TransientExecutor Executor => executor;
        internal Memory.HostBuffer StagingBuffer => stagingBuffer;
        internal Memory.HostBuffer SceneDataBuffer => sceneDataBuffer;
        internal Int2 SwapchainSize => swapchainSize;
        internal bool Dirty => dirty;

        //Data
        private readonly Camera camera;
        private readonly Window window;
        private readonly Byte4 clearColor;
        private readonly Logger logger;
        private readonly TransientExecutor executor;
        private readonly Memory.Pool memoryPool;
        private readonly Memory.HostBuffer stagingBuffer;
        private readonly Memory.HostBuffer sceneDataBuffer;
        private readonly DescriptorManager descriptorManager;
        private readonly List<RenderObject> renderObjects = new List<RenderObject>();

        private RenderPass renderpass;
        private Int2 swapchainSize;
        private DeviceTexture depthTexture;
        private bool dirty;
        private bool disposed;
        
        public RenderScene(Window window, Byte4 clearColor, Logger logger = null)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));
            this.window = window;
            this.clearColor = clearColor;
            this.logger = logger;
            camera = new Camera();

            //Create resources
            executor = new TransientExecutor(window.LogicalDevice, window.GraphicsFamilyIndex);
            memoryPool = new Memory.Pool(window.LogicalDevice, window.HostDevice, logger);
            stagingBuffer = new Memory.HostBuffer(
                window.LogicalDevice,
                memoryPool,
                BufferUsages.TransferSrc,
                size: ByteUtils.MegabyteToByte(16));
            sceneDataBuffer = new Memory.HostBuffer(
                window.LogicalDevice,
                memoryPool,
                BufferUsages.UniformBuffer,
                size: SceneData.SIZE);
            descriptorManager = new DescriptorManager(window.LogicalDevice, logger);

            //Create the renderpass
            CreateRenderpass(window.LogicalDevice, window.SurfaceFormat);
        }

        public void AddObject(RenderObject renderObject)
        {
            renderObjects.Add(renderObject);
            dirty = true;
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            renderObjects.DisposeAll();
            depthTexture?.Dispose();
            renderpass.Dispose();
            descriptorManager.Dispose();
            stagingBuffer.Dispose();
            sceneDataBuffer.Dispose();
            memoryPool.Dispose();
            executor.Dispose();
            disposed = true;
        }

        internal Framebuffer CreateFramebuffer(ImageView swapchainImageView, Int2 swapchainSize)
        {
            ThrowIfDisposed();

            if (this.swapchainSize != swapchainSize)
            {
                //Dispose of the old depth texture
                depthTexture?.Dispose();
                depthTexture = DeviceTexture.CreateDepthTexture(
                    swapchainSize, window.LogicalDevice, memoryPool, executor);
                this.swapchainSize = swapchainSize;
            }

            return renderpass.CreateFramebuffer(new FramebufferCreateInfo(
                attachments: new [] { swapchainImageView, depthTexture.View },
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

            //Record all individual objects
            for (int i = 0; i < renderObjects.Count; i++)
                renderObjects[i].Record(commandbuffer);
            
            commandbuffer.CmdEndRenderPass();

            //After recording all objects we unset the dirty flag
            dirty = false;
        }

        internal void PreDraw(FrameTracker tracker)
        {
            //Update the scene data
            float aspect = (float)swapchainSize.X / swapchainSize.Y;
            Float4x4 cameraMatrix = camera.Transformation;
            Float4x4 viewMatrix = camera.Transformation.Invert();
            Float4x4 projectionMatrix = camera.GetProjection(aspect);
            SceneData sceneData = new SceneData(
                cameraMatrix,
                viewMatrix,
                projectionMatrix,
                tracker.FrameNumber,
                (float)tracker.ElapsedTime,
                tracker.DeltaTime);
            sceneDataBuffer.Write(sceneData);
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
                format: DeviceTexture.DepthFormat,
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