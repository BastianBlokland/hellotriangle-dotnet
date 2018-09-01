using System;

using HT.Engine.Math;
using HT.Engine.Resources;
using VulkanCore;

namespace HT.Engine.Rendering
{
    internal sealed class DeviceSampler : IShaderInput, IDisposable
    {
        //Properties
        public DescriptorType DescriptorType => DescriptorType.CombinedImageSampler;
        public Sampler Sampler => sampler;
        public DeviceTexture Texture => texture;

        //Data
        private readonly Sampler sampler;
        private readonly DeviceTexture texture;
        private readonly bool disposeTexture;
        private bool disposed;

        internal DeviceSampler(
            Device logicalDevice,
            DeviceTexture texture,
            bool disposeTexture = true,
            bool repeat = false,
            bool pointFilter = false,
            float maxAnisotropy = -1f)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));

            this.texture = texture;
            this.disposeTexture = disposeTexture;
            sampler = logicalDevice.CreateSampler(new SamplerCreateInfo {
                MagFilter = pointFilter ? Filter.Nearest : Filter.Linear,
                MinFilter = pointFilter ? Filter.Nearest : Filter.Linear,
                AddressModeU = repeat ? SamplerAddressMode.Repeat : SamplerAddressMode.ClampToEdge,
                AddressModeV = repeat ? SamplerAddressMode.Repeat : SamplerAddressMode.ClampToEdge,
                AddressModeW = repeat ? SamplerAddressMode.Repeat : SamplerAddressMode.ClampToEdge,
                AnisotropyEnable = maxAnisotropy > 0,
                MaxAnisotropy = maxAnisotropy,
                CompareEnable = false,
                MipmapMode = SamplerMipmapMode.Linear,
                MipLodBias = 0f,
                MinLod = 0f,
                MaxLod = texture.MipLevels,
                BorderColor = BorderColor.IntOpaqueBlack,
                UnnormalizedCoordinates = false});
        }

        public WriteDescriptorSet CreateDescriptorWrite(DescriptorSet set, int binding)
            => new WriteDescriptorSet(
                dstSet: set,
                dstBinding: binding,
                dstArrayElement: 0,
                descriptorCount: 1,
                descriptorType: DescriptorType,
                imageInfo: new [] {
                    new DescriptorImageInfo(sampler, texture.View, texture.DesiredLayout) });

        public void Dispose()
        {
            ThrowIfDisposed();

            sampler.Dispose();
            if (disposeTexture)
                texture.Dispose();
            disposed = true;
        }
        
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(DeviceSampler)}] Allready disposed");
        }
    }
}