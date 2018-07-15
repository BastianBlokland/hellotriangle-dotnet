using VulkanCore;

namespace HT.Engine.Rendering.Memory
{
    internal interface IBuffer
    {
        VulkanCore.Buffer VulkanBuffer { get; }
        long Size { get; }
    }
}