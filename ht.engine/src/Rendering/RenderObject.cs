using System;

using HT.Engine.Math;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class RenderObject : IDisposable
    {
        private readonly ShaderProgram vertProg;
        private readonly ShaderProgram fragProg;
        private readonly Vertex[] vertices;

        private bool initialized;
        private PipelineLayout pipelineLayout;
        private Pipeline pipeline;
        private VulkanCore.Buffer vertexBuffer;
        private DeviceMemory vertexBufferMemory;

        public RenderObject(ShaderProgram vertProg, ShaderProgram fragProg)
        {
            if (vertProg == null)
                throw new ArgumentNullException(nameof(vertProg));
            if (fragProg == null)
                throw new ArgumentNullException(nameof(fragProg));

            this.vertProg = vertProg;
            this.fragProg = fragProg;
            this.vertices = new []
            {
                new Vertex(position: (-.9f, .9f, 0f), color: ColorUtils.Green),
                new Vertex(position: (-.9f, -.9f, 0f), color: ColorUtils.Red),
                new Vertex(position: (.9f, .9f, 0f), color: ColorUtils.Aqua),
                new Vertex(position: (.9f, -.9f, 0f), color: ColorUtils.Fuchsia)
            };
        }

        public void Dispose()
        {
            if(initialized)
                Deinitialize();
        }

        internal void Initialize(Device logicalDevice, HostDevice hostDevice, RenderPass renderpass)
        {
            if (initialized)
                throw new Exception(
                    $"[{nameof(RenderObject)}] Allready initialized");

            CreatePipeline(logicalDevice, renderpass);
            CreateVertexBuffer(logicalDevice, hostDevice);
            initialized = true;
        }

        internal void Record(CommandBuffer commandbuffer)
        {
            commandbuffer.CmdBindVertexBuffer(vertexBuffer, offset: 0);
            commandbuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);

            commandbuffer.CmdDraw(
                vertexCount: vertices.Length,
                instanceCount: 1,
                firstVertex: 0,
                firstInstance: 0);
        }

        internal void Deinitialize()
        {
            if (!initialized)
                throw new Exception(
                    $"[{nameof(RenderObject)}] Unable to deinitialize as we haven't initialized");
                
            vertexBufferMemory.Dispose();
            vertexBuffer.Dispose();
            pipelineLayout.Dispose();
            pipeline.Dispose();
            initialized = false;
        }

        private void CreatePipeline(Device logicalDevice, RenderPass renderpass)
        {
            //Create the pipeline layout (empty atm as we have no dynamic state yet)
            pipelineLayout = logicalDevice.CreatePipelineLayout(new PipelineLayoutCreateInfo());

            ShaderModule vertModule = vertProg.CreateModule(logicalDevice);
            ShaderModule fragModule = fragProg.CreateModule(logicalDevice);

            var shaderStages = new []
            {
                new PipelineShaderStageCreateInfo(
                    stage: ShaderStages.Vertex, module: vertModule, name: "main"),
                new PipelineShaderStageCreateInfo(
                    stage: ShaderStages.Fragment, module: fragModule, name: "main")
            };
            var vertexInput = new PipelineVertexInputStateCreateInfo(
                vertexBindingDescriptions: new [] { Vertex.GetBindingDescription() }, 
                vertexAttributeDescriptions: Vertex.GetAttributeDescriptions() 
            );
            var inputAssembly = new PipelineInputAssemblyStateCreateInfo(
                topology: PrimitiveTopology.TriangleStrip,
                primitiveRestartEnable: false
            );
            var rasterizer = new PipelineRasterizationStateCreateInfo(
                depthClampEnable: false,
                polygonMode: PolygonMode.Fill,
                cullMode: CullModes.Back,
                frontFace: FrontFace.Clockwise,
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
                inputAssemblyState: inputAssembly,
                vertexInputState: vertexInput,
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

        private void CreateVertexBuffer(Device logicalDevice, HostDevice hostDevice)
        {
            //Create vertex buffer
            vertexBuffer = logicalDevice.CreateBuffer(new BufferCreateInfo(
                size: Vertex.SIZE * vertices.Length,
                usages: BufferUsages.VertexBuffer,
                flags: BufferCreateFlags.None,
                sharingMode: SharingMode.Exclusive
            ));

            //Get the memory requirements for the vertex buffer
            MemoryRequirements memRequirements = vertexBuffer.GetMemoryRequirements();

            //Allocate the memory on the gpu
            vertexBufferMemory = logicalDevice.AllocateMemory(new MemoryAllocateInfo(
                allocationSize: memRequirements.Size,
                memoryTypeIndex: hostDevice.GetMemoryType(
                    supportedTypesBits: memRequirements.MemoryTypeBits,
                    properties: MemoryProperties.HostVisible | MemoryProperties.HostCoherent)
            ));

            //Bind the allocated memory to the vertexbuffer
            vertexBuffer.BindMemory(vertexBufferMemory, memoryOffset: 0);

            //Write out vertices to the buffer
            IntPtr hostPointer = vertexBufferMemory.Map(offset: 0, size: memRequirements.Size);
            Interop.Write(hostPointer, vertices);
            vertexBufferMemory.Unmap();
        }

        private void ThrowIfNotInitialized()
        {
            if (!initialized)
                throw new Exception($"[{nameof(RenderObject)}] Not yet initialized");
        }
    }
}