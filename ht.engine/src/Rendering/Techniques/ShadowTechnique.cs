using System;
using HT.Engine.Math;
using HT.Engine.Resources;
using HT.Engine.Utils;

using VulkanCore;

namespace HT.Engine.Rendering.Techniques
{
    internal sealed class ShadowTechnique : IDisposable
    {
        private readonly static int targetSize = 2048;
        private readonly static Format depthFormat = Format.D16UNorm;

        //Properties
        internal IShaderInput CameraInput => cameraBuffer;
        internal IShaderInput ShadowInput => depthSampler;

        //Data
        private readonly RenderScene scene;
        private readonly Renderer renderer;

        //Buffer for storing the camera transformations
        private readonly Memory.HostBuffer cameraBuffer;

        //Target to render into
        private float swapchainAspect;
        private DeviceTexture depthTarget;

        //Sampler for sampling shadow data
        private DeviceSampler depthSampler;

        private bool disposed;

        internal ShadowTechnique(
            RenderScene scene,
            Logger logger = null)
        {
            if (scene == null)
                throw new NullReferenceException(nameof(scene));
            this.scene = scene;

            //Create buffer for storing camera transformations
            cameraBuffer = new Memory.HostBuffer(
                scene.LogicalDevice, scene.MemoryPool, BufferUsages.UniformBuffer, CameraData.SIZE);

            //Create renderer for rendering into the g-buffer targets
            renderer = new Renderer(scene.LogicalDevice, scene.InputManager, logger);
        }

        internal void AddObject(
            IInternalRenderObject renderObject,
            ShaderProgram vertProg,
            ShaderProgram fragProg,
            int renderOrder = 0)
        {
            renderer.AddObject(renderObject, vertProg, fragProg, renderOrder,
                depthClamp: true, depthBias: true);
        }

        internal void CreateResources(Int2 swapchainSize, IShaderInput sceneData)
        {
            ThrowIfDisposed();

            //Dispose of the old target
            depthTarget?.Dispose();

            //Dispose of the old sampler
            depthSampler?.Dispose();

            //Create the new render target
            depthTarget = DeviceTexture.CreateDepthTarget(
                size: new Int2(targetSize, targetSize),
                depthFormat, scene.LogicalDevice, scene.MemoryPool, scene.Executor);
            //Create sampler
            depthSampler = new DeviceSampler(scene.LogicalDevice, depthTarget, disposeTexture: false);

            //Bind inputs to the renderer
            renderer.BindGlobalInputs(new IShaderInput[] { sceneData, cameraBuffer });

            //Bind the targets to the renderer
            renderer.BindTargets(new [] { depthTarget });

            //Tell the renderer to allocate its resources based on the data we've provided
            renderer.CreateResources();

            //Store the aspect of the swapchain, we need it later to calculate the shadow frustum
            swapchainAspect = (float)swapchainSize.X / swapchainSize.Y;
        }

        internal void Record(CommandBuffer commandbuffer)
        {
            ThrowIfDisposed();

            renderer.Record(commandbuffer);
        }

        internal void PreDraw(Float3 sunDirection, float shadowDistance)
        {
            //Get the 'normal' scene projections for current camera and aspect
            CameraData sceneCameraData = CameraData.FromCamera(scene.Camera, swapchainAspect);

            //Rotation from world to 'sun' direction
            Float4x4 rotationMatrix = Float4x4.CreateRotationFromAxis(sunDirection, Float3.Forward);

            //Calculate a frustum in 'lightspace' based on the frustum from the 'normal' camera
            FloatBox shadowFrustum = GetShadowFrustum(
                sceneCameraData.InverseViewProjectionMatrix,
                rotationMatrix.Invert(),
                shadowDistance);

            //Derive the projection values from the shadow-frustum
            Float3 shadowCenter = shadowFrustum.Center;
            Float2 shadowSize = shadowFrustum.Size.XY;
            float shadowNearClip = -shadowFrustum.HalfSize.Z;
            float shadowFarClip = shadowFrustum.HalfSize.Z;

            //Calculate the matrices for the shadow projection
            Float4x4 cameraMatrix = rotationMatrix * Float4x4.CreateTranslation(shadowCenter);
            Float4x4 viewMatrix = cameraMatrix.Invert();
            Float4x4 projectionMatrix = Float4x4.CreateOrthographicProjection(
                shadowSize, shadowNearClip, shadowFarClip);
            Float4x4 viewProjectionMatrix = projectionMatrix * viewMatrix;

            //Calculate a rounding matrix to fix shadow 'shimmering' as objects constantly 'switch'
            //between pixels in the shadowmap
            float targetHalfSize = targetSize / 2f;
            Float2 shadowOrigin = viewProjectionMatrix.TransformPoint((0f, 0f, 0f)).XY * targetHalfSize;
            Float2 rounding = (shadowOrigin.Round() - shadowOrigin) / targetHalfSize;
            Float4x4 roundingMat = Float4x4.CreateTranslation(rounding.XY0);

            //Apply rounding
            projectionMatrix = roundingMat * projectionMatrix;
            viewProjectionMatrix = roundingMat * viewProjectionMatrix;

            //Update shadow projection data in the buffer
            cameraBuffer.Write(new CameraData(
                cameraMatrix,
                viewMatrix,
                projectionMatrix,
                viewProjectionMatrix,
                viewProjectionMatrix.Invert(),
                shadowNearClip,
                shadowFarClip));
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            renderer.Dispose();
            cameraBuffer.Dispose();
            depthSampler?.Dispose();
            depthTarget?.Dispose();

            disposed = true;
        }

        private static FloatBox GetShadowFrustum(
            Float4x4 ndcToWorldMat,
            Float4x4 worldToLightMat,
            float shadowDistance)
        {
            //Frustum of the camera that will be covered by the shadow map in NDC space
            //Note: this covers the entire screen but only to a certain depth
            FloatBox shadowNDC = new FloatBox(
                min: (-1f, -1f, 0f),
                max: (1f, 1f, DepthUtils.LinearToDepth(
                    shadowDistance, 
                    Camera.NEAR_CLIP_DISTANCE, 
                    Camera.FAR_CLIP_DISTANCE)));
            //Transform the ndc box to worldspace and then to light space
            return shadowNDC.Transform(worldToLightMat * ndcToWorldMat);
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(ShadowTechnique)}] Allready disposed");
        }
    }
}