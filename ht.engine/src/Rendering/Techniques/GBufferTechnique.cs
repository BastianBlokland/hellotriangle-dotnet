using System;
using HT.Engine.Math;
using HT.Engine.Resources;
using HT.Engine.Utils;

using VulkanCore;

namespace HT.Engine.Rendering.Techniques
{
    internal sealed class GBufferTechnique : IDisposable
    {
        private readonly static Format colorFormat = Format.R8G8B8A8UNorm;
        private readonly static Format normalFormat = Format.R8G8B8A8SNorm;
        private readonly static Format attributeFormat = Format.R8G8B8A8UNorm;
        private readonly static Format depthFormat = Format.D16UNorm;

        //Properties
        internal IShaderInput CameraInput => cameraBuffer;
        internal IShaderInput ColorInput => colorSampler;
        internal IShaderInput NormalInput => normalSampler;
        internal IShaderInput AttributeInput => attributeSampler;
        internal IShaderInput DepthInput => depthSampler;

        //Data
        private readonly RenderScene scene;
        private readonly Renderer renderer;

        //Buffer for storing the camera transformations
        private readonly Memory.HostBuffer cameraBuffer;

        //G-Buffer targets to render into
        private DeviceTexture colorTarget;
        private DeviceTexture normalTarget;
        private DeviceTexture attributeTarget;
        private DeviceTexture depthTarget;

        //Samplers for sampling the g-buffer targets
        private DeviceSampler colorSampler;
        private DeviceSampler normalSampler;
        private DeviceSampler attributeSampler;
        private DeviceSampler depthSampler;

        private bool disposed;

        internal GBufferTechnique(
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
            renderer.AddObject(renderObject, vertProg, fragProg, renderOrder);
        }

        internal void CreateResources(Int2 swapchainSize, IShaderInput sceneData)
        {
            ThrowIfDisposed();

            //Dispose of the old targets
            colorTarget?.Dispose();
            normalTarget?.Dispose();
            attributeTarget?.Dispose();
            depthTarget?.Dispose();

            //Dispose of the old samplers
            colorSampler?.Dispose();
            normalSampler?.Dispose();
            attributeSampler?.Dispose();
            depthSampler?.Dispose();

            //Create the new render targets
            colorTarget = DeviceTexture.CreateColorTarget(
                swapchainSize, colorFormat, scene.LogicalDevice, scene.MemoryPool, scene.Executor);
            normalTarget = DeviceTexture.CreateColorTarget(
                swapchainSize, normalFormat, scene.LogicalDevice, scene.MemoryPool, scene.Executor);
            attributeTarget = DeviceTexture.CreateColorTarget(
                swapchainSize, attributeFormat, scene.LogicalDevice, scene.MemoryPool, scene.Executor);
            depthTarget = DeviceTexture.CreateDepthTarget(
                swapchainSize, depthFormat, scene.LogicalDevice, scene.MemoryPool, scene.Executor);

            //Create samplers for the targets
            colorSampler = new DeviceSampler(scene.LogicalDevice, colorTarget, disposeTexture: false);
            normalSampler = new DeviceSampler(scene.LogicalDevice, normalTarget, disposeTexture: false);
            attributeSampler = new DeviceSampler(scene.LogicalDevice, attributeTarget, disposeTexture: false);
            depthSampler = new DeviceSampler(scene.LogicalDevice, depthTarget, disposeTexture: false);

            //Bind inputs to the renderer
            renderer.BindGlobalInputs(new IShaderInput[] { sceneData, cameraBuffer });

            //Bind the targets to the renderer
            renderer.BindTargets(new [] { colorTarget, normalTarget, attributeTarget, depthTarget });

            //Tell the renderer to allocate its resources based on the data we've provided
            renderer.CreateResources();
        }

        internal void Record(CommandBuffer commandbuffer)
        {
            ThrowIfDisposed();

            renderer.Record(commandbuffer);
        }

        internal void PreDraw()
        {
            float aspect = (float)colorTarget.Size.X / colorTarget.Size.Y;
            var cameraData = CameraData.FromCamera(scene.Camera, aspect);
            cameraBuffer.Write(cameraData);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            renderer.Dispose();
            cameraBuffer.Dispose();

            colorSampler?.Dispose();
            normalSampler?.Dispose();
            attributeSampler?.Dispose();
            depthSampler?.Dispose();

            colorTarget?.Dispose();
            normalTarget?.Dispose();
            attributeTarget?.Dispose();
            depthTarget?.Dispose();

            disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(GBufferTechnique)}] Allready disposed");
        }
    }
}