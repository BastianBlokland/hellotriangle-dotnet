using System;

using HT.Engine.Math;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class RenderObject : IDisposable
    {
        private readonly Model.Mesh mesh;
        private readonly Texture texture;
        private readonly ShaderProgram vertProg;
        private readonly ShaderProgram fragProg;

        private bool initialized;
        private Memory.StagingBuffer stagingBuffer;
        private Memory.DeviceBuffer transformationBuffer;
        private DescriptorSetLayout descriptorSetLayout;
        private DescriptorSet descriptorSet;
        private PipelineLayout pipelineLayout;
        private Pipeline pipeline;
        private float yAngle;

        public RenderObject(
            Model.Mesh mesh,
            Texture texture,
            ShaderProgram vertProg,
            ShaderProgram fragProg)
        {
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));
            if (vertProg == null)
                throw new ArgumentNullException(nameof(vertProg));
            if (fragProg == null)
                throw new ArgumentNullException(nameof(fragProg));
            this.mesh = mesh;
            this.texture = texture;
            this.vertProg = vertProg;
            this.fragProg = fragProg;
        }

        public void Dispose()
        {
            if (initialized)
                Deinitialize();
        }

        internal void Initialize(
            Device logicalDevice,
            HostDevice hostDevice,
            DescriptorPool descriptorPool,
            RenderPass renderpass,
            Memory.PoolGroup memoryGroup,
            Memory.StagingBuffer stagingBuffer)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (hostDevice == null)
                throw new ArgumentNullException(nameof(hostDevice));
            if (descriptorPool == null)
                throw new ArgumentNullException(nameof(descriptorPool));
            if (renderpass == null)
                throw new ArgumentNullException(nameof(renderpass));
            if (memoryGroup == null)
                throw new ArgumentNullException(nameof(memoryGroup));
            if (stagingBuffer == null)
                throw new ArgumentNullException(nameof(stagingBuffer));
            if (initialized)
                throw new Exception(
                    $"[{nameof(RenderObject)}] Allready initialized");
            
            this.stagingBuffer = stagingBuffer;

            //Upload our mesh to the gpu
            if (!mesh.Uploaded)
                mesh.Upload(logicalDevice, memoryGroup, stagingBuffer);

            //Upload our texture to the gpu
            if (!texture.Uploaded)
                texture.Upload(logicalDevice, memoryGroup, stagingBuffer);

            //Allocate a buffer for our transformation
            transformationBuffer = new Memory.DeviceBuffer(
                logicalDevice: logicalDevice,
                memoryGroup: memoryGroup,
                size: Transformation.SIZE,
                usages: BufferUsages.UniformBuffer);

            CreateDescriptorSet(logicalDevice, descriptorPool);

            //Create the pipeline
            CreatePipeline(logicalDevice, renderpass);

            initialized = true;
        }

        internal void Record(CommandBuffer commandbuffer)
        {
            //Bind data
            mesh.RecordBind(commandbuffer);
            commandbuffer.CmdBindDescriptorSet(
                PipelineBindPoint.Graphics,
                pipelineLayout,
                descriptorSet);

            //Bind pipeline
            commandbuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);

            //Draw
            mesh.RecordDraw(commandbuffer);
        }

        internal void Update(Float4x4 viewMatrix, Float4x4 projectionMatrix)
        {
            ThrowIfNotInitialized();

            yAngle += FloatUtils.DegreesToRadians(.5f);
            Transformation trans = new Transformation(
                model: Float4x4.CreateRotationFromYAngle(yAngle),
                view: viewMatrix,
                projection: projectionMatrix);
            stagingBuffer.Upload(new [] { trans }, transformationBuffer);
        }

        internal void Deinitialize()
        {
            if (!initialized)
                throw new Exception(
                    $"[{nameof(RenderObject)}] Unable to deinitialize as we haven't initialized");
            
            mesh.ClearUpload();
            texture.ClearUpload();
            transformationBuffer.Dispose();
            descriptorSetLayout.Dispose();
            pipelineLayout.Dispose();
            pipeline.Dispose();
            initialized = false;
        }

        private void CreateDescriptorSet(Device logicalDevice, DescriptorPool descriptorPool)
        {
            //Create layout
            descriptorSetLayout = logicalDevice.CreateDescriptorSetLayout(
                new DescriptorSetLayoutCreateInfo(
                    bindings: new []
                    { 
                        new DescriptorSetLayoutBinding(
                            binding: 0,
                            descriptorType: DescriptorType.UniformBuffer,
                            descriptorCount: 1,
                            stageFlags: ShaderStages.Vertex),
                        new DescriptorSetLayoutBinding(
                            binding: 1,
                            descriptorType: DescriptorType.CombinedImageSampler,
                            descriptorCount: 1,
                            stageFlags: ShaderStages.Fragment) 
                    }));

            //Allocate the set
            descriptorSet = descriptorPool.AllocateSets(new DescriptorSetAllocateInfo(
                    descriptorSetCount: 1,
                    setLayouts: descriptorSetLayout))[0];

            //Bind data to the set
            descriptorPool.UpdateSets(descriptorWrites: new []
            {
                new WriteDescriptorSet(
                    dstSet: descriptorSet,
                    dstBinding: 0,
                    dstArrayElement: 0,
                    descriptorCount: 1,
                    descriptorType: DescriptorType.UniformBuffer,
                    bufferInfo: new [] { new DescriptorBufferInfo(
                            buffer: transformationBuffer.Buffer,
                            offset: 0,
                            range: transformationBuffer.Size) }),
                new WriteDescriptorSet(
                    dstSet: descriptorSet,
                    dstBinding: 1,
                    dstArrayElement: 0,
                    descriptorCount: 1,
                    descriptorType: DescriptorType.CombinedImageSampler,
                    imageInfo: new [] { new DescriptorImageInfo(
                        sampler: texture.ImageSampler,
                        imageView: texture.ImageView,
                        imageLayout: ImageLayout.ShaderReadOnlyOptimal) })
            }, descriptorCopies: null);
        }

        private void CreatePipeline(Device logicalDevice, RenderPass renderpass)
        {
            //Create the pipeline layout (empty atm as we have no dynamic state yet)
            pipelineLayout = logicalDevice.CreatePipelineLayout(new PipelineLayoutCreateInfo(
                setLayouts: new [] { descriptorSetLayout }));

            ShaderModule vertModule = vertProg.CreateModule(logicalDevice);
            ShaderModule fragModule = fragProg.CreateModule(logicalDevice);

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
                frontFace: mesh.GetFrontFace(),
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
            
            //Create the pipeline
            pipeline = logicalDevice.CreateGraphicsPipeline(new GraphicsPipelineCreateInfo(
                layout: pipelineLayout,
                renderPass: renderpass,
                subpass: 0,
                stages: shaderStages,
                inputAssemblyState: mesh.GetInputAssemblyStateInfo(),
                vertexInputState: mesh.GetVertexInputStateInfo(),
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
            
            //After pipeline creation we no longer need the shader modules
            vertModule.Dispose();
            fragModule.Dispose();
        }

        private void ThrowIfNotInitialized()
        {
            if (!initialized)
                throw new Exception($"[{nameof(RenderObject)}] Not yet initialized");
        }
    }
}