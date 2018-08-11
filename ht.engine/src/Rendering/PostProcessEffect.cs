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
        private readonly DeviceSampler[] samplers;
        private readonly DescriptorManager.Block descriptorBlock;
        private readonly PipelineLayout pipelineLayout;
        private readonly Pipeline pipeline;
        private bool disposed;

        public PostProcessEffect(
            RenderScene scene,
            ShaderProgram vertProg,
            ShaderProgram fragProg)
        {
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));
            if (vertProg == null)
                throw new ArgumentNullException(nameof(vertProg));
            if (fragProg == null)
                throw new ArgumentNullException(nameof(fragProg));
            this.scene = scene;

            //Create the shader modules
            vertModule = vertProg.CreateModule(scene.LogicalDevice);
            fragModule = fragProg.CreateModule(scene.LogicalDevice);

            //Create the samplers (1 for each scene target)
            samplers = new DeviceSampler[3];
            for (int i = 0; i < samplers.Length; i++)
                samplers[i] = new DeviceSampler(scene.LogicalDevice);    

            //Create the descriptor binding
            var binding = new DescriptorBinding(uniformBufferCount: 1, imageSamplerCount: samplers.Length);
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
            samplers.DisposeAll();
            disposed = true;
        }

        internal void BindSceneTargets(
            DeviceTexture sceneColor,
            DeviceTexture sceneNormal,
            DeviceTexture sceneDepth)
        {
            descriptorBlock.Update(
                buffers: new Memory.IBuffer[] { scene.SceneDataBuffer },
                samplers: samplers,
                textures: new [] { sceneColor, sceneNormal, sceneDepth });
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