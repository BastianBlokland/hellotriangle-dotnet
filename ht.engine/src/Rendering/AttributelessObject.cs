using System;
using System.Runtime.CompilerServices;
using HT.Engine.Math;
using HT.Engine.Resources;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class AttributelessObject : IInternalRenderObject
    {
        //Properties
        public int RenderOrder => renderOrder;
        
        //Data
        private readonly RenderScene scene;
        private readonly int vertexCount;
        private readonly ShaderModule vertModule;
        private readonly ShaderModule fragModule;
        private readonly ShaderModule shadowFragModule;
        private readonly DeviceSampler[] samplers;
        private readonly DeviceTexture[] textures;
        private readonly DescriptorManager.Block descriptorBlock;
        private readonly PipelineLayout pipelineLayout;
        private readonly Pipeline pipeline;
        private readonly Pipeline shadowPipeline;
        private int renderOrder;
        private bool disposed;

        public AttributelessObject(
            RenderScene scene,
            int renderOrder,
            int vertexCount,
            TextureInfo[] textureInfos,
            ShaderProgram vertProg,
            ShaderProgram fragProg,
            ShaderProgram shadowFragProg)
        {
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));
            if (textureInfos == null)
                throw new ArgumentNullException(nameof(textureInfos));
            if (vertProg == null)
                throw new ArgumentNullException(nameof(vertProg));
            if (fragProg == null)
                throw new ArgumentNullException(nameof(fragProg));
            if (shadowFragProg == null)
                throw new ArgumentNullException(nameof(shadowFragProg));
            this.scene = scene;
            this.renderOrder = renderOrder;
            this.vertexCount = vertexCount;

            //Create the shader modules
            vertModule = vertProg.CreateModule(scene.LogicalDevice);
            fragModule = fragProg.CreateModule(scene.LogicalDevice);
            shadowFragModule = shadowFragProg.CreateModule(scene.LogicalDevice);

            //Setup textures and samplers
            textures = new DeviceTexture[textureInfos.Length];
            samplers = new DeviceSampler[textureInfos.Length];
            for (int i = 0; i < textureInfos.Length; i++)
            {
                textures[i] = DeviceTexture.UploadTexture(
                    texture: textureInfos[i].Texture as IInternalTexture,
                    generateMipMaps: textureInfos[i].UseMipMaps,
                    scene.LogicalDevice, scene.MemoryPool, scene.StagingBuffer, scene.Executor);
                samplers[i] = new DeviceSampler(scene.LogicalDevice,
                    mipLevels: textures[i].MipLevels,
                    repeat: textureInfos[i].Repeat,
                    maxAnisotropy: 8f);
            }

            //Create the descriptor binding
            var binding = new DescriptorBinding(uniformBufferCount: 1, imageSamplerCount: textures.Length);
            descriptorBlock = scene.DescriptorManager.Allocate(binding);
            descriptorBlock.Update(new Memory.IBuffer[] { scene.SceneDataBuffer }, samplers, textures);

            //Create the pipeline
            pipelineLayout = scene.LogicalDevice.CreatePipelineLayout(new PipelineLayoutCreateInfo(
                setLayouts: new [] { descriptorBlock.Layout },
                pushConstantRanges:  null ));

            pipeline = CreatePipeline(scene.LogicalDevice, scene.GeometryRenderpass, shadowPass: false);
            shadowPipeline = CreatePipeline(scene.LogicalDevice, scene.ShadowRenderpass, shadowPass: true);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            textures.DisposeAll();
            samplers.DisposeAll();
            descriptorBlock.Free();
            pipelineLayout.Dispose();
            pipeline.Dispose();
            shadowPipeline.Dispose();
            vertModule.Dispose();
            fragModule.Dispose();
            disposed = true;
        }

        void IInternalRenderObject.Record(CommandBuffer commandbuffer, bool shadowPass)
        {
            commandbuffer.CmdBindDescriptorSet(
                PipelineBindPoint.Graphics,
                pipelineLayout,
                descriptorBlock.Set);

            //Bind pipeline
            commandbuffer.CmdBindPipeline(PipelineBindPoint.Graphics,
                pipeline: shadowPass ? shadowPipeline : pipeline);

            //Draw
            commandbuffer.CmdDraw(vertexCount, instanceCount: 1, firstVertex: 0, firstInstance: 0);
        }

        private Pipeline CreatePipeline(Device logicalDevice, RenderPass renderpass, bool shadowPass)
        {
            var shaderStages = new []
            {
                new PipelineShaderStageCreateInfo(
                    stage: ShaderStages.Vertex, module: vertModule, name: "main"),
                new PipelineShaderStageCreateInfo(
                    stage: ShaderStages.Fragment, 
                    module: shadowPass ? shadowFragModule : fragModule, name: "main")
            };
            var depthTest = new PipelineDepthStencilStateCreateInfo {
                DepthTestEnable = true,
                DepthWriteEnable = true,
                DepthCompareOp = CompareOp.LessOrEqual,
                DepthBoundsTestEnable = false,
                StencilTestEnable = false
            };
            var rasterizer = new PipelineRasterizationStateCreateInfo(
                depthClampEnable: false,
                polygonMode: PolygonMode.Fill,
                cullMode: shadowPass ? CullModes.Front : CullModes.Back,
                frontFace: FrontFace.Clockwise,
                lineWidth: 1f
            );
            var blending = new PipelineColorBlendStateCreateInfo(
                attachments: shadowPass ? new PipelineColorBlendAttachmentState[0] : new [] 
                { 
                    //Color target
                    new PipelineColorBlendAttachmentState(
                        colorWriteMask: ColorComponents.All, blendEnable: false),
                    //Normal target
                    new PipelineColorBlendAttachmentState(
                        colorWriteMask: ColorComponents.All, blendEnable: false),
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
                throw new Exception($"[{nameof(AttributelessObject)}] Allready disposed");
        }
    }
}