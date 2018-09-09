using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using HT.Engine.Math;
using HT.Engine.Resources;
using HT.Engine.Utils;

using VulkanCore;

namespace HT.Engine.Rendering.Techniques
{
    internal sealed class GaussianBlurTechnique : IDisposable
    {
        //Data
        private readonly int iterations;
        private readonly RenderScene scene;
        private readonly Renderer rendererHor;
        private readonly Renderer rendererVer;

        private readonly AttributelessObject renderObject;

        //Create a temp target. Ping-pongs from the provided target to this 'temp' target for each step
        private DeviceTexture targetB;

        //Samplers used for the ping-ponging
        private DeviceSampler samplerHor;
        private DeviceSampler samplerVer;

        private bool disposed;

        internal GaussianBlurTechnique(
            ShaderProgram postVertProg, ShaderProgram blurFragProg,
            int iterations, float sampleScale, RenderScene scene, Logger logger = null)
        {
            if (postVertProg == null)
                throw new ArgumentNullException(nameof(postVertProg));
            if (blurFragProg == null)
                throw new ArgumentNullException(nameof(blurFragProg));
            if (scene == null)
                throw new NullReferenceException(nameof(scene));

            this.iterations = iterations;
            this.scene = scene;

            //Create renderers (2 so we can ping-pong between two targets)
            rendererHor = new Renderer(scene, logger);
            rendererHor.AddSpecialization(true); //IsHorizontal
            rendererHor.AddSpecialization(sampleScale);

            rendererVer = new Renderer(scene, logger);
            rendererVer.AddSpecialization(false); //NOT IsHorizontal
            rendererVer.AddSpecialization(sampleScale);

            //Add a full-screen object to both renderers
            renderObject = new AttributelessObject(scene, vertexCount: 3, new TextureInfo[0]);
            rendererHor.AddObject(renderObject, postVertProg, blurFragProg, debugName: "horizontal");
            rendererVer.AddObject(renderObject, postVertProg, blurFragProg, debugName: "vertical");
        }

        internal void CreateResources(DeviceTexture blurTarget)
        {
            ThrowIfDisposed();

            //Dispose of the resources
            targetB?.Dispose();
            samplerHor?.Dispose();
            samplerVer?.Dispose();

            //Create a temp target (same format and size as the blur-target) so we can ping-point
            //between the original target and this temp target for each blur step
            targetB = DeviceTexture.CreateColorTarget(blurTarget.Size, blurTarget.Format, scene);
            
            //Create samplers
            samplerHor = new DeviceSampler(scene.LogicalDevice, blurTarget, disposeTexture: false);
            samplerVer = new DeviceSampler(scene.LogicalDevice, targetB, disposeTexture: false);

            //Bind the inputs
            rendererHor.BindGlobalInputs(new IShaderInput[] { samplerHor });
            rendererVer.BindGlobalInputs(new IShaderInput[] { samplerVer });

            //Bind the targets
            rendererHor.BindTargets(new [] { targetB });
            rendererVer.BindTargets(new [] { blurTarget });

            //Tell the renderers to allocate their resources based on the data we've provided
            rendererHor.CreateResources();
            rendererVer.CreateResources();
        }

        internal void Record(CommandBuffer commandbuffer)
        {
            ThrowIfDisposed();

            scene.BeginDebugMarker(commandbuffer, "GaussianBlur");
            {
                for (int i = 0; i < iterations; i++)
                {
                    if (i != 0)
                    {
                        //Insert barrier to wait for last blur 'step' to be done
                        Renderer.InsertOutputReadBarrier(commandbuffer);
                    }

                    rendererHor.Record(commandbuffer);

                    //Insert barrier to wait for rendererA is done before starting rendererB
                    Renderer.InsertOutputReadBarrier(commandbuffer);

                    rendererVer.Record(commandbuffer);
                }
            }
            scene.EndDebugMarker(commandbuffer);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            rendererHor.Dispose();
            rendererVer.Dispose();
            renderObject.Dispose();
            samplerVer?.Dispose();
            samplerHor?.Dispose();
            targetB?.Dispose();

            disposed = true;
        }

        [Conditional("DEBUG")]
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(GaussianBlurTechnique)}] Allready disposed");
        }
    }
}