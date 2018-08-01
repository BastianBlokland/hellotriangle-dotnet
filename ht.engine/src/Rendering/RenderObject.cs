using System;
using System.Runtime.CompilerServices;
using HT.Engine.Math;
using HT.Engine.Resources;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class RenderObject : IDisposable
    {
        private readonly RenderScene scene;
        private readonly ShaderModule vertModule;
        private readonly ShaderModule fragModule;
        private readonly DeviceMesh deviceMesh;
        private readonly DeviceTexture[] deviceTextures;
        private readonly DeviceSampler[] deviceSamplers;
        private readonly Memory.HostBuffer instanceDataBuffer;
        private readonly Memory.HostBuffer indirectArgumentsBuffer;
        private readonly DescriptorManager.Block descriptorBlock;
        private readonly PipelineLayout pipelineLayout;
        private readonly Pipeline pipeline;
        private bool disposed;

        public RenderObject(
            RenderScene scene,
            Mesh mesh,
            ITexture[] textures,
            ShaderProgram vertProg,
            ShaderProgram fragProg,
            int maxInstances = 100_000)
        {
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));
            if (textures == null)
                throw new ArgumentNullException(nameof(textures));
            if (vertProg == null)
                throw new ArgumentNullException(nameof(vertProg));
            if (fragProg == null)
                throw new ArgumentNullException(nameof(fragProg));
            this.scene = scene;

            //Create the shader modules
            vertModule = vertProg.CreateModule(scene.LogicalDevice);
            fragModule = fragProg.CreateModule(scene.LogicalDevice);

            //Upload our mesh to the gpu
            deviceMesh = new DeviceMesh(
                mesh, 
                scene.LogicalDevice,
                scene.MemoryPool,
                scene.StagingBuffer,
                scene.Executor);
            //Upload the textures to the gpu
            deviceTextures = new DeviceTexture[textures.Length];
            for (int i = 0; i < deviceTextures.Length; i++)
                deviceTextures[i] = DeviceTexture.UploadTexture(
                    texture: textures[i] as IInternalTexture,
                    logicalDevice: scene.LogicalDevice,
                    memoryPool: scene.MemoryPool,
                    stagingBuffer: scene.StagingBuffer,
                    executor: scene.Executor);
            deviceSamplers = new DeviceSampler[textures.Length];
            for (int i = 0; i < deviceSamplers.Length; i++)
                deviceSamplers[i] = new DeviceSampler(scene.LogicalDevice);

            //Allocate a buffers for the instance data and indirect args
            instanceDataBuffer = new Memory.HostBuffer(
                logicalDevice: scene.LogicalDevice, 
                memoryPool: scene.MemoryPool,
                usages: BufferUsages.VertexBuffer,
                size: InstanceData.SIZE * maxInstances);
            indirectArgumentsBuffer = new Memory.HostBuffer(
                logicalDevice: scene.LogicalDevice,
                memoryPool: scene.MemoryPool,
                usages: BufferUsages.IndirectBuffer,
                size: DrawIndexedIndirectCommand.SIZE);
            //Write defaults to the indirect args buffer
            indirectArgumentsBuffer.Write(new DrawIndexedIndirectCommand(
                indexCount: (uint)deviceMesh.IndexCount,
                instanceCount: 0, firstIndex: 0, vertexOffset: 0, firstInstance: 0));

            //Create the descriptor binding
            var binding = new DescriptorBinding(uniformBufferCount: 1, imageSamplerCount: textures.Length);
            descriptorBlock = scene.DescriptorManager.Allocate(binding);
            descriptorBlock.Update(
                new Memory.IBuffer[] { scene.SceneDataBuffer }, deviceSamplers, deviceTextures);

            //Create the pipeline
            pipelineLayout = scene.LogicalDevice.CreatePipelineLayout(new PipelineLayoutCreateInfo(
                setLayouts: new [] { descriptorBlock.Layout },
                pushConstantRanges:  null ));

            pipeline = CreatePipeline(scene.LogicalDevice, scene.RenderPass);
        }

        public void UpdateInstances(Span<InstanceData> instances)
        {
            instanceDataBuffer.Write(instances);
            indirectArgumentsBuffer.Write(new DrawIndexedIndirectCommand(
                indexCount: (uint)deviceMesh.IndexCount,
                instanceCount: (uint)instances.Length,
                firstIndex: 0,
                vertexOffset: 0,
                firstInstance: 0));
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            deviceMesh.Dispose();
            deviceSamplers.DisposeAll();
            deviceTextures.DisposeAll();
            instanceDataBuffer.Dispose();
            indirectArgumentsBuffer.Dispose();
            descriptorBlock.Free();
            pipelineLayout.Dispose();
            pipeline.Dispose();
            vertModule.Dispose();
            fragModule.Dispose();
            disposed = true;
        }

        internal void Record(CommandBuffer commandbuffer)
        {
            //Bind mesh data
            deviceMesh.RecordBind(commandbuffer, binding: 0);
            //Binding instance data
            commandbuffer.CmdBindVertexBuffer(
                instanceDataBuffer.VulkanBuffer,
                firstBinding: 1,
                offset: 0);

            commandbuffer.CmdBindDescriptorSet(
                PipelineBindPoint.Graphics,
                pipelineLayout,
                descriptorBlock.Set);

            //Bind pipeline
            commandbuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);

            //Draw
            commandbuffer.CmdDrawIndexedIndirect(
                buffer: indirectArgumentsBuffer.VulkanBuffer,
                offset: 0,
                drawCount: 1,
                stride: DrawIndexedIndirectCommand.SIZE);
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
                DepthTestEnable = true,
                DepthWriteEnable = true,
                DepthCompareOp = CompareOp.Less,
                DepthBoundsTestEnable = false,
                StencilTestEnable = false
            };
            var rasterizer = new PipelineRasterizationStateCreateInfo(
                depthClampEnable: false,
                polygonMode: PolygonMode.Fill,
                cullMode: CullModes.Back,
                frontFace: deviceMesh.GetFrontFace(),
                lineWidth: 1f
            );
            var blending = new PipelineColorBlendStateCreateInfo(
                attachments: new [] 
                { 
                    new PipelineColorBlendAttachmentState(
                        colorWriteMask: ColorComponents.All,
                        blendEnable: false
                    )
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

            //Gather the attribute descriptions
            var vertexAttributeDescriptions = new ResizeArray<VertexInputAttributeDescription>();
            Vertex.AddAttributeDescriptions(binding: 0, vertexAttributeDescriptions);
            InstanceData.AddAttributeDescriptions(binding: 1, vertexAttributeDescriptions);
            
            return logicalDevice.CreateGraphicsPipeline(new GraphicsPipelineCreateInfo(
                layout: pipelineLayout,
                renderPass: renderpass,
                subpass: 0,
                stages: shaderStages,
                inputAssemblyState: deviceMesh.GetInputAssemblyStateInfo(),
                vertexInputState: new PipelineVertexInputStateCreateInfo(
                    vertexBindingDescriptions: new [] 
                    { 
                        new VertexInputBindingDescription(
                            binding: 0,
                            stride: Vertex.SIZE,
                            inputRate: VertexInputRate.Vertex),
                        new VertexInputBindingDescription(
                            binding: 1,
                            stride: InstanceData.SIZE,
                            inputRate: VertexInputRate.Instance)
                    },
                    vertexAttributeDescriptions: vertexAttributeDescriptions.ToArray()),
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
                throw new Exception($"[{nameof(RenderObject)}] Allready disposed");
        }
    }
}