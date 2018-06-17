using System;

using HT.Engine.Math;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class RenderScene
    {
        private readonly Float4 clearColor;
        private readonly ShaderProgram vertProg;
        private readonly ShaderProgram fragProg;

        private PipelineLayout pipelineLayout;
        private Pipeline pipeline;
        private bool pipelineCreated;

        public RenderScene(Float4 clearColor, ShaderProgram vertProg, ShaderProgram fragProg)
        {
            this.clearColor = clearColor;
            this.vertProg = vertProg;
            this.fragProg = fragProg;
        }

        internal void CreatePipeline(Device logicalDevice, Int2 size, RenderPass renderpass)
        {
            if(pipelineCreated)
                throw new Exception($"[{nameof(RenderScene)}] Unable to create a pipeline before disposing of the previous pipeline");

            //Create the pipeline layout
            pipelineLayout = logicalDevice.CreatePipelineLayout(new PipelineLayoutCreateInfo()); //Empty atm as we have no dynamic state yet

            ShaderModule vertModule = vertProg.CreateModule(logicalDevice);
            ShaderModule fragModule = fragProg.CreateModule(logicalDevice);

            var shaderStages = new [] 
            { 
                new PipelineShaderStageCreateInfo(stage: ShaderStages.Vertex, module: vertModule, name: "main"),
                new PipelineShaderStageCreateInfo(stage: ShaderStages.Fragment, module: fragModule, name: "main")
            };
            var vertexInput = new PipelineVertexInputStateCreateInfo
            (
                vertexBindingDescriptions: null, //No vertex info atm because we are hard-coding positions in the shader
                vertexAttributeDescriptions: null
            );
            var inputAssembly = new PipelineInputAssemblyStateCreateInfo
            (
                topology: PrimitiveTopology.TriangleStrip,
                primitiveRestartEnable: false
            );
            var viewport = new PipelineViewportStateCreateInfo
            (
                viewport: new Viewport(0f, 0f, width: size.X, height: size.Y, minDepth: 0f, maxDepth: 1f),
                scissor: new Rect2D(x: 0, y: 0, width: size.X, height: size.Y)
            );
            var rasterizer = new PipelineRasterizationStateCreateInfo
            (
                depthClampEnable: false,
                polygonMode: PolygonMode.Fill,
                cullMode: CullModes.Back,
                frontFace: FrontFace.Clockwise,
                lineWidth: 1f
            );
            var blending = new PipelineColorBlendStateCreateInfo
            (
                attachments: new [] 
                { 
                    new PipelineColorBlendAttachmentState
                    (
                        colorWriteMask: ColorComponents.All,
                        blendEnable: false
                    )
                },
                logicOpEnable: false
            );
            var multisampleState = new PipelineMultisampleStateCreateInfo
            (
                rasterizationSamples: SampleCounts.Count1,
                sampleShadingEnable: false
            );
            
            //Create the pipeline
            pipeline = logicalDevice.CreateGraphicsPipeline(new GraphicsPipelineCreateInfo
            (
                layout: pipelineLayout,
                renderPass: renderpass,
                subpass: 0,
                stages: shaderStages,
                inputAssemblyState: inputAssembly,
                vertexInputState: vertexInput,
                rasterizationState: rasterizer,
                tessellationState: null,
                viewportState: viewport,
                multisampleState: multisampleState,
                depthStencilState: null,
                colorBlendState: blending,
                dynamicState: null,
                flags: PipelineCreateFlags.None
            ));
            
            //After pipeline creation we no longer need the shader modules
            vertModule.Dispose();
            fragModule.Dispose();

            pipelineCreated = true;
        }

        internal void Record(CommandBuffer commandbuffer, Framebuffer framebuffer, RenderPass renderpass, Int2 swapchainSize)
        {
            if(!pipelineCreated)
                throw new Exception($"[{nameof(RenderScene)}] Unable to record if we have no pipeline created");

            commandbuffer.CmdBeginRenderPass(new RenderPassBeginInfo
            (
                renderPass: renderpass,
                framebuffer: framebuffer,
                renderArea: new Rect2D(x: 0, y: 0, width: swapchainSize.X, height: swapchainSize.Y),
                clearValues: new ClearValue(new ClearColorValue(new ColorF4(clearColor.R, clearColor.G, clearColor.B, clearColor.A)))
            ));

            commandbuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);
            commandbuffer.CmdDraw(vertexCount: 4, instanceCount: 1, firstVertex: 0, firstInstance: 0);

            commandbuffer.CmdEndRenderPass();
        }

        internal void DisposePipeline()
        {
            if(!pipelineCreated)
                throw new Exception($"[{nameof(RenderScene)}] Unable to dispose of the pipeline as we haven't created one");

            pipelineLayout.Dispose();
            pipeline.Dispose();

            pipelineCreated = false;
        }

        ~RenderScene()
        {
            if(pipelineCreated)
                throw new Exception($"[{nameof(RenderScene)}] Scene was released without disposing the pipeline!");
        }
    }
}