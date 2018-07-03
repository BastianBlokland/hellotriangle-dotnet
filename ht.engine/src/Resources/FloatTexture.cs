using System;

using HT.Engine.Math;
using HT.Engine.Rendering.Memory;
using VulkanCore;

namespace HT.Engine.Resources
{
    public sealed class FloatTexture
    {
        //Properties
        public Int2 Size => new Int2(width, height);
        public int Width => width;
        public int Height => height;
        public int PixelCount => width * height;

        //Data
        private Float4[] pixels; //stored row by row
        private readonly int width;
        private readonly int height;

        public FloatTexture(Float4[] pixels, int width, int height)
        {
            if (pixels == null)
                throw new ArgumentNullException(nameof(pixels));
            if (pixels.Length != width * height)
                throw new ArgumentException(
                    $"[{nameof(FloatTexture)}] Invalid count, expected: {width * height}, got: {pixels.Length}", nameof(pixels));
            this.pixels = pixels;
            this.width = width;
            this.height = height;
        }

        internal void Upload(StagingBuffer stagingBuffer, Image image, ImageAspects aspects)
        {
            if (stagingBuffer == null)
                throw new ArgumentNullException(nameof(stagingBuffer));

            stagingBuffer.Upload(
                data: pixels,
                destination: image,
                subresource: new ImageSubresourceLayers(
                    aspectMask: aspects, mipLevel: 0, baseArrayLayer: 0, layerCount: 1),
                imageExtents: (width, height));
        }
    }
}