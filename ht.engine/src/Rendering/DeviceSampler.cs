using System;

using HT.Engine.Math;
using HT.Engine.Resources;
using VulkanCore;

namespace HT.Engine.Rendering
{
    internal sealed class DeviceSampler : IDisposable
    {
        //Properties
        internal Sampler Sampler => sampler;

        //Data
        private readonly Sampler sampler;
        private bool disposed;

        internal DeviceSampler(Device logicalDevice, int mipLevels)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            
            sampler = logicalDevice.CreateSampler(new SamplerCreateInfo {
                MagFilter = Filter.Linear,
                MinFilter = Filter.Linear,
                AddressModeU = SamplerAddressMode.Repeat,
                AddressModeV = SamplerAddressMode.Repeat,
                AddressModeW = SamplerAddressMode.Repeat,
                AnisotropyEnable = true,
                MaxAnisotropy = 8f,
                CompareEnable = false,
                MipmapMode = SamplerMipmapMode.Linear,
                MipLodBias = 0f,
                MinLod = 0f,
                MaxLod = mipLevels,
                BorderColor = BorderColor.IntOpaqueBlack,
                UnnormalizedCoordinates = false});
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            sampler.Dispose();
            disposed = true;
        }
        
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(DeviceSampler)}] Allready disposed");
        }
    }
}