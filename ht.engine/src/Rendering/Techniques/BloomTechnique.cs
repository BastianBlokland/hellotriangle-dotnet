using System;
using System.Diagnostics;

using HT.Engine.Math;
using HT.Engine.Resources;
using HT.Engine.Utils;

using VulkanCore;

namespace HT.Engine.Rendering.Techniques
{
    internal sealed class BloomTechnique : IDisposable
    {
        private readonly static Format bloomFormat = Format.R8G8B8A8UNorm;
        private readonly static int blurIterations = 2;
        private readonly static float blurSampleScale = 1.5f;

        //Properties
        internal IShaderInput BloomOutput => bloomSampler;

        //Data
        private readonly GBufferTechnique gbufferTechnique;
        private readonly RenderScene scene;
        private readonly Renderer renderer;

        private readonly AttributelessObject renderObject;
        private readonly GaussianBlurTechnique blurTechnique;

        //Target to render into
        private DeviceTexture bloomTarget;

        //Sampler for sampling the bloom target
        private DeviceSampler bloomSampler;

        private bool disposed;

        internal BloomTechnique(
            GBufferTechnique gbufferTechnique,
            ShaderProgram postVertProg, ShaderProgram bloomFragProg, ShaderProgram gaussBlurFragProg,
            RenderScene scene,
            Logger logger = null)
        {
            if (gbufferTechnique == null)
                throw new NullReferenceException(nameof(gbufferTechnique));
            if (postVertProg == null)
                throw new ArgumentNullException(nameof(postVertProg));
            if (bloomFragProg == null)
                throw new ArgumentNullException(nameof(bloomFragProg));
            if (gaussBlurFragProg == null)
                throw new ArgumentNullException(nameof(gaussBlurFragProg));
            if (scene == null)
                throw new NullReferenceException(nameof(scene));

            this.gbufferTechnique = gbufferTechnique;
            this.scene = scene;

            //Create renderer for rendering the bloom texture
            renderer = new Renderer(scene, logger);

            //Add full-screen object for drawing the composition
            renderObject = new AttributelessObject(scene, vertexCount: 3, new TextureInfo[0]);
            renderer.AddObject(renderObject, postVertProg, bloomFragProg, debugName: "fullscreen");

            //Create a BlurTechnique for blurring the bloom texture
            blurTechnique = new GaussianBlurTechnique(
                postVertProg,
                gaussBlurFragProg,
                blurIterations,
                blurSampleScale,
                scene,
                logger);
        }

        internal void CreateResources(Int2 swapchainSize)
        {
            ThrowIfDisposed();

            //Dispose of the old target
            bloomTarget?.Dispose();

            //Dispose of the old sampler
            bloomSampler?.Dispose();

            //Create the new render target
            bloomTarget = DeviceTexture.CreateColorTarget(swapchainSize, bloomFormat, scene);
            
            //Create sampler
            bloomSampler = new DeviceSampler(scene.LogicalDevice, bloomTarget, disposeTexture: false);

            //Bind inputs to the renderer
            renderer.BindGlobalInputs(new IShaderInput[] { gbufferTechnique.ColorOutput });

            //Bind the targets to the renderer
            renderer.BindTargets(new [] { bloomTarget });

            //Tell the renderer to allocate its resources based on the data we've provided
            renderer.CreateResources();

            //Initialize the blurTechnique, point it to the bloom-target
            blurTechnique.CreateResources(bloomTarget);
        }

        internal void Record(CommandBuffer commandbuffer)
        {
            ThrowIfDisposed();

            scene.BeginDebugMarker(commandbuffer, "Bloom");
            {
                renderer.Record(commandbuffer);

                //Insert barrier because bloom rendering needs to be done before we can blur it
                Renderer.InsertOutputReadBarrier(commandbuffer);

                blurTechnique.Record(commandbuffer);
            }
            scene.EndDebugMarker(commandbuffer);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            blurTechnique.Dispose();
            renderer.Dispose();
            renderObject.Dispose();
            bloomSampler?.Dispose();
            bloomTarget?.Dispose();

            disposed = true;
        }

        [Conditional("DEBUG")]
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(BloomTechnique)}] Allready disposed");
        }
    }
}