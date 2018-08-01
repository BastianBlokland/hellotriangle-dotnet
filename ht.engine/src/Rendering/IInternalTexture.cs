using HT.Engine.Math;
using HT.Engine.Rendering;
using HT.Engine.Rendering.Memory;
using HT.Engine.Resources;
using VulkanCore;

namespace HT.Engine.Rendering
{
    internal interface IInternalTexture : ITexture
    {
        Format Format { get; }
        
        int Write(HostBuffer buffer, long offset = 0);

        void Upload(HostBuffer stagingBuffer, TransientExecutor executor, Image image, ImageAspects aspects);
    }
}