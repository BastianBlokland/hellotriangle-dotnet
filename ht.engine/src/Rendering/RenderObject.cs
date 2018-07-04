using System;
using System.Runtime.CompilerServices;
using HT.Engine.Math;
using HT.Engine.Resources;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class RenderObject : IDisposable
    {
        private readonly RenderScene scene;
        private readonly ShaderModule vertModule;
        private readonly ShaderModule fragModule;
        private readonly DeviceMesh deviceMesh;
        private readonly DeviceTexture deviceTexture;
        private readonly DeviceSampler deviceSampler;
        private readonly Memory.DeviceBuffer objectDataBuffer;
        private readonly DescriptorManager.Block descriptorBlock;
        private readonly PipelineLayout pipelineLayout;
        private readonly Pipeline pipeline;
        private bool disposed;

        public RenderObject(
            RenderScene scene,
            Mesh mesh,
            ByteTexture texture,
            ShaderProgram vertProg,
            ShaderProgram fragProg)
        {
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));
            if (vertProg == null)
                throw new ArgumentNullException(nameof(vertProg));
            if (fragProg == null)
                throw new ArgumentNullException(nameof(fragProg));
            this.scene = scene;

            //Create the shader modules
            vertModule = vertProg.CreateModule(scene.LogicalDevice);
            fragModule = fragProg.CreateModule(scene.LogicalDevice);

            //Upload our mesh to the gpu
            deviceMesh = new DeviceMesh(mesh, scene.LogicalDevice, scene.MemoryPool, scene.StagingBuffer);
            deviceTexture = DeviceTexture.UploadTexture(
                texture: texture, 
                logicalDevice: scene.LogicalDevice,
                memoryPool: scene.MemoryPool,
                stagingBuffer: scene.StagingBuffer,
                executor: scene.Executor);
            deviceSampler = new DeviceSampler(scene.LogicalDevice);

            //Allocate a buffer for our object data
            objectDataBuffer = new Memory.DeviceBuffer(
                logicalDevice: scene.LogicalDevice,
                memoryPool: scene.MemoryPool,
                size: Float4x4.SIZE,
                usages: BufferUsages.UniformBuffer);

            //Create the descriptor binding
            var binding = new DescriptorBinding(uniformBufferCount: 1, imageSamplerCount: 1);
            descriptorBlock = scene.DescriptorManager.Allocate(binding);
            descriptorBlock.Update(
                new [] { objectDataBuffer },
                new [] { deviceSampler },
                new [] { deviceTexture });

            //Create the pipeline
            pipelineLayout = scene.LogicalDevice.CreatePipelineLayout(new PipelineLayoutCreateInfo(
                setLayouts: new [] { 
                    descriptorBlock.Layout },
                pushConstantRanges: new [] { 
                    new PushConstantRange(ShaderStages.Vertex, offset: 0, size: SceneData.SIZE) }));

            pipeline = CreatePipeline(scene.LogicalDevice, scene.RenderPass);

            //Create a transformation entry
            Float4x4 viewMatrix =   Float4x4.CreateRotationFromXAngle(FloatUtils.DegreesToRadians(15f)) * 
                                    Float4x4.CreateTranslation((x: 0f, y: -.5f, z: -2));
            Float4x4 projectionMatrix = Float4x4.CreatePerspectiveProjection(
                Frustum.CreateFromVerticalAngleAndAspect(
                    verticalAngle: FloatUtils.DegreesToRadians(45f),
                    aspect: (float)scene.SwapchainSize.X / scene.SwapchainSize.Y,
                    nearDistance: .1f,
                    farDistance: 100f));
            scene.StagingBuffer.Upload(new [] { Float4x4.Identity }, objectDataBuffer);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            deviceMesh.Dispose();
            deviceSampler.Dispose();
            deviceTexture.Dispose();
            objectDataBuffer.Dispose();
            descriptorBlock.Free();
            pipelineLayout.Dispose();
            pipeline.Dispose();
            vertModule.Dispose();
            fragModule.Dispose();
            disposed = true;
        }

        internal void Record(CommandBuffer commandbuffer)
        {
            //Push scene-data
            unsafe
            {
                SceneData sceneData = scene.Data;
                void* dataPointer = Unsafe.AsPointer(ref sceneData);
                commandbuffer.CmdPushConstants(
                    pipelineLayout,
                    ShaderStages.Vertex,
                    offset: 0,
                    size: SceneData.SIZE,
                    values: (IntPtr)dataPointer);
            }

            //Bind data
            deviceMesh.RecordBind(commandbuffer);
            commandbuffer.CmdBindDescriptorSet(
                PipelineBindPoint.Graphics,
                pipelineLayout,
                descriptorBlock.Set);

            //Bind pipeline
            commandbuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);

            //Draw
            deviceMesh.RecordDraw(commandbuffer);
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
            
            return logicalDevice.CreateGraphicsPipeline(new GraphicsPipelineCreateInfo(
                layout: pipelineLayout,
                renderPass: renderpass,
                subpass: 0,
                stages: shaderStages,
                inputAssemblyState: deviceMesh.GetInputAssemblyStateInfo(),
                vertexInputState: deviceMesh.GetVertexInputStateInfo(),
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