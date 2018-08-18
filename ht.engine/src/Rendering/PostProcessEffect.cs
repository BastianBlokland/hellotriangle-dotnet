using System;
using System.Runtime.CompilerServices;
using HT.Engine.Math;
using HT.Engine.Resources;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering
{
    internal sealed class PostProcessEffect : IDisposable
    {
        private readonly RenderScene scene;
        private readonly ShaderModule vertModule;
        private readonly ShaderModule fragModule;
        private readonly DeviceTexture[] textures;
        private readonly DeviceSampler[] samplers;
        private readonly DescriptorManager.Block descriptorBlock;
        private readonly PipelineLayout pipelineLayout;
        private readonly Pipeline pipeline;
        private bool disposed;

        public PostProcessEffect(
            RenderScene scene,
            TextureInfo[] textureInfos,
            ShaderProgram vertProg,
            ShaderProgram fragProg)
        {
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));
            if (textureInfos == null)
                throw new ArgumentNullException(nameof(textureInfos));
            if (vertProg == null)
                throw new ArgumentNullException(nameof(vertProg));
            if (fragProg == null)
                throw new ArgumentNullException(nameof(fragProg));
            this.scene = scene;

            //Create the shader modules
            vertModule = vertProg.CreateModule(scene.LogicalDevice);
            fragModule = fragProg.CreateModule(scene.LogicalDevice);

            //Create the textures and samplers
            textures = new DeviceTexture[RenderScene.RENDER_TARGET_COUNT + textureInfos.Length];
            samplers = new DeviceSampler[RenderScene.RENDER_TARGET_COUNT + textureInfos.Length];
            for (int i = 0; i < RenderScene.RENDER_TARGET_COUNT; i++)
            {
                textures[i] = null; //Will be set dynamically
                samplers[i] = new DeviceSampler(scene.LogicalDevice);
            }
            for (int i = 0; i < textureInfos.Length; i++)
            {
                int index = RenderScene.RENDER_TARGET_COUNT + i;
                textures[index] = DeviceTexture.UploadTexture(
                    texture: textureInfos[i].Texture as IInternalTexture,
                    generateMipMaps: textureInfos[i].UseMipMaps,
                    scene.LogicalDevice, scene.MemoryPool, scene.StagingBuffer, scene.Executor);
                samplers[index] = new DeviceSampler(scene.LogicalDevice,
                    mipLevels: textures[index].MipLevels,
                    repeat: textureInfos[i].Repeat,
                    maxAnisotropy: 8f);
            }

            //Create the descriptor binding
            var binding = new DescriptorBinding(uniformBufferCount: 3, imageSamplerCount: samplers.Length);
            descriptorBlock = scene.DescriptorManager.Allocate(binding);

            //Create the pipeline
            pipelineLayout = scene.LogicalDevice.CreatePipelineLayout(new PipelineLayoutCreateInfo(
                setLayouts: new [] { descriptorBlock.Layout },
                pushConstantRanges:  null));

            pipeline = CreatePipeline(scene.LogicalDevice, scene.CompositionRenderpass);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            descriptorBlock.Free();
            pipelineLayout.Dispose();
            pipeline.Dispose();
            vertModule.Dispose();
            fragModule.Dispose();
            //Note: We only need to dispose our own textures,
            //the scene render targets will be disposed by the scene
            for (int i = RenderScene.RENDER_TARGET_COUNT; i < textures.Length; i++)
                textures[i].Dispose();
            samplers.DisposeAll();

            disposed = true;
        }

        internal void BindSceneTargets(
            DeviceTexture sceneColor,
            DeviceTexture sceneNormal,
            DeviceTexture sceneAttributes,
            DeviceTexture sceneDepth,
            DeviceTexture sceneShadow)
        {
            //Set the scene target textures
            textures[0] = sceneColor;
            textures[1] = sceneNormal;
            textures[2] = sceneAttributes;
            textures[3] = sceneDepth;
            textures[4] = sceneShadow;

            descriptorBlock.Update(
                buffers: new Memory.IBuffer[] { 
                    scene.CameraBuffer, scene.ShadowCameraBuffer, scene.SceneDataBuffer },
                samplers: samplers,
                textures: textures);
        }

        internal void Record(CommandBuffer commandbuffer)
        {
            commandbuffer.CmdBindDescriptorSet(
                PipelineBindPoint.Graphics,
                pipelineLayout,
                descriptorBlock.Set);

            commandbuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);
            commandbuffer.CmdDraw(vertexCount: 3, instanceCount: 1, firstVertex: 0, firstInstance: 0);
        }

        private Pipeline CreatePipeline(Device logicalDevice, RenderPass renderpass)
        {
            var shaderStages = new []
            {
                new PipelineShaderStageCreateInfo(
                    stage: ShaderStages.Vertex, module: vertModule, name: "main"),
                new PipelineShaderStageCreateInfo(
                    stage: ShaderStages.Fragment, module: fragModule, name: "main")
            };
            var depthTest = new PipelineDepthStencilStateCreateInfo {
                DepthTestEnable = false, //No depth testing
                DepthWriteEnable = false, //No depth writing
                DepthCompareOp = CompareOp.LessOrEqual,
                DepthBoundsTestEnable = false,
                StencilTestEnable = false
            };
            var rasterizer = new PipelineRasterizationStateCreateInfo(
                depthClampEnable: false,
                polygonMode: PolygonMode.Fill,
                cullMode: CullModes.None, //No culling
                frontFace: FrontFace.Clockwise,
                lineWidth: 1f
            );
            var blending = new PipelineColorBlendStateCreateInfo(
                attachments: new [] 
                { 
                    new PipelineColorBlendAttachmentState(
                        blendEnable: false,
                        colorWriteMask: ColorComponents.All)
                },
                logicOpEnable: false
            );
            var multisampleState = new PipelineMultisampleStateCreateInfo(
                rasterizationSamples: SampleCounts.Count1,
                sampleShadingEnable: false
            );
            //Pass the viewport and scissor-rect as dynamic so we are not tied to swapchain size
            //the advantage is this is that we don't need to recreate the pipeline on swapchain
            //resize
            var dynamicState = new PipelineDynamicStateCreateInfo(
                DynamicState.Viewport,
                DynamicState.Scissor
            );
            
            return logicalDevice.CreateGraphicsPipeline(new GraphicsPipelineCreateInfo(
                layout: pipelineLayout,
                renderPass: renderpass,
                subpass: 0,
                stages: shaderStages,
                inputAssemblyState: new PipelineInputAssemblyStateCreateInfo(
                    topology: PrimitiveTopology.TriangleList,
                    primitiveRestartEnable: false),
                vertexInputState: new PipelineVertexInputStateCreateInfo(), //No vertex inputs
                rasterizationState: rasterizer,
                tessellationState: null,
                //Pass empty viewport and scissor-rect as we set them dynamically
                viewportState: new PipelineViewportStateCreateInfo(new Viewport(), new Rect2D()),
                multisampleState: multisampleState,
                depthStencilState: depthTest,
                colorBlendState: blending,
                dynamicState: dynamicState,
                flags: PipelineCreateFlags.None
            ));
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(PostProcessEffect)}] Allready disposed");
        }
    }
}