using System;

using HT.Engine.Math;
using HT.Engine.Rendering;
using HT.Engine.Rendering.Memory;
using VulkanCore;

namespace HT.Engine.Resources
{
    public sealed class Float4Texture : IInternalTexture
    {
        //Properties
        public Int2 Size => size;
        public Format Format => Format.R32G32B32A32SFloat;
        public bool IsCubeMap => false;

        //Data
        private readonly ReadOnlyMemory<Float4> pixels; //stored row by row
        private readonly Int2 size;

        public Float4Texture(ReadOnlyMemory<Float4> pixels, Int2 size)
        {
            if (pixels.Length == 0)
                throw new ArgumentException($"[{nameof(Float4Texture)}] No pixels provided", nameof(pixels));
            if (pixels.Length != size.X * size.Y)
                throw new ArgumentException(
                    $"[{nameof(Float4Texture)}] Invalid count, expected: {size.X * size.Y}, got: {pixels.Length}", nameof(pixels));
            this.pixels = pixels;
            this.size = size;
        }

        int IInternalTexture.Write(HostBuffer buffer, long offset)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            return buffer.Write(pixels, offset);
        }

        void IInternalTexture.Upload(
            HostBuffer stagingBuffer,
            TransientExecutor executor,
            Image image,
            ImageAspects aspects)
        {
            if (stagingBuffer == null)
                throw new ArgumentNullException(nameof(stagingBuffer));
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));

            //Write to the staging buffer
            stagingBuffer.Write(pixels, offset: 0);

            //Copy the staging buffer to the image
            executor.ExecuteBlocking(commandBuffer =>
            {
                commandBuffer.CmdCopyBufferToImage(
                    srcBuffer: stagingBuffer.VulkanBuffer,
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
                            width: size.X,
                            height: size.Y,
                            depth: 1)});
            });
        }
    }
}