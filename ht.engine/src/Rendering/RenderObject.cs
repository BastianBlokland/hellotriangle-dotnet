using System;

using HT.Engine.Math;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class RenderObject : IDisposable
    {
        private readonly Model.Mesh mesh;
        private readonly ShaderProgram vertProg;
        private readonly ShaderProgram fragProg;

        private bool initialized;
        private Memory.Pool transformationMemoryPool;
        private Memory.Region transformationMemoryRegion;
        private DescriptorSetLayout descriptorSetLayout;
        private DescriptorSet descriptorSet;
        private PipelineLayout pipelineLayout;
        private Pipeline pipeline;
        private float yAngle;

        public RenderObject(Model.Mesh mesh, ShaderProgram vertProg, ShaderProgram fragProg)
        {
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));
            if (vertProg == null)
                throw new ArgumentNullException(nameof(vertProg));
            if (fragProg == null)
                throw new ArgumentNullException(nameof(fragProg));
            this.mesh = mesh;
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
            Memory.Pool meshPool,
            Memory.Pool tranformationPool)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (hostDevice == null)
                throw new ArgumentNullException(nameof(hostDevice));
            if (descriptorPool == null)
                throw new ArgumentNullException(nameof(descriptorPool));
            if (renderpass == null)
                throw new ArgumentNullException(nameof(renderpass));
            if (meshPool == null)
                throw new ArgumentNullException(nameof(meshPool));
            if (tranformationPool == null)
                throw new ArgumentNullException(nameof(tranformationPool));
            if (initialized)
                throw new Exception(
                    $"[{nameof(RenderObject)}] Allready initialized");

            //Upload our mesh to the gpu
            if (!mesh.Uploaded)
                mesh.Upload(meshPool);

            //Allocate a region for our transformation
            transformationMemoryPool = tranformationPool;
            transformationMemoryRegion = tranformationPool.Allocate<Transformation>(count: 1);

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
            transformationMemoryPool.Write(new [] { trans }, transformationMemoryRegion);
        }

        internal void Deinitialize()
        {
            if (!initialized)
                throw new Exception(
                    $"[{nameof(RenderObject)}] Unable to deinitialize as we haven't initialized");
            
            descriptorSetLayout.Dispose();
            pipelineLayout.Dispose();
            pipeline.Dispose();
            initialized = false;
        }

        private void CreateDescriptorSet(Device logicalDevice, DescriptorPool descriptorPool)
        {
            //Create layout
            var transformationBinding = new DescriptorSetLayoutBinding(
                binding: 0,
                descriptorType: DescriptorType.UniformBuffer,
                descriptorCount: 1,
                stageFlags: ShaderStages.Vertex);

            descriptorSetLayout = logicalDevice.CreateDescriptorSetLayout(
                new DescriptorSetLayoutCreateInfo(bindings: transformationBinding));

            //Allocate the set
            descriptorSet = descriptorPool.AllocateSets(new DescriptorSetAllocateInfo(
                    descriptorSetCount: 1,
                    setLayouts: descriptorSetLayout))[0];

            descriptorPool.UpdateSets(descriptorWrites: new []
            {
                new WriteDescriptorSet(
                    dstSet: descriptorSet,
                    dstBinding: 0,
                    dstArrayElement: 0,
                    descriptorCount: 1,
                    descriptorType: DescriptorType.UniformBuffer,
                    bufferInfo: new [] { new DescriptorBufferInfo(
                        buffer: transformationMemoryPool.Buffer,
                        offset: transformationMemoryRegion.Offset,
                        range: transformationMemoryRegion.RequestedSize) })
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
                depthStencilState: null,
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