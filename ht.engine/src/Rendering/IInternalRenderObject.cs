using VulkanCore;

namespace HT.Engine.Rendering
{
    internal interface IInternalRenderObject : IRenderObject
    {
         void Record(CommandBuffer commandbuffer);
    }
}