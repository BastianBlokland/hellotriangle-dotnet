using System;

using HT.Engine.Math;
using HT.Engine.Rendering;
using HT.Engine.Rendering.Memory;
using VulkanCore;

namespace HT.Engine.Resources
{
    public sealed class ByteTexture
    {
        //Properties
        public Int2 Size => new Int2(width, height);
        public int Width => width;
        public int Height => height;
        public int PixelCount => width * height;

        //Data
        private Byte4[] pixels; //stored row by row
        private readonly int width;
        private readonly int height;

        public ByteTexture(Byte4[] pixels, int width, int height)
        {
            if (pixels == null)
                throw new ArgumentNullException(nameof(pixels));
            if (pixels.Length != width * height)
                throw new ArgumentException(
                    $"[{nameof(ByteTexture)}] Invalid count, expected: {width * height}, got: {pixels.Length}", nameof(pixels));
            this.pixels = pixels;
            this.width = width;
            this.height = height;
        }

        internal void Upload(
            HostBuffer stagingBuffer,
            TransientExecutor executor,
            Image image,
            ImageAspects aspects)
        {
            if (stagingBuffer == null)
                throw new ArgumentNullException(nameof(stagingBuffer));
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));

            //Write to our staging buffer
            stagingBuffer.Write(pixels);

            //Copy our staging buffer to the image
            executor.ExecuteBlocking(commandBuffer =>
            {
                commandBuffer.CmdCopyBufferToImage(
                    srcBuffer: stagingBuffer.Buffer,
                    dstImage: image,
                    dstImageLayout: ImageLayout.TransferDstOptimal,
                    regions: new BufferImageCopy {
                        BufferOffset = 0,
                        BufferRowLength = 0,
                        BufferImageHeight = 0,
                        ImageSubresource = new ImageSubresourceLayers(
                            aspectMask: aspects, mipLevel: 0, baseArrayLayer: 0, layerCount: 1),
                        ImageOffset = new Offset3D(x: 0, y: 0, z: 0),
                        ImageExtent = new Extent3D(
                            width: width,
                            height: height,
                            depth: 1)});
            });
        }
    }
}