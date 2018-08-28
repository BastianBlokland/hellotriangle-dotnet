using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HT.Engine.Math;
using HT.Engine.Resources;
using HT.Engine.Utils;

using VulkanCore;

namespace HT.Engine.Rendering.Techniques
{
    internal sealed class BlurTechnique : IDisposable
    {
        private class SpecializationProvider : ISpecializationProvider
        {
            [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
            private readonly struct Data
            {
                public const int SIZE = sizeof(bool) + sizeof(float);
                
                //Data
                public readonly bool IsHorizontal;
                public readonly float StepScale;

                public Data(bool isHorizontal, float stepScale)
                {
                    IsHorizontal = isHorizontal;
                    StepScale = stepScale;
                }
            }

            private Data data;

            public SpecializationProvider(bool isHorizontal, float stepScale)
                => this.data = new Data(isHorizontal, stepScale);

            SpecializationInfo ISpecializationProvider.GetSpecialization()
            {
                unsafe
                {
                    return new SpecializationInfo(new [] 
                        {
                            new SpecializationMapEntry(
                                constantId: 0,
                                offset: 0,
                                size: new Size(sizeof(bool))),
                            new SpecializationMapEntry(
                                constantId: 1,
                                offset: sizeof(bool),
                                size: new Size(sizeof(float)))
                        },
                        new Size(Data.SIZE),
                        data: new IntPtr(Unsafe.AsPointer(ref data)));
                }
            }
        }

        //Data
        private readonly int iterations;
        private readonly RenderScene scene;
        private readonly SpecializationProvider horizontalSpecialization;
        private readonly SpecializationProvider verticalSpecialization;
        private readonly Renderer rendererA;
        private readonly Renderer rendererB;

        private readonly AttributelessObject renderObject;

        //Create a temp target. Ping-pongs from the provided target to this 'temp' target for each step
        private DeviceTexture targetB;

        //Samplers used for the ping-ponging
        private DeviceSampler samplerA;
        private DeviceSampler samplerB;

        private bool disposed;

        internal BlurTechnique(
            ShaderProgram postVertProg, ShaderProgram blurFragProg,
            int iterations, float stepScale, RenderScene scene, Logger logger = null)
        {
            if (postVertProg == null)
                throw new ArgumentNullException(nameof(postVertProg));
            if (blurFragProg == null)
                throw new ArgumentNullException(nameof(blurFragProg));
            if (scene == null)
                throw new NullReferenceException(nameof(scene));

            this.iterations = iterations;
            this.scene = scene;

            horizontalSpecialization = new SpecializationProvider(isHorizontal: true, stepScale);
            verticalSpecialization = new SpecializationProvider(isHorizontal: false, stepScale);

            //Create renderers (2 so we can ping-pong between two targets)
            rendererA = new Renderer(scene.LogicalDevice, scene.InputManager, logger);
            rendererB = new Renderer(scene.LogicalDevice, scene.InputManager, logger);

            //Add a full-screen object to both renderers
            renderObject = new AttributelessObject(scene, vertexCount: 3, new TextureInfo[0]);
            rendererA.AddObject(renderObject, postVertProg, blurFragProg);
            rendererB.AddObject(renderObject, postVertProg, blurFragProg);
        }

        internal void CreateResources(DeviceTexture blurTarget)
        {
            ThrowIfDisposed();

            //Dispose of the resources
            targetB?.Dispose();
            samplerA?.Dispose();
            samplerB?.Dispose();

            //Create a temp target (same format and size as the blur-target) so we can ping-point
            //between the original target and this temp target for each blur step
            targetB = DeviceTexture.CreateColorTarget(blurTarget.Size, blurTarget.Format,
                scene.LogicalDevice, scene.MemoryPool, scene.Executor);
            
            //Create samplers
            samplerA = new DeviceSampler(scene.LogicalDevice, blurTarget, disposeTexture: false);
            samplerB = new DeviceSampler(scene.LogicalDevice, targetB, disposeTexture: false);

            //Bind the inputs
            rendererA.BindGlobalInputs(new IShaderInput[] { samplerA });
            rendererB.BindGlobalInputs(new IShaderInput[] { samplerB });

            //Bind the targets
            rendererA.BindTargets(new [] { targetB });
            rendererB.BindTargets(new [] { blurTarget });

            //Tell the renderers to allocate their resources based on the data we've provided
            rendererA.CreateResources(specialization: horizontalSpecialization);
            rendererB.CreateResources(specialization: verticalSpecialization);
        }

        internal void Record(CommandBuffer commandbuffer)
        {
            ThrowIfDisposed();

            for (int i = 0; i < iterations; i++)
            {
                if (i != 0)
                {
                    //Insert barrier to wait for last blur 'step' to be done
                    commandbuffer.CmdPipelineBarrier(
                        srcStageMask: PipelineStages.BottomOfPipe,
                        dstStageMask: PipelineStages.FragmentShader);
                }

                rendererA.Record(commandbuffer);

                //Insert barrier to wait for rendererA is done before starting rendererB
                commandbuffer.CmdPipelineBarrier(
                    srcStageMask: PipelineStages.BottomOfPipe,
                    dstStageMask: PipelineStages.FragmentShader);

                rendererB.Record(commandbuffer);
            }
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            rendererA.Dispose();
            rendererB.Dispose();
            renderObject.Dispose();
            samplerB?.Dispose();
            samplerA?.Dispose();
            targetB?.Dispose();

            disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(BlurTechnique)}] Allready disposed");
        }
    }
}