using System;
using HT.Engine.Math;
using HT.Engine.Rendering;
using HT.Engine.Rendering.Memory;
using HT.Engine.Resources;

using VulkanCore;

namespace HT.Engine.Rendering
{
    internal interface IShaderInput : IDisposable
    {
        DescriptorType DescriptorType { get; }
        
        WriteDescriptorSet CreateDescriptorWrite(DescriptorSet set, int binding);
    }
}