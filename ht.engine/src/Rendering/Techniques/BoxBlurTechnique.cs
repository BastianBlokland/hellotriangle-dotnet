using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HT.Engine.Math;
using HT.Engine.Resources;
using HT.Engine.Utils;

using VulkanCore;

namespace HT.Engine.Rendering.Techniques
{
    internal sealed class BoxBlurTechnique : IDisposable
    {
        //Properties
        internal IShaderInput BlurOutput => outputSampler;

        //Data
        private readonly RenderScene scene;
        private readonly Renderer renderer;

        private readonly AttributelessObject renderObject;

        //Target and sampler used during the blur pass
        private DeviceTexture outputTarget;
        private DeviceSampler inputSampler;
        private DeviceSampler outputSampler;

        private bool disposed;

        internal BoxBlurTechnique(
            ShaderProgram postVertProg, ShaderProgram blurFragProg,
            int sampleRange, float sampleScale, RenderScene scene, Logger logger = null)
        {
            if (postVertProg == null)
                throw new ArgumentNullException(nameof(postVertProg));
            if (blurFragProg == null)
                throw new ArgumentNullException(nameof(blurFragProg));
            if (scene == null)
                throw new NullReferenceException(nameof(scene));

            this.scene = scene;

            //Setup renderer
            renderer = new Renderer(scene, logger);
            renderer.AddSpecialization(sampleRange);
            renderer.AddSpecialization(sampleScale);

            //Add a full-screen object to the renderer
            renderObject = new AttributelessObject(scene, vertexCount: 3, new TextureInfo[0]);
            renderer.AddObject(renderObject, postVertProg, blurFragProg, debugName: "fullscreen");
        }

        internal void CreateResources(DeviceTexture blurTarget)
        {
            ThrowIfDisposed();

            //Dispose of the resources
            outputTarget?.Dispose();
            inputSampler?.Dispose();
            outputSampler?.Dispose();

            //Create a output target (same format and size as the blur-target)
            outputTarget = DeviceTexture.CreateColorTarget(blurTarget.Size, blurTarget.Format, scene);
            
            //Create samplers
            inputSampler = new DeviceSampler(scene.LogicalDevice, blurTarget, disposeTexture: false);
            outputSampler = new DeviceSampler(scene.LogicalDevice, outputTarget, disposeTexture: false);

            //Bind the input
            renderer.BindGlobalInputs(new IShaderInput[] { inputSampler });

            //Bind the target
            renderer.BindTargets(new [] { outputTarget });

            //Tell the renderer to allocate its resources based on the data we've provided
            renderer.CreateResources();
        }

        internal void Record(CommandBuffer commandbuffer)
        {
            ThrowIfDisposed();

            scene.BeginDebugMarker(commandbuffer, "BoxBlur");
            {
                renderer.Record(commandbuffer);
            }
            scene.EndDebugMarker(commandbuffer);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            renderer.Dispose();
            renderObject.Dispose();
            inputSampler?.Dispose();
            outputSampler?.Dispose();
            outputTarget?.Dispose();

            disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(BoxBlurTechnique)}] Allready disposed");
        }
    }
}