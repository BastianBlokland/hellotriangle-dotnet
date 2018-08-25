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
        public readonly static Format ColorTargetFormat = Format.R8G8B8A8UNorm;
        public readonly static Format NormalTargetFormat = Format.R8G8B8A8SNorm;
        public readonly static Format AttributeTargetFormat = Format.R8G8B8A8UNorm;
        public readonly static Format DepthTargetFormat = Format.D32SFloat;
        public readonly static Format ShadowTargetFormat = Format.D32SFloat;
        public readonly static int ShadowTargetSize = 2048;
        public readonly static Float3 SunDirection = Float3.Normalize(new Float3(-1f, -.3f, -.1f));
        public readonly static float ShadowDistance = 85f;

        //Public properties
        public Camera Camera => camera;

        //Internal properties
        internal Device LogicalDevice => window.LogicalDevice;
        internal Memory.Pool MemoryPool => memoryPool;
        internal TransientExecutor Executor => executor;
        internal Memory.HostBuffer StagingBuffer => stagingBuffer;
        internal bool Dirty => dirty;

        //Data
        private readonly Camera camera;
        private readonly Window window;
        private readonly Logger logger;
        private readonly TransientExecutor executor;
        private readonly Memory.Pool memoryPool;
        private readonly Memory.HostBuffer stagingBuffer;
        private readonly Memory.HostBuffer sceneDataBuffer;
        private readonly Memory.HostBuffer cameraBuffer;
        private readonly Memory.HostBuffer shadowCameraBuffer;
        private readonly ShaderInputManager shaderInputManager;
        private readonly Renderer gBufferRenderer;
        private readonly Renderer shadowRenderer;
        private readonly Renderer compositionRenderer;
        private readonly AttributelessObject postRenderObject;
        private readonly List<IInternalRenderObject> objects = new List<IInternalRenderObject>();

        private Int2 swapchainSize;
        private DeviceSampler colorTarget;
        private DeviceSampler normalTarget;
        private DeviceSampler attributeTarget;
        private DeviceSampler depthTarget;
        private DeviceSampler shadowTarget;
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
            cameraBuffer = new Memory.HostBuffer(
                window.LogicalDevice, memoryPool, BufferUsages.UniformBuffer, CameraData.SIZE);
            shadowCameraBuffer = new Memory.HostBuffer(
                window.LogicalDevice, memoryPool, BufferUsages.UniformBuffer, CameraData.SIZE);
            shaderInputManager = new ShaderInputManager(window.LogicalDevice, logger);

            //Create renderers
            gBufferRenderer = new Renderer(window.LogicalDevice, shaderInputManager, logger);
            shadowRenderer = new Renderer(window.LogicalDevice, shaderInputManager, logger);
            compositionRenderer = new Renderer(window.LogicalDevice, shaderInputManager, logger);

            postRenderObject = new AttributelessObject(this, 
                vertexCount: 3, new [] { reflectionTexture });
            compositionRenderer.AddObject(postRenderObject, compositionVertProg, compositionFragProg);
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
            
            objects.Add(internalRenderObj);
            gBufferRenderer.AddObject(internalRenderObj, vertProg, fragProg, renderOrder,
                depthClamp: true, depthBias: true);

            if (shadowFragProg != null)
            {
                shadowRenderer.AddObject(internalRenderObj, vertProg, shadowFragProg, renderOrder,
                    depthClamp: true, depthBias: true);
            }
            dirty = true;
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            objects.DisposeAll();
            postRenderObject.Dispose();
            gBufferRenderer.Dispose();
            shadowRenderer.Dispose();
            compositionRenderer.Dispose();

            colorTarget?.Dispose();
            normalTarget?.Dispose();
            attributeTarget?.Dispose();
            depthTarget?.Dispose();
            shadowTarget?.Dispose();
            shaderInputManager.Dispose();
            stagingBuffer.Dispose();
            sceneDataBuffer.Dispose();
            cameraBuffer.Dispose();
            shadowCameraBuffer.Dispose();
            memoryPool.Dispose();
            executor.Dispose();
            disposed = true;
        }

        internal void CreateResources(Int2 swapchainSize, DeviceTexture[] swapchainTargets)
        {
            ThrowIfDisposed();

            //Save swapchain size
            this.swapchainSize = swapchainSize;

            //Setup g-buffer resources
            colorTarget?.Dispose();
            normalTarget?.Dispose();
            attributeTarget?.Dispose();
            depthTarget?.Dispose();

            colorTarget = new DeviceSampler(window.LogicalDevice, DeviceTexture.CreateColorTarget(
                swapchainSize, ColorTargetFormat, window.LogicalDevice, memoryPool, executor));

            normalTarget = new DeviceSampler(window.LogicalDevice, DeviceTexture.CreateColorTarget(
                swapchainSize, NormalTargetFormat, window.LogicalDevice, memoryPool, executor));

            attributeTarget = new DeviceSampler(window.LogicalDevice, DeviceTexture.CreateColorTarget(
                swapchainSize, AttributeTargetFormat, window.LogicalDevice, memoryPool, executor));

            depthTarget = new DeviceSampler(window.LogicalDevice, DeviceTexture.CreateDepthTarget(
                swapchainSize, DepthTargetFormat, window.LogicalDevice, memoryPool, executor));

            gBufferRenderer.BindGlobalInputs(new IShaderInput[] {
                cameraBuffer,
                sceneDataBuffer });

            gBufferRenderer.BindTargets(new [] {
                colorTarget.Texture,
                normalTarget.Texture,
                attributeTarget.Texture,
                depthTarget.Texture });

            gBufferRenderer.CreateResources();

            //Setup shadow resources
            shadowTarget?.Dispose();

            shadowTarget = new DeviceSampler(window.LogicalDevice, DeviceTexture.CreateDepthTarget(
                (ShadowTargetSize, ShadowTargetSize),
                ShadowTargetFormat,
                window.LogicalDevice,
                memoryPool,
                executor));

            shadowRenderer.BindGlobalInputs(new IShaderInput[] {
                shadowCameraBuffer,
                sceneDataBuffer });

            shadowRenderer.BindTargets(new [] {
                shadowTarget.Texture });

            shadowRenderer.CreateResources();

            //Setup composition resources
            compositionRenderer.SetOutputCount(swapchainTargets.Length);
            for (int i = 0; i < swapchainTargets.Length; i++)
                compositionRenderer.BindTargets(new [] { swapchainTargets[i] }, outputIndex: i);

            compositionRenderer.BindGlobalInputs(new IShaderInput[] {
                cameraBuffer, shadowCameraBuffer, sceneDataBuffer,
                colorTarget, normalTarget, attributeTarget, depthTarget, shadowTarget });

            compositionRenderer.CreateResources();
        }

        internal void Record(CommandBuffer commandbuffer, int swapchainImageIndex)
        {
            ThrowIfDisposed();

            gBufferRenderer.Record(commandbuffer);
            shadowRenderer.Record(commandbuffer);

            //Insert barrier to wait for the geometry rendering to be complete before we start
            //the composition pass
            commandbuffer.CmdPipelineBarrier(
                srcStageMask: PipelineStages.BottomOfPipe,
                dstStageMask: PipelineStages.FragmentShader);

            compositionRenderer.Record(commandbuffer, swapchainImageIndex);

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

            //Update the camera projection data
            float swapchainAspect = (float)swapchainSize.X / swapchainSize.Y;
            CameraData camData = CameraData.FromCamera(camera, swapchainAspect);
            cameraBuffer.Write(camData);

            //Calculate and update shadow projection
            Float4x4 shadowRotationMat = Float4x4.CreateRotationFromAxis(SunDirection, Float3.Forward);
            FloatBox shadowFrustum = GetShadowFrustum(
                camData.InverseViewProjectionMatrix, shadowRotationMat.Invert());

            //Then the values for the shadow projection can be derived from the shadow-frustum
            Float3 shadowCenter = shadowFrustum.Center;
            Float2 shadowSize = shadowFrustum.Size.XY;
            float shadowNearClip = -shadowFrustum.HalfSize.Z;
            float shadowFarClip = shadowFrustum.HalfSize.Z;

            //Calculate the matrices for the shadow projection
            Float4x4 shadowCameraMat = shadowRotationMat * Float4x4.CreateTranslation(shadowCenter);
            Float4x4 shadowViewMat = shadowCameraMat.Invert();
            Float4x4 shadowProjMat = Float4x4.CreateOrthographicProjection(
                shadowSize, shadowNearClip, shadowFarClip);
            Float4x4 shadowViewProj = shadowProjMat * shadowViewMat;

            //Calculate a rounding matrix to fix shadow 'shimmering' as objects constantly 'switch'
            //between pixels in the shadowmap
            float targetHalfSize = ShadowTargetSize / 2f;
            Float2 shadowOrigin = shadowViewProj.TransformPoint((0f, 0f, 0f)).XY * targetHalfSize;
            Float2 rounding = (shadowOrigin.Round() - shadowOrigin) / targetHalfSize;
            Float4x4 roundingMat = Float4x4.CreateTranslation(rounding.XY0);

            //Apply rounding
            shadowProjMat = roundingMat * shadowProjMat;
            shadowViewProj = roundingMat * shadowViewProj;

            //Update shadow projection data in the buffer
            shadowCameraBuffer.Write(new CameraData(
                shadowCameraMat,
                shadowViewMat,
                shadowProjMat,
                shadowViewProj,
                shadowViewProj.Invert(),
                shadowNearClip,
                shadowFarClip));
        }

        private FloatBox GetShadowFrustum(Float4x4 ndcToWorldMat, Float4x4 worldToLightMat)
        {
            //Frustum of the camera that will be covered by the shadow map in NDC space
            //Note: this covers the entire screen but only to a certain depth
            FloatBox shadowNDC = new FloatBox(
                min: (-1f, -1f, 0f),
                max: (1f, 1f, DepthUtils.LinearToDepth(
                    ShadowDistance, 
                    Camera.NEAR_CLIP_DISTANCE, 
                    Camera.FAR_CLIP_DISTANCE)));
            //Transform the ndc box to worldspace and then to light space
            return shadowNDC.Transform(worldToLightMat * ndcToWorldMat);
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(RenderScene)}] Allready disposed");
        }
    }
}