using System;

using HT.Engine.Math;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class Texture
    {
        //Helper properties
        internal bool Uploaded => uploaded;
        internal int Width => width;
        internal int Height => height;
        internal Sampler ImageSampler => imageSampler;
        internal ImageView ImageView => imageView;

        //Data
        private Float4[] pixels; //stored row by row
        private readonly int width;
        private readonly int height;

        private bool uploaded;
        private Image image;
        private ImageView imageView;
        private Sampler imageSampler;

        public Texture(Float4[] pixels, int width, int height)
        {
            if (pixels == null)
                throw new ArgumentNullException(nameof(pixels));
            if (pixels.Length != width * height)
                throw new ArgumentException(
                    $"[{nameof(Texture)}] Invalid count, expected: {width * height}, got: {pixels.Length}", nameof(pixels));
            this.pixels = pixels;
            this.width = width;
            this.height = height;
        }

        internal void Upload(
            Device logicalDevice,
            Memory.PoolGroup memoryGroup,
            Memory.StagingBuffer stagingBuffer)
        {
            //TODO: Make this dynamic somehow, as its a pretty big format :)
            var format = Format.R32G32B32A32SFloat;
            var aspects = ImageAspects.Color;

            //Create the image
            image = logicalDevice.CreateImage(new ImageCreateInfo {
                Flags = ImageCreateFlags.None,
                ImageType = ImageType.Image2D,
                Format = format,
                Extent = new Extent3D(width, height, 1),
                MipLevels = 1,
                ArrayLayers = 1,
                Samples = SampleCounts.Count1,
                Tiling = ImageTiling.Optimal,
                Usage = ImageUsages.TransferDst | ImageUsages.Sampled,
                SharingMode = SharingMode.Exclusive,
                InitialLayout = ImageLayout.Undefined});
            
            //Bind memory from our pool to this buffer
            memoryGroup.AllocateAndBind(image);
        
            //Upload the image to the gpu
            stagingBuffer.Upload(
                data: pixels,
                destination: image,
                destinationLayout: ImageLayout.ShaderReadOnlyOptimal,
                subresource: new ImageSubresourceLayers(
                    aspectMask: aspects, mipLevel: 0, baseArrayLayer: 0, layerCount: 1),
                imageExtents: (width, height));

            //Create a image-view
            imageView = image.CreateView(new ImageViewCreateInfo(
                format: format,
                subresourceRange: new ImageSubresourceRange(
                    aspectMask: aspects, baseMipLevel: 0, levelCount: 1, baseArrayLayer: 0, layerCount: 1),
                viewType: ImageViewType.Image2D,
                components: new ComponentMapping(
                    r: ComponentSwizzle.R,
                    g: ComponentSwizzle.G,
                    b: ComponentSwizzle.B,
                    a: ComponentSwizzle.A)));

            //Create a image-sampler
            imageSampler = logicalDevice.CreateSampler(new SamplerCreateInfo {
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

            uploaded = true;
        }

        internal void ClearUpload()
        {
            if (uploaded)
            {
                imageSampler.Dispose();
                imageView.Dispose();
                image.Dispose();
            }
            uploaded = false;
        }

        private void ThrowIfNotUploaded()
        {
            if (!uploaded)
                throw new Exception($"[{nameof(Texture)}] Data has not been upload yet");
        }
    }
}