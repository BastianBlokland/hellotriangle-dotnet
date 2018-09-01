using VulkanCore;

namespace HT.Engine.Rendering
{
    internal interface IPushDataProvider
    {
        PushConstantRange[] GetDataRanges();

        void PushData(CommandBuffer commandBuffer, PipelineLayout pipelineLayout);
    }
}