using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using HT.Engine.Math;
using HT.Engine.Rendering.Memory;
using HT.Engine.Resources;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering.Techniques
{
    internal sealed class AmbientOcclusionTechnique
        : ISpecializationProvider, IPushDataProvider, IDisposable
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
        private readonly struct SpecializationData
        {
            public const int SIZE = sizeof(int) + sizeof(float) * 3;
            
            public readonly int SampleKernelSize;
            public readonly float SampleRadius;
            public readonly float SampleBias;
            public readonly float OcclusionMultiplier;

            public SpecializationData(
                int sampleKernelSize,
                float sampleRadius,
                float sampleBias,
                float occlusionMultiplier)
            {
                SampleKernelSize = sampleKernelSize;
                SampleRadius = sampleRadius;
                SampleBias = sampleBias;
                OcclusionMultiplier = occlusionMultiplier;
            }
        }

        private readonly static Format aoFormat = Format.R8UNorm;
        private readonly static int sampleKernelSize = 16;
        private readonly static float sampleRadius = 1.15f;
        private readonly static float sampleBias = -.025f;
        private readonly static float occlusionMultiplier = 3f;
        private readonly static int noiseSize = 4;
        //We want to blur out the noise, but blur goes in both directions so we need to take half
        private readonly static int blurSampleRange = noiseSize / 2; 

        //Properties
        internal IShaderInput AOOutput => blurTechnique.BlurOutput;

        //Data
        private readonly GBufferTechnique gbufferTechnique;
        private readonly RenderScene scene;
        private readonly Renderer renderer;
        private readonly Logger logger;

        private readonly DeviceSampler rotationNoiseSampler;
        private readonly AttributelessObject renderObject;
        private readonly BoxBlurTechnique blurTechnique;

        private SpecializationData specializationData;

        //Target to render into
        private DeviceTexture aoTarget;
        private DeviceBuffer sampleKernelBuffer;

        private bool disposed;

        internal AmbientOcclusionTechnique(
            GBufferTechnique gbufferTechnique,
            ShaderProgram postVertProg, ShaderProgram aoFragProg, ShaderProgram boxBlurFragProg,
            RenderScene scene,
            Logger logger = null)
        {
            if (gbufferTechnique == null)
                throw new NullReferenceException(nameof(gbufferTechnique));
            if (postVertProg == null)
                throw new ArgumentNullException(nameof(postVertProg));
            if (aoFragProg == null)
                throw new ArgumentNullException(nameof(aoFragProg));
            if (boxBlurFragProg == null)
                throw new ArgumentNullException(nameof(boxBlurFragProg));
            if (scene == null)
                throw new NullReferenceException(nameof(scene));

            this.gbufferTechnique = gbufferTechnique;
            this.scene = scene;
            this.logger = logger;
            
            specializationData = new SpecializationData(
                sampleKernelSize, sampleRadius, sampleBias, occlusionMultiplier);

            //Create renderer for rendering the ao texture
            renderer = new Renderer(scene.LogicalDevice, scene.InputManager, logger);

            //Create random for generating the kernel and noise (using a arbitrary fixed seed)
            IRandom random = new ShiftRandom(seed: 1234); 

            //Create the sample kernel
            Span<Float4> sampleKernel = stackalloc Float4[sampleKernelSize];
            GenerateSampleKernel(random, sampleKernel);
            sampleKernelBuffer = DeviceBuffer.UploadData<Float4>(
                sampleKernel, scene, BufferUsages.UniformBuffer);

            //Create the rotation noise texture
            FloatTexture rotationNoiseTexture = TextureUtils.CreateRandomFloatTexture(
                random, min: (-1f, -1f, 0f, 0f), max: (1f, 1f, 0f, 0f), size: (noiseSize, noiseSize));
            rotationNoiseSampler = new DeviceSampler(scene.LogicalDevice, 
                texture: DeviceTexture.UploadTexture(rotationNoiseTexture, scene),
                repeat: true,
                pointFilter: true);

            //Add full-screen object for drawing the composition
            renderObject = new AttributelessObject(scene, vertexCount: 3, new TextureInfo[0]);
            renderer.AddObject(renderObject, postVertProg, aoFragProg, pushDataProvider: this);

            //Create a BlurTechnique for blurring the ao texture (to hide the ao sample pattern)
            blurTechnique = new BoxBlurTechnique(
                postVertProg,
                boxBlurFragProg,
                blurSampleRange,
                sampleScale: 1f,
                scene,
                logger);
        }

        internal void CreateResources(Int2 swapchainSize)
        {
            ThrowIfDisposed();

            //Dispose of the old target
            aoTarget?.Dispose();

            //Create the new render target
            aoTarget = DeviceTexture.CreateColorTarget(swapchainSize, aoFormat, scene);

            //Bind inputs to the renderer
            renderer.BindGlobalInputs(new IShaderInput[] {
                gbufferTechnique.CameraOutput,
                gbufferTechnique.DepthOutput,
                gbufferTechnique.NormalOutput,
                sampleKernelBuffer,
                rotationNoiseSampler });

            //Bind the targets to the renderer
            renderer.BindTargets(new [] { aoTarget });

            //Tell the renderer to allocate its resources based on the data we've provided
            renderer.CreateResources(specialization: this);

            //Initialize the blurTechnique, point it to the ao-target
            blurTechnique.CreateResources(aoTarget);
        }

        internal void Record(CommandBuffer commandbuffer)
        {
            ThrowIfDisposed();

            renderer.Record(commandbuffer);

            //Insert barrier to wait for rendering of ao-target to be done before starting the blurring
            Renderer.InsertOutputReadBarrier(commandbuffer);

            blurTechnique.Record(commandbuffer);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            blurTechnique.Dispose();
            renderer.Dispose();
            renderObject.Dispose();
            rotationNoiseSampler.Dispose();
            aoTarget?.Dispose();
            sampleKernelBuffer.Dispose();

            disposed = true;
        }

        private static void GenerateSampleKernel(IRandom random, Span<Float4> kernel)
        {
            //Generate points in the hemisphere with higher density near the center
            for (int i = 0; i < kernel.Length; i++)
            {
                Float3 dir = Float3.FastNormalize(
                    random.GetBetween(minValue: (-1f, -1f, 0f), maxValue: (1f, 1f, 1f)));

                float scale = (float)i / kernel.Length;
                Float3 point = dir * FloatUtils.Lerp(.1f, 1f, scale * scale);
                kernel[i] = point.XYZ0;
            }
        }

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
                            size: new Size(sizeof(float))),
                        new SpecializationMapEntry(
                            constantId: 2,
                            offset: sizeof(int) + sizeof(float),
                            size: new Size(sizeof(float))),
                            
                        new SpecializationMapEntry(
                            constantId: 3,
                            offset: sizeof(int) + sizeof(float) + sizeof(float),
                            size: new Size(sizeof(float)))
                    },
                    new Size(SpecializationData.SIZE),
                    data: new IntPtr(Unsafe.AsPointer(ref specializationData)));
            }
        }

        PushConstantRange[] IPushDataProvider.GetDataRanges()
            => new [] { new PushConstantRange(ShaderStages.Fragment, offset: 0, size: Int2.SIZE) };

        void IPushDataProvider.PushData(CommandBuffer commandBuffer, PipelineLayout pipelineLayout)
        {
            //Send the target size as a push-constant to the ambient occlusion shader
            unsafe
            {
                Int2 size = aoTarget.Size;
                commandBuffer.CmdPushConstants(
                    pipelineLayout,
                    ShaderStages.Fragment,
                    offset: 0,
                    size: Int2.SIZE,
                    values: new IntPtr(Unsafe.AsPointer(ref size)));
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(AmbientOcclusionTechnique)}] Allready disposed");
        }
    }
}