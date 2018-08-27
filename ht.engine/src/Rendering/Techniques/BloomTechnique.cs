using System;
using HT.Engine.Math;
using HT.Engine.Resources;
using HT.Engine.Utils;

using VulkanCore;

namespace HT.Engine.Rendering.Techniques
{
    internal sealed class BloomTechnique : IDisposable
    {
        private readonly static Format bloomFormat = Format.R8G8B8A8UNorm;

        //Properties
        internal IShaderInput BloomInput => bloomSampler;

        //Data
        private readonly GBufferTechnique gbufferTechnique;
        private readonly RenderScene scene;
        private readonly Renderer renderer;

        private readonly AttributelessObject renderObject;

        //Target to render into
        private DeviceTexture bloomTarget;

        //Sampler for sampling shadow data
        private DeviceSampler bloomSampler;

        private bool disposed;

        internal BloomTechnique(
            GBufferTechnique gbufferTechnique,
            ShaderProgram postVertProg, ShaderProgram bloomFragProg,
            RenderScene scene,
            Logger logger = null)
        {
            if (gbufferTechnique == null)
                throw new NullReferenceException(nameof(gbufferTechnique));
            if (postVertProg == null)
                throw new ArgumentNullException(nameof(postVertProg));
            if (bloomFragProg == null)
                throw new ArgumentNullException(nameof(bloomFragProg));
            if (scene == null)
                throw new NullReferenceException(nameof(scene));

            this.gbufferTechnique = gbufferTechnique;
            this.scene = scene;

            //Create renderer for rendering the composition effects
            renderer = new Renderer(scene.LogicalDevice, scene.InputManager, logger);

            //Add full-screen object for drawing the composition
            renderObject = new AttributelessObject(scene, vertexCount: 3, new TextureInfo[0]);
            renderer.AddObject(renderObject, postVertProg, bloomFragProg);
        }

        internal void CreateResources(Int2 swapchainSize)
        {
            ThrowIfDisposed();

            //Dispose of the old target
            bloomTarget?.Dispose();

            //Dispose of the old sampler
            bloomSampler?.Dispose();

            //Create the new render target
            bloomTarget = DeviceTexture.CreateColorTarget(swapchainSize, bloomFormat,
                scene.LogicalDevice, scene.MemoryPool, scene.Executor);
            //Create sampler
            bloomSampler = new DeviceSampler(scene.LogicalDevice, bloomTarget, disposeTexture: false);

            //Bind inputs to the renderer
            renderer.BindGlobalInputs(new IShaderInput[] { gbufferTechnique.ColorInput });

            //Bind the targets to the renderer
            renderer.BindTargets(new [] { bloomTarget });

            //Tell the renderer to allocate its resources based on the data we've provided
            renderer.CreateResources(specialization: null);
        }

        internal void Record(CommandBuffer commandbuffer)
        {
            ThrowIfDisposed();

            renderer.Record(commandbuffer);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            renderer.Dispose();
            renderObject.Dispose();
            bloomSampler?.Dispose();
            bloomTarget?.Dispose();

            disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(BloomTechnique)}] Allready disposed");
        }
    }
}