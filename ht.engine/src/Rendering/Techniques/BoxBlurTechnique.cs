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
        private class SpecializationProvider : ISpecializationProvider
        {
            [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
            private readonly struct Data
            {
                public const int SIZE = sizeof(int) + sizeof(float);
                
                //Data
                public readonly int SampleRange;
                public readonly float SampleScale;

                public Data(int sampleRange, float sampleScale)
                {
                    SampleRange = sampleRange;
                    SampleScale = sampleScale;
                }
            }

            private Data data;

            public SpecializationProvider(int sampleRange, float sampleScale)
                => this.data = new Data(sampleRange, sampleScale);

            SpecializationInfo ISpecializationProvider.GetSpecialization()
            {
                unsafe
                {
                    return new SpecializationInfo(new [] 
                        {
                            new SpecializationMapEntry(
                                constantId: 0,
                                offset: 0,
                                size: new Size(sizeof(int))),
                            new SpecializationMapEntry(
                                constantId: 1,
                                offset: sizeof(int),
                                size: new Size(sizeof(float)))
                        },
                        new Size(Data.SIZE),
                        data: new IntPtr(Unsafe.AsPointer(ref data)));
                }
            }
        }

        //Properties
        internal IShaderInput BlurOutput => outputSampler;

        //Data
        private readonly RenderScene scene;
        private readonly SpecializationProvider specialization;
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

            specialization = new SpecializationProvider(sampleRange, sampleScale);

            //Setup renderer
            renderer = new Renderer(scene.LogicalDevice, scene.InputManager, logger);

            //Add a full-screen object to the renderer
            renderObject = new AttributelessObject(scene, vertexCount: 3, new TextureInfo[0]);
            renderer.AddObject(renderObject, postVertProg, blurFragProg);
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
            renderer.CreateResources(specialization);
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