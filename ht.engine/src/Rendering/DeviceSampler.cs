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
        internal ImageView View => deviceTexture.View;

        //Data
        private readonly DeviceTexture deviceTexture;
        private readonly Sampler sampler;
        private bool disposed;

        internal DeviceSampler(
            TextureInfo textureInfo,
            Device logicalDevice,
            Memory.Pool memoryPool,
            Memory.HostBuffer stagingBuffer,
            TransientExecutor executor)
        {
            if (textureInfo.Texture == null)
                throw new ArgumentNullException(nameof(textureInfo));
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (memoryPool == null)
                throw new ArgumentNullException(nameof(memoryPool));
            if (stagingBuffer == null)
                throw new ArgumentNullException(nameof(stagingBuffer));
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));
            
            //Upload the texture
            deviceTexture = DeviceTexture.UploadTexture(
                    textureInfo.Texture as IInternalTexture,
                    generateMipMaps: textureInfo.UseMipMaps,
                    logicalDevice, memoryPool, stagingBuffer, executor);

            //Create a sampler
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
                MaxLod = deviceTexture.MipLevels,
                BorderColor = BorderColor.IntOpaqueBlack,
                UnnormalizedCoordinates = false});
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            deviceTexture.Dispose();
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