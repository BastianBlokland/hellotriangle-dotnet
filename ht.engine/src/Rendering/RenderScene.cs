using System;
using System.Collections.Generic;

using HT.Engine.Math;
using HT.Engine.Resources;
using HT.Engine.Utils;
using HT.Engine.Rendering.Techniques;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class RenderScene : IDisposable
    {
        private readonly static Float3 sunDirection = Float3.Normalize(new Float3(-1f, -.3f, -.1f));
        private readonly static float shadowDistance = 85f;

        //Public properties
        public Camera Camera => camera;

        //Internal properties
        internal Device LogicalDevice => window.LogicalDevice;
        internal Memory.Pool MemoryPool => memoryPool;
        internal TransientExecutor Executor => executor;
        internal Memory.HostBuffer StagingBuffer => stagingBuffer;
        internal ShaderInputManager InputManager => inputManager;
        internal bool Dirty => dirty;

        //Data
        private readonly Camera camera;
        private readonly Window window;
        private readonly Logger logger;
        private readonly TransientExecutor executor;
        private readonly Memory.Pool memoryPool;
        private readonly Memory.HostBuffer stagingBuffer;
        private readonly Memory.HostBuffer sceneDataBuffer;
        private readonly ShaderInputManager inputManager;
        
        private readonly GBufferTechnique gbufferTechnique;
        private readonly ShadowTechnique shadowTechnique;
        private readonly DeferredTechnique deferredTechnique;
        
        private readonly List<IInternalRenderObject> objects = new List<IInternalRenderObject>();

        private bool dirty;
        private bool disposed;
        
        public RenderScene(Window window,
            TextureInfo reflectionTexture,
            ShaderProgram compositionVertProg, ShaderProgram compositionFragProg,
            Logger logger = null)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));
            if (reflectionTexture.Texture == null)
                throw new ArgumentNullException(nameof(reflectionTexture));
            if (compositionVertProg == null)
                throw new ArgumentNullException(nameof(compositionVertProg));
            if (compositionFragProg == null)
                throw new ArgumentNullException(nameof(compositionFragProg));

            this.window = window;
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
                window.LogicalDevice, memoryPool, BufferUsages.UniformBuffer, SceneData.SIZE);
            inputManager = new ShaderInputManager(window.LogicalDevice, logger);

            //Create techniques
            gbufferTechnique = new GBufferTechnique(this, logger);
            shadowTechnique = new ShadowTechnique(this, logger);
            deferredTechnique = new DeferredTechnique(
                gbufferTechnique, shadowTechnique,
                reflectionTexture, compositionVertProg, compositionFragProg,
                this, logger);
        }

        public void AddObject(
            int renderOrder,
            IRenderObject renderObject,
            ShaderProgram vertProg,
            ShaderProgram fragProg,
            ShaderProgram shadowFragProg)
        {
            IInternalRenderObject internalRenderObj = renderObject as IInternalRenderObject;
            if (internalRenderObj == null)
                throw new Exception(
                    $"[{nameof(RenderScene)}] Render objects have to be implemented at engine level");
            
            //Keep track of all objects
            objects.Add(internalRenderObj);

            //Add them to techniques for rendering
            gbufferTechnique.AddObject(internalRenderObj, vertProg, fragProg, renderOrder);
            if (shadowFragProg != null)
                shadowTechnique.AddObject(internalRenderObj, vertProg, shadowFragProg, renderOrder);
            
            dirty = true;
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            objects.DisposeAll();
            deferredTechnique.Dispose();
            gbufferTechnique.Dispose();
            shadowTechnique.Dispose();
            
            inputManager.Dispose();
            stagingBuffer.Dispose();
            sceneDataBuffer.Dispose();
            memoryPool.Dispose();
            executor.Dispose();
            disposed = true;
        }

        internal void CreateResources(Int2 swapchainSize, DeviceTexture[] swapchainTargets)
        {
            ThrowIfDisposed();

            gbufferTechnique.CreateResources(swapchainSize, sceneDataBuffer);

            shadowTechnique.CreateResources(swapchainSize, sceneDataBuffer);

            deferredTechnique.CreateResources(swapchainTargets, sceneDataBuffer);
        }

        internal void Record(CommandBuffer commandbuffer, int swapchainImageIndex)
        {
            ThrowIfDisposed();

            //First render the gbuffer and shadow targets
            gbufferTechnique.Record(commandbuffer);
            shadowTechnique.Record(commandbuffer);

            //Insert barrier to wait for the gbuffer and shadow rendering to be complete
            commandbuffer.CmdPipelineBarrier(
                srcStageMask: PipelineStages.BottomOfPipe,
                dstStageMask: PipelineStages.FragmentShader);

            deferredTechnique.Record(commandbuffer, swapchainImageIndex);

            //All added / removed objects have been taking into account so we can unset the dirty flag
            dirty = false;
        }

        internal void PreDraw(FrameTracker tracker)
        {
            //Update the scene data
            SceneData sceneData = new SceneData(
                tracker.FrameNumber,
                (float)tracker.ElapsedTime,
                tracker.DeltaTime);
            sceneDataBuffer.Write(sceneData);

            gbufferTechnique.PreDraw();
            shadowTechnique.PreDraw(sunDirection, shadowDistance);
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(RenderScene)}] Allready disposed");
        }
    }
}