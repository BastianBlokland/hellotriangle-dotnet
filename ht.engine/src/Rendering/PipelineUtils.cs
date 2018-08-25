using System;
using HT.Engine.Utils;

using VulkanCore;

namespace HT.Engine.Rendering
{
    internal static class PipelineUtils
    {
        internal static Pipeline CreatePipeline(
            Device logicalDevice,
            RenderPass renderpass,
            PipelineLayout layout,
            ShaderModule vertModule,
            ShaderModule fragModule,
            bool depthClamp,
            bool depthBias,
            ReadOnlySpan<DeviceTexture> targets,
            IInternalRenderObject renderObject)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (renderpass == null)
                throw new ArgumentNullException(nameof(renderpass));

            var shaderStages = new []
            {
                new PipelineShaderStageCreateInfo(
                    stage: ShaderStages.Vertex, module: vertModule, name: "main"),
                new PipelineShaderStageCreateInfo(
                    stage: ShaderStages.Fragment,
                    module: fragModule, name: "main")
            };
            var depthTest = new PipelineDepthStencilStateCreateInfo {
                DepthTestEnable = true,
                DepthWriteEnable = true,
                DepthCompareOp = CompareOp.LessOrEqual,
                DepthBoundsTestEnable = false,
                StencilTestEnable = false
            };
            var rasterizer = new PipelineRasterizationStateCreateInfo(
                depthClampEnable: depthClamp,
                rasterizerDiscardEnable: false,
                polygonMode: PolygonMode.Fill,
                cullMode: CullModes.Back,
                frontFace: renderObject.GetFrontFace(),
                depthBiasEnable: depthBias,
                depthBiasConstantFactor: .1f,
                depthBiasSlopeFactor: 1.75f,
                lineWidth: 1f
            );

            //Gather all the color targets and setup a blend-state for them
            ResizeArray<PipelineColorBlendAttachmentState> blendAttachments =
                new ResizeArray<PipelineColorBlendAttachmentState>();
            for (int i = 0; i < targets.Length; i++)
            {
                if (!targets[i].DepthTexture)
                    blendAttachments.Add(new PipelineColorBlendAttachmentState(
                        colorWriteMask: ColorComponents.All, blendEnable: false));
            }

            var blending = new PipelineColorBlendStateCreateInfo(
                attachments: blendAttachments.ToArray(),
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
                layout: layout,
                renderPass: renderpass,
                subpass: 0,
                stages: shaderStages,
                inputAssemblyState: renderObject.GetInputAssemblyStateInfo(),
                vertexInputState: renderObject.GetVertexInputState(),
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
    }
}