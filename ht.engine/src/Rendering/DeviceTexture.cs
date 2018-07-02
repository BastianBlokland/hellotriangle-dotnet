using System;

using HT.Engine.Math;
using HT.Engine.Resources;
using VulkanCore;

namespace HT.Engine.Rendering
{
    //GPU size representation of a texture
    //NOTE: Does not hold on to the cpu representation of the texture so it can be garbage collected
    //TODO: Sampler createion can be moved out of here so the texture can be used with different
    //sampler as they are not really tied together
    internal sealed class DeviceTexture : IDisposable
    {
        //Properties
        internal ImageView View => view;
        internal Sampler Sampler => sampler;

        //Data
        private readonly Format format;
        private readonly ImageAspects aspects;
        private readonly Image image;
        private readonly Memory.Block memory;
        private readonly ImageView view;
        private readonly Sampler sampler;
        private bool disposed;

        internal DeviceTexture(
            ByteTexture texture, 
            Device logicalDevice,
            Memory.Pool memoryPool,
            Memory.StagingBuffer stagingBuffer)
        {
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (memoryPool == null)
                throw new ArgumentNullException(nameof(memoryPool));
            if (stagingBuffer == null)
                throw new ArgumentNullException(nameof(stagingBuffer));
            
            format = Format.R8G8B8A8UNorm;
            aspects = ImageAspects.Color;
            image = CreateImage(logicalDevice, format, texture.Size);
            memory = memoryPool.AllocateAndBind(image);
            texture.Upload(stagingBuffer, image, aspects);
            view = CreateView(image, format, aspects);
            sampler = CreateSampler(logicalDevice);
        }

        internal DeviceTexture(
            FloatTexture texture, 
            Device logicalDevice,
            Memory.Pool memoryPool,
            Memory.StagingBuffer stagingBuffer)
        {
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (memoryPool == null)
                throw new ArgumentNullException(nameof(memoryPool));
            if (stagingBuffer == null)
                throw new ArgumentNullException(nameof(stagingBuffer));
            
            format = Format.R32G32B32A32SFloat;
            aspects = ImageAspects.Color;
            image = CreateImage(logicalDevice, format, texture.Size);
            memory = memoryPool.AllocateAndBind(image);
            texture.Upload(stagingBuffer, image, aspects);
            view = CreateView(image, format, aspects);
            sampler = CreateSampler(logicalDevice);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            sampler.Dispose();
            view.Dispose();
            image.Dispose();
            memory.Free();

            disposed = true;
        }

        private static Image CreateImage(Device logicalDevice, Format format, Int2 size)
            => logicalDevice.CreateImage(new ImageCreateInfo {
                Flags = ImageCreateFlags.None,
                ImageType = ImageType.Image2D,
                Format = format,
                Extent = new Extent3D(size.X, size.Y, 1),
                MipLevels = 1,
                ArrayLayers = 1,
                Samples = SampleCounts.Count1,
                Tiling = ImageTiling.Optimal,
                Usage = ImageUsages.TransferDst | ImageUsages.Sampled,
                SharingMode = SharingMode.Exclusive,
                InitialLayout = ImageLayout.Undefined});

        private static ImageView CreateView(Image image, Format format, ImageAspects aspects)
            => image.CreateView(new ImageViewCreateInfo(
                format: format,
                subresourceRange: new ImageSubresourceRange(
                    aspectMask: aspects, baseMipLevel: 0, levelCount: 1, baseArrayLayer: 0, layerCount: 1),
                viewType: ImageViewType.Image2D,
                components: new ComponentMapping(
                    r: ComponentSwizzle.R,
                    g: ComponentSwizzle.G,
                    b: ComponentSwizzle.B,
                    a: ComponentSwizzle.A)));

        private static Sampler CreateSampler(Device logicalDevice)
            => logicalDevice.CreateSampler(new SamplerCreateInfo {
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
                MaxLod = 0f,
                BorderColor = BorderColor.IntOpaqueBlack,
                UnnormalizedCoordinates = false});

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(DeviceTexture)}] Allready disposed");
        }
    }
}