using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using HT.Engine.Math;
using HT.Engine.Rendering.Memory;
using HT.Engine.Resources;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering.Techniques
{
    internal sealed class AmbientOcclusionTechnique : IDisposable
    {
        private readonly static Format aoFormat = Format.R8UNorm;
        private readonly static float targetSizeMultiplier = 1f;
        private readonly static int sampleKernelSize = 8;
        private readonly static float sampleRadius = 1f;
        private readonly static float sampleBias = -.005f;
        private readonly static int noiseSize = 2;
        //We want to blur out the noise, but blur goes in both directions so we need to take half
        private readonly static int blurSampleRange = noiseSize / 2; 

        //Properties
        internal IShaderInput AOOutput => blurTechnique.BlurOutput;

        //Data
        private readonly GBufferTechnique gbufferTechnique;
        private readonly RenderScene scene;
        private readonly Renderer renderer;
        private readonly int swapchainIndexPushDataBinding;
        private readonly int targetSizePushBinding;
        private readonly Logger logger;

        private readonly DeviceSampler rotationNoiseSampler;
        private readonly AttributelessObject renderObject;
        private readonly BoxBlurTechnique blurTechnique;

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
            
            //Create renderer for rendering the ao texture
            renderer = new Renderer(scene, logger);
            renderer.AddSpecialization(scene.SwapchainCount);
            renderer.AddSpecialization(sampleKernelSize);
            renderer.AddSpecialization(sampleRadius);
            renderer.AddSpecialization(sampleBias);
            swapchainIndexPushDataBinding = renderer.AddPushData<int>();
            targetSizePushBinding = renderer.AddPushData<Int2>();

            //Create random for generating the kernel and noise (using a arbitrary fixed seed)
            IRandom random = new ShiftRandom(seed: 1234); 

            //Create the sample kernel
            Span<Float4> sampleKernel = stackalloc Float4[sampleKernelSize];
            GenerateSampleKernel(random, sampleKernel);
            sampleKernelBuffer = DeviceBuffer.UploadData<Float4>(
                sampleKernel, scene, BufferUsages.UniformBuffer);

            //Create the rotation noise texture
            Float4Texture rotationNoiseTexture = TextureUtils.CreateRandomFloatTexture(
                random, min: (-1f, -1f, 0f, 0f), max: (1f, 1f, 0f, 0f), size: (noiseSize, noiseSize));
            rotationNoiseSampler = new DeviceSampler(scene.LogicalDevice, 
                texture: DeviceTexture.UploadTexture(rotationNoiseTexture, scene),
                repeat: true,
                pointFilter: true);

            //Add full-screen object for drawing the composition
            renderObject = new AttributelessObject(scene, vertexCount: 3, new TextureInfo[0]);
            renderer.AddObject(renderObject, postVertProg, aoFragProg, debugName: "fullscreen");

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
            aoTarget = DeviceTexture.CreateColorTarget(
                size: (swapchainSize * targetSizeMultiplier).RoundToInt(), aoFormat, scene);

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
            renderer.CreateResources();

            //Initialize the blurTechnique, point it to the ao-target
            blurTechnique.CreateResources(aoTarget);
        }

        internal void Record(CommandBuffer commandbuffer, int swapchainIndex)
        {
            ThrowIfDisposed();

            scene.BeginDebugMarker(commandbuffer, "AmbientOcclusion");
            {
                renderer.SetPushData(swapchainIndexPushDataBinding, swapchainIndex);
                renderer.SetPushData(targetSizePushBinding, aoTarget.Size);
                renderer.Record(commandbuffer);

                //Insert barrier to wait for rendering of ao-target to be done before starting the blurring
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
                    random.GetBetween(minValue: (-1f, -1f, .2f), maxValue: (1f, 1f, 1f)));

                float scale = (float)i / kernel.Length;
                Float3 point = dir * FloatUtils.Lerp(.1f, 1f, scale * scale);
                kernel[i] = point.XYZ0;
            }
        }

        [Conditional("DEBUG")]
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(AmbientOcclusionTechnique)}] Allready disposed");
        }
    }
}