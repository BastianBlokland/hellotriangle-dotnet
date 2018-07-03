using System;

using HT.Engine.Math;
using HT.Engine.Rendering.Memory;
using HT.Engine.Resources;
using VulkanCore;

namespace HT.Engine.Rendering
{
    //GPU size representation of a texture
    //NOTE: Does not hold on to the cpu representation of the texture so it can be garbage collected
    internal sealed class DeviceTexture : IDisposable
    {
        public static Format DepthFormat = Format.D32SFloat;
        public static Format ByteTextureFormat = Format.R8G8B8A8UNorm;
        public static Format FloatTextureFormat = Format.R32G32B32A32SFloat;

        //Properties
        internal ImageView View => view;

        //Data
        private readonly Format format;
        private readonly ImageAspects aspects;
        private readonly Image image;
        private readonly Memory.Block memory;
        private readonly ImageView view;
        private bool disposed;

        internal static DeviceTexture UploadTexture(
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
            
            var aspects = ImageAspects.Color;
            var image = CreateImage(
                logicalDevice, ByteTextureFormat, texture.Size, ImageUsages.TransferDst | ImageUsages.Sampled);
            var memory = memoryPool.AllocateAndBind(image);
            texture.Upload(stagingBuffer, image, aspects);
            var view = CreateView(image, ByteTextureFormat, aspects);

            return new DeviceTexture(ByteTextureFormat, aspects, image, memory, view);
        }

        internal static DeviceTexture UploadTexture(
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
            
            var aspects = ImageAspects.Color;
            var image = CreateImage(
                logicalDevice, FloatTextureFormat, texture.Size, ImageUsages.TransferDst | ImageUsages.Sampled);
            var memory = memoryPool.AllocateAndBind(image);
            texture.Upload(stagingBuffer, image, aspects);
            var view = CreateView(image, FloatTextureFormat, aspects);

            return new DeviceTexture(FloatTextureFormat, aspects, image, memory, view);
        }

        internal static DeviceTexture CreateDepthTexture(
            Int2 size,
            Device logicalDevice,
            Memory.Pool memoryPool,
            Copier copier)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (memoryPool == null)
                throw new ArgumentNullException(nameof(memoryPool));
            if (copier == null)
                throw new ArgumentNullException(nameof(copier));

            var aspects = ImageAspects.Depth;
            var image = CreateImage(logicalDevice, DepthFormat, size, ImageUsages.DepthStencilAttachment);
            var memory = memoryPool.AllocateAndBind(image);
            var view = CreateView(image, DepthFormat, aspects);

            //Transition the image to the depth layout
            copier.TransitionImageLayout(
                image: image,
                subresource: new ImageSubresourceLayers(ImageAspects.Depth, mipLevel: 0, baseArrayLayer: 0, layerCount: 1),
                oldLayout: ImageLayout.Undefined,
                newLayout: ImageLayout.DepthStencilAttachmentOptimal);

            return new DeviceTexture(DepthFormat, aspects, image, memory, view);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            view.Dispose();
            image.Dispose();
            memory.Free();

            disposed = true;
        }

        private DeviceTexture(
            Format format,
            ImageAspects aspects,
            Image image,
            Block memory,
            ImageView view)
        {
            this.format = format;
            this.aspects = aspects;
            this.image = image;
            this.memory = memory;
            this.view = view;
        }

        private static Image CreateImage(Device logicalDevice, Format format, Int2 size, ImageUsages usage)
            => logicalDevice.CreateImage(new ImageCreateInfo {
                Flags = ImageCreateFlags.None,
                ImageType = ImageType.Image2D,
                Format = format,
                Extent = new Extent3D(size.X, size.Y, 1),
                MipLevels = 1,
                ArrayLayers = 1,
                Samples = SampleCounts.Count1,
                Tiling = ImageTiling.Optimal,
                Usage = usage,
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
        
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(DeviceTexture)}] Allready disposed");
        }
    }
}