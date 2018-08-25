using System;

using VulkanCore;

namespace HT.Engine.Rendering
{
    internal interface IInternalRenderObject : IRenderObject
    {
        ReadOnlySpan<IShaderInput> Inputs { get; }

        FrontFace GetFrontFace();
        PipelineInputAssemblyStateCreateInfo GetInputAssemblyStateInfo();
        PipelineVertexInputStateCreateInfo GetVertexInputState();

        void Record(CommandBuffer commandbuffer);
    }
}