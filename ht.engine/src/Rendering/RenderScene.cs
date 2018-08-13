using System;
using System.Collections.Generic;

using HT.Engine.Math;
using HT.Engine.Resources;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class RenderScene : IDisposable
    {
        private readonly static Format ColorTargetFormat = Format.R8G8B8A8UNorm;
        private readonly static Format NormalTargetFormat = Format.R8G8B8A8SNorm;
        private readonly static Format DepthTargetFormat = Format.D32SFloat;
        private readonly static Format ShadowTargetFormat = Format.D32SFloat;
        private readonly static int SHADOW_TARGET_SIZE = 1024;

        //Public properties
        public Camera Camera => camera;

        //Internal properties
        internal Device LogicalDevice => window.LogicalDevice;
        internal DescriptorManager DescriptorManager => descriptorManager;
        internal RenderPass GeometryRenderpass => geometryRenderpass;
        internal RenderPass ShadowRenderpass => shadowRenderpass;
        internal RenderPass CompositionRenderpass => compositionRenderpass;
        internal DeviceTexture ColorTarget => colorTarget;
        internal DeviceTexture DepthTarget => depthTarget;
        internal Memory.Pool MemoryPool => memoryPool;
        internal TransientExecutor Executor => executor;
        internal Memory.HostBuffer StagingBuffer => stagingBuffer;
        internal Memory.HostBuffer SceneDataBuffer => sceneDataBuffer;
        internal bool Dirty => dirty;

        //Data
        private readonly Camera camera;
        private readonly Window window;
        private readonly Byte4? clearColor;
        private readonly Logger logger;
        private readonly TransientExecutor executor;
        private readonly Memory.Pool memoryPool;
        private readonly Memory.HostBuffer stagingBuffer;
        private readonly Memory.HostBuffer sceneDataBuffer;
        private readonly DescriptorManager descriptorManager;
        private readonly List<IInternalRenderObject> renderObjects = new List<IInternalRenderObject>();
        private readonly List<PostProcessEffect> postEffects = new List<PostProcessEffect>();

        private RenderPass geometryRenderpass;
        private RenderPass shadowRenderpass;
        private RenderPass compositionRenderpass;
        private Framebuffer geometryFrameBuffer;
        private Framebuffer shadowFrameBuffer;
        private Framebuffer[] presentFrameBuffers;
        private Int2 swapchainSize;
        private DeviceTexture colorTarget;
        private DeviceTexture normalTarget;
        private DeviceTexture depthTarget;
        private DeviceTexture shadowTarget;
        private bool dirty;
        private bool disposed;
        
        public RenderScene(Window window, Byte4? clearColor,
            ShaderProgram compositionVertProg, ShaderProgram compositionFragProg,
            Logger logger = null)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));
            if (compositionVertProg == null)
                throw new ArgumentNullException(nameof(compositionVertProg));
            if (compositionFragProg == null)
                throw new ArgumentNullException(nameof(compositionFragProg));

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

            //Create the renderpasses
            geometryRenderpass =
                CreateGeometryRenderPass(window.LogicalDevice, clearColor);
            shadowRenderpass = 
                CreateShadowRenderPass(window.LogicalDevice); 
            compositionRenderpass = 
                CreateCompositionRenderpass(window.LogicalDevice, window.SurfaceFormat);

            //Setup composition post effect
            postEffects.Add(new PostProcessEffect(this, compositionVertProg, compositionFragProg));
        }

        public void AddObject(IRenderObject renderObject)
        {
            if (!(renderObject is IInternalRenderObject))
                throw new Exception(
                    $"[{nameof(RenderScene)}] Render objects have to be implemented at engine level");
            renderObjects.Add(renderObject as IInternalRenderObject);
            dirty = true;
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            renderObjects.DisposeAll();
            postEffects.DisposeAll();
            geometryRenderpass.Dispose();
            shadowRenderpass.Dispose();
            compositionRenderpass.Dispose();
            colorTarget?.Dispose();
            normalTarget?.Dispose();
            depthTarget?.Dispose();
            shadowTarget?.Dispose();
            geometryFrameBuffer?.Dispose();
            shadowFrameBuffer?.Dispose();
            presentFrameBuffers?.DisposeAll();
            descriptorManager.Dispose();
            stagingBuffer.Dispose();
            sceneDataBuffer.Dispose();
            memoryPool.Dispose();
            executor.Dispose();
            disposed = true;
        }

        internal void CreateResources(Int2 swapchainSize, ImageView[] swapchainImages)
        {
            ThrowIfDisposed();

            //Save swapchain size
            this.swapchainSize = swapchainSize;

            //Dispose of the old color and depth targets
            colorTarget?.Dispose();
            normalTarget?.Dispose();
            depthTarget?.Dispose();
            shadowTarget?.Dispose();
            //Dispose of old framebuffers
            geometryFrameBuffer?.Dispose();
            shadowFrameBuffer?.Dispose();
            presentFrameBuffers?.DisposeAll();

            //Create new rendertargets
            colorTarget = DeviceTexture.CreateColorTarget(
                swapchainSize, ColorTargetFormat,
                window.LogicalDevice, memoryPool, executor, allowSampling: true);
            normalTarget = DeviceTexture.CreateColorTarget(
                swapchainSize, NormalTargetFormat,
                window.LogicalDevice, memoryPool, executor, allowSampling: true);
            depthTarget = DeviceTexture.CreateDepthTarget(
                swapchainSize, DepthTargetFormat,
                window.LogicalDevice, memoryPool, executor, allowSampling: true);
            shadowTarget = DeviceTexture.CreateDepthTarget(
                (SHADOW_TARGET_SIZE, SHADOW_TARGET_SIZE), ShadowTargetFormat,
                window.LogicalDevice, memoryPool, executor, allowSampling: true);

            //Create geometry framebuffer
            geometryFrameBuffer = geometryRenderpass.CreateFramebuffer(new FramebufferCreateInfo(
                attachments: new [] { colorTarget.View, normalTarget.View, depthTarget.View },
                width: swapchainSize.X,
                height: swapchainSize.Y));

            //Create shadow framebuffer
            shadowFrameBuffer = shadowRenderpass.CreateFramebuffer(new FramebufferCreateInfo(
                attachments: new [] { shadowTarget.View },
                width: SHADOW_TARGET_SIZE,
                height: SHADOW_TARGET_SIZE));

            //Create present framebuffers (need to create 1 for each swapchain image)
            presentFrameBuffers = new Framebuffer[swapchainImages.Length];
            for (int i = 0; i < presentFrameBuffers.Length; i++)
                presentFrameBuffers[i] = compositionRenderpass.CreateFramebuffer(new FramebufferCreateInfo(
                    attachments: new [] { swapchainImages[i] },
                    width: swapchainSize.X,
                    height: swapchainSize.Y));

            //Give the rendertargets to the post-effects so they can use them as inputs
            for (int i = 0; i < postEffects.Count; i++)
                postEffects[i].BindSceneTargets(colorTarget, normalTarget, depthTarget, shadowTarget);

            //All added / removed objects have been taking into account so we can unset the dirty flag
            dirty = false;
        }

        internal void Record(CommandBuffer commandbuffer, int swapchainImageIndex)
        {
            ThrowIfDisposed();

            //Sort the objects based on render-order
            renderObjects.Sort(CompareRenderOrder);

            //Render the g-buffers
            RecordGeometryRenderPass(commandbuffer);
            //Render the shadow buffer
            RecordShadowRenderPass(commandbuffer);

            //Insert barrier to wait for the geometry rendering to be complete before we start
            //the composition pass
            commandbuffer.CmdPipelineBarrier(
                srcStageMask: PipelineStages.BottomOfPipe,
                dstStageMask: PipelineStages.FragmentShader);

            RecordCompositionRenderPass(commandbuffer, swapchainImageIndex);
        }

        internal void PreDraw(FrameTracker tracker)
        {
            //Update the scene data
            float aspect = (float)swapchainSize.X / swapchainSize.Y;
            SceneData sceneData = new SceneData(
                camera.Transformation,
                camera.GetProjection(aspect),
                Camera.NEAR_CLIP_DISTANCE,
                Camera.FAR_CLIP_DISTANCE,
                swapchainSize,
                tracker.FrameNumber,
                (float)tracker.ElapsedTime,
                tracker.DeltaTime);
            sceneDataBuffer.Write(sceneData);
        }

        private void RecordGeometryRenderPass(CommandBuffer commandbuffer)
        {
            Float4 normClearColor = clearColor == null ? (0f, 0f, 0f, 1f) : clearColor.Value.Normalized;
            commandbuffer.CmdBeginRenderPass(new RenderPassBeginInfo(
                renderPass: geometryRenderpass,
                framebuffer: geometryFrameBuffer,
                renderArea: new Rect2D(x: 0, y: 0, width: swapchainSize.X, height: swapchainSize.Y),
                clearValues: new []
                {
                    //Color target
                    new ClearValue(new ClearColorValue(new ColorF4(
                        normClearColor.R, normClearColor.G, normClearColor.B, normClearColor.A))),
                    //Normal target
                    new ClearValue(new ClearColorValue(ColorF4.Zero)),
                    //Depth target
                    new ClearValue(new ClearDepthStencilValue(depth: 1f, stencil: 0))
                }));
            SetViewport(commandbuffer, swapchainSize);

            //Record all individual objects
            for (int i = 0; i < renderObjects.Count; i++)
                renderObjects[i].Record(commandbuffer, shadowPass: false);

            commandbuffer.CmdEndRenderPass();
        }

        private void RecordShadowRenderPass(CommandBuffer commandbuffer)
        {
            commandbuffer.CmdBeginRenderPass(new RenderPassBeginInfo(
                renderPass: shadowRenderpass,
                framebuffer: shadowFrameBuffer,
                renderArea: new Rect2D(0, 0, SHADOW_TARGET_SIZE, SHADOW_TARGET_SIZE),
                clearValues: new []
                {
                    //Depth target
                    new ClearValue(new ClearDepthStencilValue(depth: 1f, stencil: 0))
                }));
            SetViewport(commandbuffer, swapchainSize);

            //Record all individual objects
            for (int i = 0; i < renderObjects.Count; i++)
                renderObjects[i].Record(commandbuffer, shadowPass: true);

            commandbuffer.CmdEndRenderPass();
        }

        private void RecordCompositionRenderPass(CommandBuffer commandbuffer, int swapchainImageIndex)
        {
            commandbuffer.CmdBeginRenderPass(new RenderPassBeginInfo(
                renderPass: compositionRenderpass,
                framebuffer: presentFrameBuffers[swapchainImageIndex],
                renderArea: new Rect2D(x: 0, y: 0, width: swapchainSize.X, height: swapchainSize.Y),
                clearValues: new [] { new ClearValue() }));
            SetViewport(commandbuffer, swapchainSize);

            for (int i = 0; i < postEffects.Count; i++)
                postEffects[i].Record(commandbuffer);
            
            commandbuffer.CmdEndRenderPass();
        }

        private static RenderPass CreateGeometryRenderPass(Device logicalDevice, Byte4? clearColor)
        {
            //Description of our color target (output)
            var colorAttachment = new AttachmentDescription(
                flags: AttachmentDescriptions.MayAlias,
                format: ColorTargetFormat,
                samples: SampleCounts.Count1,
                loadOp: clearColor == null ? AttachmentLoadOp.DontCare : AttachmentLoadOp.Clear,
                storeOp: AttachmentStoreOp.Store,
                stencilLoadOp: AttachmentLoadOp.DontCare,
                stencilStoreOp: AttachmentStoreOp.DontCare,
                initialLayout: ImageLayout.Undefined,
                finalLayout: ImageLayout.ShaderReadOnlyOptimal
            );
            //Description of our normal target (output)
            var normalAttachment = new AttachmentDescription(
                flags: AttachmentDescriptions.MayAlias,
                format: NormalTargetFormat,
                samples: SampleCounts.Count1,
                loadOp: AttachmentLoadOp.Clear,
                storeOp: AttachmentStoreOp.Store,
                stencilLoadOp: AttachmentLoadOp.DontCare,
                stencilStoreOp: AttachmentStoreOp.DontCare,
                initialLayout: ImageLayout.Undefined,
                finalLayout: ImageLayout.ShaderReadOnlyOptimal
            );
            //Description of our depth target (output)
            var depthAttachment = new AttachmentDescription(
                flags: AttachmentDescriptions.MayAlias,
                format: DepthTargetFormat,
                samples: SampleCounts.Count1,
                loadOp: AttachmentLoadOp.Clear,
                storeOp: AttachmentStoreOp.Store,
                stencilLoadOp: AttachmentLoadOp.DontCare,
                stencilStoreOp: AttachmentStoreOp.DontCare,
                initialLayout: ImageLayout.Undefined,
                finalLayout: ImageLayout.ShaderReadOnlyOptimal
            );
            //Dependency at the beginning to transition the attachments to the proper layout
            var beginTransitionDependency = new SubpassDependency(
                srcSubpass: Constant.SubpassExternal, 
                dstSubpass: 0, //Our subpass
                srcStageMask: PipelineStages.BottomOfPipe,
                srcAccessMask: Accesses.MemoryRead,
                dstStageMask: PipelineStages.ColorAttachmentOutput,
                dstAccessMask: Accesses.ColorAttachmentWrite,
                dependencyFlags: Dependencies.ByRegion
            );
            //Dependency at the end to transition the attachments to the final layout
            var endTransitionDependency = new SubpassDependency(
                srcSubpass: 0, //Our subpass
                dstSubpass: Constant.SubpassExternal, 
                srcStageMask: PipelineStages.ColorAttachmentOutput,
                srcAccessMask: Accesses.ColorAttachmentWrite,
                dstStageMask: PipelineStages.BottomOfPipe,
                dstAccessMask: Accesses.MemoryRead,
                dependencyFlags: Dependencies.ByRegion
            );
            return logicalDevice.CreateRenderPass(new RenderPassCreateInfo(
                subpasses: new [] 
                {
                    new SubpassDescription
                    (
                        flags: SubpassDescriptionFlags.None,
                        colorAttachments: new []
                        {
                            //Color attachment
                            new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal),
                            new AttachmentReference(1, ImageLayout.ColorAttachmentOptimal)
                        },
                        //Depth attachment
                        depthStencilAttachment:
                            new AttachmentReference(2, ImageLayout.DepthStencilAttachmentOptimal)
                    )
                },
                attachments: new [] { colorAttachment, normalAttachment, depthAttachment },
                dependencies: new [] { beginTransitionDependency, endTransitionDependency } 
            ));
        }

        private static RenderPass CreateShadowRenderPass(Device logicalDevice)
        {
            //Description of our shadow depth target (output)
            var depthAttachment = new AttachmentDescription(
                flags: AttachmentDescriptions.MayAlias,
                format: ShadowTargetFormat,
                samples: SampleCounts.Count1,
                loadOp: AttachmentLoadOp.Clear,
                storeOp: AttachmentStoreOp.Store,
                stencilLoadOp: AttachmentLoadOp.DontCare,
                stencilStoreOp: AttachmentStoreOp.DontCare,
                initialLayout: ImageLayout.Undefined,
                finalLayout: ImageLayout.ShaderReadOnlyOptimal
            );
            //Dependency at the beginning to transition the attachments to the proper layout
            var beginTransitionDependency = new SubpassDependency(
                srcSubpass: Constant.SubpassExternal, 
                dstSubpass: 0, //Our subpass
                srcStageMask: PipelineStages.BottomOfPipe,
                srcAccessMask: Accesses.MemoryRead,
                dstStageMask: PipelineStages.ColorAttachmentOutput,
                dstAccessMask: Accesses.ColorAttachmentWrite,
                dependencyFlags: Dependencies.ByRegion
            );
            //Dependency at the end to transition the attachments to the final layout
            var endTransitionDependency = new SubpassDependency(
                srcSubpass: 0, //Our subpass
                dstSubpass: Constant.SubpassExternal, 
                srcStageMask: PipelineStages.ColorAttachmentOutput,
                srcAccessMask: Accesses.ColorAttachmentWrite,
                dstStageMask: PipelineStages.BottomOfPipe,
                dstAccessMask: Accesses.MemoryRead,
                dependencyFlags: Dependencies.ByRegion
            );
            return logicalDevice.CreateRenderPass(new RenderPassCreateInfo(
                subpasses: new [] 
                {
                    new SubpassDescription
                    (
                        flags: SubpassDescriptionFlags.None,
                        colorAttachments: new AttachmentReference[0],
                        //Depth attachment
                        depthStencilAttachment:
                            new AttachmentReference(0, ImageLayout.DepthStencilAttachmentOptimal)
                    )
                },
                attachments: new [] { depthAttachment },
                dependencies: new [] { beginTransitionDependency, endTransitionDependency } 
            ));
        }

        private static RenderPass CreateCompositionRenderpass(Device logicalDevice, Format surfaceFormat)
        {
            //Description of our frame-buffer attachment
            var frameBufferAttachment = new AttachmentDescription(
                flags: AttachmentDescriptions.MayAlias,
                format: surfaceFormat,
                samples: SampleCounts.Count1,
                loadOp: AttachmentLoadOp.DontCare,
                storeOp: AttachmentStoreOp.Store,
                stencilLoadOp: AttachmentLoadOp.DontCare,
                stencilStoreOp: AttachmentStoreOp.DontCare,
                initialLayout: ImageLayout.Undefined,
                finalLayout: ImageLayout.PresentSrcKhr
            );
            //Dependency at the beginning to transition the attachments to the proper layout
            var beginTransitionDependency = new SubpassDependency(
                srcSubpass: Constant.SubpassExternal, 
                dstSubpass: 0, //Our subpass
                srcStageMask: PipelineStages.BottomOfPipe,
                srcAccessMask: Accesses.MemoryRead,
                dstStageMask: PipelineStages.ColorAttachmentOutput,
                dstAccessMask: Accesses.ColorAttachmentWrite,
                dependencyFlags: Dependencies.ByRegion
            );
            //Dependency at the end to transition the attachments to the final layout
            var endTransitionDependency = new SubpassDependency(
                srcSubpass: 0, //Our subpass
                dstSubpass: Constant.SubpassExternal, 
                srcStageMask: PipelineStages.ColorAttachmentOutput,
                srcAccessMask: Accesses.ColorAttachmentWrite,
                dstStageMask: PipelineStages.BottomOfPipe,
                dstAccessMask: Accesses.MemoryRead,
                dependencyFlags: Dependencies.ByRegion
            );
            return logicalDevice.CreateRenderPass(new RenderPassCreateInfo(
                subpasses: new [] 
                {
                    new SubpassDescription
                    (
                        flags: SubpassDescriptionFlags.None,
                        colorAttachments: new []
                        {
                            //Frame-buffer attachment
                            new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal)
                        }
                    ),
                },
                attachments: new [] { frameBufferAttachment },
                dependencies: new [] { beginTransitionDependency, endTransitionDependency }
            ));
        }

        private static int CompareRenderOrder(IInternalRenderObject a, IInternalRenderObject b)
            => a.RenderOrder.CompareTo(b.RenderOrder);

        private static void SetViewport(CommandBuffer commandbuffer, Int2 size)
        {
            //Set viewport and scissor-rect dynamically to avoid the pipelines depending on
            //swapchain size (and thus having to be recreated on resize)
            commandbuffer.CmdSetViewport(
                new Viewport(
                    x: 0f, y: 0f, width: size.X, height: size.Y,
                    minDepth: 0f, maxDepth: 1f));
            commandbuffer.CmdSetScissor(
                new Rect2D(x: 0, y: 0, width: size.X, height: size.Y));
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(RenderScene)}] Allready disposed");
        }
    }
}