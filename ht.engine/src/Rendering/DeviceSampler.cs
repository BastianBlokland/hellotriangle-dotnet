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

        internal DeviceSampler(
            Device logicalDevice,
            int mipLevels = 1,
            bool repeat = false,
            float maxAnisotropy = -1f)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));

            //Create a sampler
            sampler = logicalDevice.CreateSampler(new SamplerCreateInfo {
                MagFilter = Filter.Linear,
                MinFilter = Filter.Linear,
                AddressModeU = repeat ? SamplerAddressMode.Repeat : SamplerAddressMode.ClampToEdge,
                AddressModeV = repeat ? SamplerAddressMode.Repeat : SamplerAddressMode.ClampToEdge,
                AddressModeW = repeat ? SamplerAddressMode.Repeat : SamplerAddressMode.ClampToEdge,
                AnisotropyEnable = maxAnisotropy > 0,
                MaxAnisotropy = maxAnisotropy,
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