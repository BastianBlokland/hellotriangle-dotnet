using System;
using System.Runtime.CompilerServices;

using HT.Engine.Math;
using HT.Engine.Resources;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering.Techniques
{
    internal sealed class AmbientOcclusionTechnique : IPushDataProvider, IDisposable
    {
        private readonly static Format aoFormat = Format.R8UNorm;
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

        //Target to render into
        private DeviceTexture aoTarget;

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
            renderer = new Renderer(scene.LogicalDevice, scene.InputManager, logger);

            //Create random for generating the kernel and noise (using a arbitrary fixed seed)
            IRandom random = new ShiftRandom(seed: 1234); 

            //Create the rotation noise texture
            FloatTexture rotationNoiseTexture = TextureUtils.CreateRandomFloatTexture(
                random, min: (-1f, -1f, 0f, 0f), max: (1f, 1f, 0f, 0f), size: (noiseSize, noiseSize));
            rotationNoiseSampler = new DeviceSampler(scene.LogicalDevice, 
                texture: DeviceTexture.UploadTexture(rotationNoiseTexture, scene),
                repeat: true);

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
                gbufferTechnique.DepthOutput,
                gbufferTechnique.NormalOutput,
                rotationNoiseSampler });

            //Bind the targets to the renderer
            renderer.BindTargets(new [] { aoTarget });

            //Tell the renderer to allocate its resources based on the data we've provided
            renderer.CreateResources(specialization: null);

            //Initialize the blurTechnique, point it to the ao-target
            blurTechnique.CreateResources(aoTarget);
        }

        internal void Record(CommandBuffer commandbuffer)
        {
            ThrowIfDisposed();

            renderer.Record(commandbuffer);

            //Insert barrier to wait for rendering of ao-target to be done before starting the blurring
            commandbuffer.CmdPipelineBarrier(
                srcStageMask: PipelineStages.BottomOfPipe,
                dstStageMask: PipelineStages.FragmentShader);

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

            disposed = true;
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