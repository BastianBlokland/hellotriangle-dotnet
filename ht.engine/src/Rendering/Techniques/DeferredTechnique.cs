using System;
using HT.Engine.Math;
using HT.Engine.Resources;
using HT.Engine.Utils;

using VulkanCore;

namespace HT.Engine.Rendering.Techniques
{
    internal sealed class DeferredTechnique : IDisposable
    {
        private readonly GBufferTechnique gbufferTechnique;
        private readonly ShadowTechnique shadowTechnique;
        private readonly BloomTechnique bloomTechnique;
        private readonly AmbientOcclusionTechnique aoTechnique;
        private readonly RenderScene scene;
        private readonly Renderer renderer;

        private readonly AttributelessObject renderObject;

        private bool disposed;

        internal DeferredTechnique(
            GBufferTechnique gbufferTechnique,
            ShadowTechnique shadowTechnique,
            BloomTechnique bloomTechnique,
            AmbientOcclusionTechnique aoTechnique,
            TextureInfo reflectionTexture,
            ShaderProgram compositionVertProg, ShaderProgram compositionFragProg,
            RenderScene scene,
            Logger logger = null)
        {
            if (gbufferTechnique == null)
                throw new NullReferenceException(nameof(gbufferTechnique));
            if (shadowTechnique == null)
                throw new NullReferenceException(nameof(shadowTechnique));
            if (bloomTechnique == null)
                throw new NullReferenceException(nameof(bloomTechnique));
            if (aoTechnique == null)
                throw new NullReferenceException(nameof(aoTechnique));
            if (reflectionTexture.Texture == null)
                throw new ArgumentNullException(nameof(reflectionTexture));
            if (compositionVertProg == null)
                throw new ArgumentNullException(nameof(compositionVertProg));
            if (compositionFragProg == null)
                throw new ArgumentNullException(nameof(compositionFragProg));
            if (scene == null)
                throw new NullReferenceException(nameof(scene));

            this.gbufferTechnique = gbufferTechnique;
            this.shadowTechnique = shadowTechnique;
            this.bloomTechnique = bloomTechnique;
            this.aoTechnique = aoTechnique;
            this.scene = scene;

            //Create renderer for rendering the composition effects
            renderer = new Renderer(scene.LogicalDevice, scene.InputManager, logger);

            //Add full-screen object for drawing the composition
            renderObject = new AttributelessObject(scene, 
                vertexCount: 3, new [] { reflectionTexture });
            renderer.AddObject(renderObject, compositionVertProg, compositionFragProg);
        }

        internal void CreateResources(DeviceTexture[] swapchain, IShaderInput sceneData)
        {
            ThrowIfDisposed();

            //Bind the output of the renderer to the swapchain
            renderer.SetOutputCount(swapchain.Length);
            for (int i = 0; i < swapchain.Length; i++)
                renderer.BindTargets(new [] { swapchain[i] }, outputIndex: i);

            //Bind all the inputs
            renderer.BindGlobalInputs(new IShaderInput[] {
                sceneData, gbufferTechnique.CameraOutput, shadowTechnique.CameraOutput,
                gbufferTechnique.ColorOutput,
                gbufferTechnique.NormalOutput,
                gbufferTechnique.AttributeOutput,
                gbufferTechnique.DepthOutput,
                shadowTechnique.ShadowOutput,
                bloomTechnique.BloomOutput,
                aoTechnique.AOOutput });

            //Tell the renderer to allocate its resources based on the data we've provided
            renderer.CreateResources(specialization: null);
        }

        internal void Record(CommandBuffer commandbuffer, int swapchainIndex)
        {
            ThrowIfDisposed();

            renderer.Record(commandbuffer, outputIndex: swapchainIndex);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            renderer.Dispose();
            renderObject.Dispose();

            disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(DeferredTechnique)}] Allready disposed");
        }
    }
}