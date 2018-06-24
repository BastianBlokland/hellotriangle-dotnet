using System;

using HT.Engine.Math;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class RenderScene : IDisposable
    {
        private readonly Float4 clearColor;
        private readonly RenderObject[] renderobjects;

        private bool initialized;
        private Memory.Copier copier;
        private Memory.Pool meshMemoryPool;
        private Memory.Pool transformationPool;
        private DescriptorPool descriptorPool;
        private RenderPass renderpass;
        private Int2 currentSwapchainSize;
        
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
            if (initialized)
                throw new Exception(
                    $"[{nameof(RenderScene)}] Allready initialized");

            //Allocate gpu memory
            copier = new Memory.Copier(logicalDevice, transferQueueFamilyIndex);
            meshMemoryPool = new Memory.Pool(
                logicalDevice, hostDevice, copier, BufferUsages.VertexBuffer | BufferUsages.IndexBuffer);
            transformationPool = new Memory.Pool(
                logicalDevice, hostDevice, copier, BufferUsages.UniformBuffer);

            //Create a descriptor pool for the render-objects to create descriptor-sets from
            descriptorPool = logicalDevice.CreateDescriptorPool(new DescriptorPoolCreateInfo(
                maxSets: renderobjects.Length,
                poolSizes: new []
                {
                    new DescriptorPoolSize(DescriptorType.UniformBuffer, renderobjects.Length)
                },
                flags: DescriptorPoolCreateFlags.None));

            //Create the renderpass
            CreateRenderpass(logicalDevice, surfaceFormat);

            //Initialize all the renderobjects
            for (int i = 0; i < renderobjects.Length; i++)
                renderobjects[i].Initialize(
                    logicalDevice,
                    hostDevice,
                    descriptorPool,
                    renderpass,
                    meshMemoryPool,
                    transformationPool);
            
            initialized = true;
        }

        internal Framebuffer CreateFramebuffer(FramebufferCreateInfo createInfo)
        {
            ThrowIfNotInitialized();
            return renderpass.CreateFramebuffer(createInfo);
        }

        internal void Record(
            CommandBuffer commandbuffer,
            Framebuffer framebuffer,
            Int2 swapchainSize)
        {
            ThrowIfNotInitialized();
            currentSwapchainSize = swapchainSize;

            commandbuffer.CmdBeginRenderPass(new RenderPassBeginInfo(
                renderPass: renderpass,
                framebuffer: framebuffer,
                renderArea: new Rect2D(x: 0, y: 0, width: swapchainSize.X, height: swapchainSize.Y),
                clearValues: new ClearValue(
                    new ClearColorValue(
                        new ColorF4(clearColor.R, clearColor.G, clearColor.B, clearColor.A)))
            ));

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

            Float4x4 viewMatrix = Float4x4.CreateTranslation((x: 0f, y: 0f, z: -2));
            Float4x4 projectionMatrix = Float4x4.CreatePerspectiveProjection(
                Frustum.CreateFromVerticalAngleAndAspect(
                    verticalAngle: FloatUtils.DegreesToRadians(45f),
                    aspect: (float)currentSwapchainSize.X / currentSwapchainSize.Y,
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

            transformationPool.Dispose();
            meshMemoryPool.Dispose();
            copier.Dispose();
            renderobjects.DisposeAll();
            renderpass.Dispose();
            descriptorPool.Dispose();
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
            //Dependency to wait on the framebuffer being loaded before we write to it
            var attachmentAvailableDependency = new SubpassDependency(
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
                        }
                    )
                },
                attachments: new [] { colorAttachment },
                dependencies: new [] { attachmentAvailableDependency }
            ));
        }

        private void ThrowIfNotInitialized()
        {
            if (!initialized)
                throw new Exception($"[{nameof(RenderScene)}] Not yet initialized");
        }
    }
}