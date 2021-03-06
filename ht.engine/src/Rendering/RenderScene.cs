using System;
using System.Diagnostics;
using System.Collections.Generic;

using HT.Engine.Math;
using HT.Engine.Resources;
using HT.Engine.Utils;
using HT.Engine.Rendering.Techniques;

using VulkanCore;
using VulkanCore.Ext;

namespace HT.Engine.Rendering
{
    public sealed class RenderScene : IDisposable
    {
        private readonly static Float3 sunDirection = Float3.Normalize(new Float3(-1f, -.3f, -.1f));
        private readonly static float shadowDistance = 95f;

        //Public properties
        public Camera Camera => camera;

        //Internal properties
        internal int SwapchainCount => window.SwapchainCount;
        internal HostDevice HostDevice => window.HostDevice;
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
        private readonly BloomTechnique bloomTechnique;
        private readonly AmbientOcclusionTechnique aoTechnique;
        private readonly DeferredTechnique deferredTechnique;
        
        private readonly List<IInternalRenderObject> objects = new List<IInternalRenderObject>();

        private bool dirty;
        private bool disposed;
        
        public RenderScene(Window window,
            TextureInfo reflectionTexture,
            ShaderProgram postVertProg,
            ShaderProgram bloomFragProg,
            ShaderProgram aoFragProg,
            ShaderProgram gaussBlurFragProg,
            ShaderProgram boxBlurFragProg,
            ShaderProgram compositionFragProg,
            Logger logger = null)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));
            if (reflectionTexture.Texture == null)
                throw new ArgumentNullException(nameof(reflectionTexture));
            if (postVertProg == null)
                throw new ArgumentNullException(nameof(postVertProg));
            if (bloomFragProg == null)
                throw new ArgumentNullException(nameof(bloomFragProg));
            if (aoFragProg == null)
                throw new ArgumentNullException(nameof(aoFragProg));
            if (gaussBlurFragProg == null)
                throw new ArgumentNullException(nameof(gaussBlurFragProg));
            if (boxBlurFragProg == null)
                throw new ArgumentNullException(nameof(boxBlurFragProg));
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
                window.LogicalDevice, memoryPool, BufferUsages.UniformBuffer,
                size: SceneData.SIZE * window.SwapchainCount);
            inputManager = new ShaderInputManager(window.LogicalDevice, logger);

            //Create techniques
            gbufferTechnique = new GBufferTechnique(this, logger);
            shadowTechnique = new ShadowTechnique(this, logger);
            bloomTechnique = new BloomTechnique(
                gbufferTechnique, postVertProg, bloomFragProg, gaussBlurFragProg, this, logger);
            aoTechnique = new AmbientOcclusionTechnique(
                gbufferTechnique, postVertProg, aoFragProg, boxBlurFragProg, this, logger);
            deferredTechnique = new DeferredTechnique(
                gbufferTechnique, shadowTechnique, bloomTechnique, aoTechnique,
                reflectionTexture, postVertProg, compositionFragProg,
                this, logger);
        }

        public void AddObject(
            int renderOrder,
            IRenderObject renderObject,
            ShaderProgram vertProg,
            ShaderProgram fragProg,
            ShaderProgram shadowFragProg,
            string debugName = null)
        {
            IInternalRenderObject internalRenderObj = renderObject as IInternalRenderObject;
            if (internalRenderObj == null)
                throw new Exception(
                    $"[{nameof(RenderScene)}] Render objects have to be implemented at engine level");
            
            //Keep track of all objects
            objects.Add(internalRenderObj);

            //Add them to techniques for rendering
            gbufferTechnique.AddObject(internalRenderObj, vertProg, fragProg, renderOrder, debugName);
            if (shadowFragProg != null)
                shadowTechnique.AddObject(
                    internalRenderObj, vertProg, shadowFragProg, renderOrder, debugName);
            
            dirty = true;
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            objects.DisposeAll();
            deferredTechnique.Dispose();
            shadowTechnique.Dispose();
            bloomTechnique.Dispose();
            aoTechnique.Dispose();
            gbufferTechnique.Dispose();
            
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
            bloomTechnique.CreateResources(swapchainSize);
            aoTechnique.CreateResources(swapchainSize);
            deferredTechnique.CreateResources(swapchainTargets, sceneDataBuffer);
        }

        internal void Record(CommandBuffer commandbuffer, int swapchainIndex)
        {
            ThrowIfDisposed();

            BeginDebugMarker(commandbuffer, "RenderScene");
            {
                //First render the gbuffer and shadow targets
                gbufferTechnique.Record(commandbuffer, swapchainIndex);
                shadowTechnique.Record(commandbuffer, swapchainIndex);

                //Insert barrier because bloom and ao depend on gbuffer output
                Renderer.InsertOutputReadBarrier(commandbuffer);

                bloomTechnique.Record(commandbuffer);
                aoTechnique.Record(commandbuffer, swapchainIndex);

                //Insert barrier because deferred pass depends on all other outputs
                Renderer.InsertOutputReadBarrier(commandbuffer);

                deferredTechnique.Record(commandbuffer, swapchainIndex);
            }
            EndDebugMarker(commandbuffer);

            //All added / removed objects have been taking into account so we can unset the dirty flag
            dirty = false;
        }

        internal void PreDraw(FrameTracker tracker, int swapchainIndex)
        {
            //Update the scene data
            SceneData sceneData = new SceneData(
                tracker.FrameNumber,
                (float)tracker.ElapsedTime,
                tracker.DeltaTime);
            sceneDataBuffer.Write(sceneData, SceneData.SIZE * swapchainIndex);

            gbufferTechnique.PreDraw(swapchainIndex);
            shadowTechnique.PreDraw(swapchainIndex, sunDirection, shadowDistance);
        }

        [Conditional("DEBUG")]
        internal void BeginDebugMarker(CommandBuffer commandbuffer, string name)
        {
            if (window.DebugMarkerIsSupported)
                commandbuffer.CmdDebugMarkerBeginExt(new DebugMarkerMarkerInfoExt(name));
        }

        [Conditional("DEBUG")]
        internal void EndDebugMarker(CommandBuffer commandbuffer)
        {
             if (window.DebugMarkerIsSupported)
                commandbuffer.CmdDebugMarkerEndExt();
        }

        [Conditional("DEBUG")]
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(RenderScene)}] Allready disposed");
        }
    }
}