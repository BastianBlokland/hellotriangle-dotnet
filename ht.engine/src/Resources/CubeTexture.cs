using System;

using HT.Engine.Math;
using HT.Engine.Rendering;
using HT.Engine.Rendering.Memory;
using VulkanCore;

namespace HT.Engine.Resources
{
    public sealed class CubeTexture : IInternalTexture
    {
        //Properties
        public Int2 Size => size;
        public Format Format => faces[0].Format;
        public bool IsCubeMap => true;

        //Data
        private readonly IInternalTexture[] faces;
        private readonly Int2 size;

        public CubeTexture(
            ITexture left,
            ITexture right,
            ITexture up,
            ITexture down,
            ITexture front,
            ITexture back,
            Int2 size) : this(
                left as IInternalTexture, 
                right as IInternalTexture, 
                up as IInternalTexture,
                down as IInternalTexture,
                front as IInternalTexture,
                back as IInternalTexture,
                size) {}

        internal CubeTexture(
            IInternalTexture left,
            IInternalTexture right,
            IInternalTexture up,
            IInternalTexture down,
            IInternalTexture front,
            IInternalTexture back,
            Int2 size)
        {
            if (left == null)
                throw new ArgumentNullException(nameof(left));
            if (right == null)
                throw new ArgumentNullException(nameof(right));
            if (up == null)
                throw new ArgumentNullException(nameof(up));
            if (down == null)
                throw new ArgumentNullException(nameof(down));
            if (front == null)
                throw new ArgumentNullException(nameof(front));
            if (back == null)
                throw new ArgumentNullException(nameof(back));
            if (left.Size != size || right.Size != size ||
                up.Size != size || down.Size != size ||
                front.Size != size || back.Size != size)
            {
                throw new ArgumentException(
                    $"[{nameof(CubeTexture)}] All faces of the cube-map need to match the given size",
                    nameof(size));
            }
            Format format = left.Format;
            if (right.Format != format ||
                up.Format != format || down.Format != format ||
                front.Format != format || back.Format != format)
            {
                throw new ArgumentException(
                    $"[{nameof(CubeTexture)}] All faces of the cube-map need to have the same format",
                    nameof(size));
            }
            faces = new IInternalTexture[6];
            faces[0] = left;
            faces[1] = right;
            faces[2] = up;
            faces[3] = down;
            faces[4] = front;
            faces[5] = back;
            this.size = size;
        }

        int IInternalTexture.Write(HostBuffer buffer, long offset)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            int written = 0;
            for (int i = 0; i < faces.Length; i++)
                written += faces[i].Write(buffer, offset + written);
            return written;
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

            //Write all the faces to the staging buffer and create copy commands
            long offset = 0;
            BufferImageCopy[] copyRegions = new BufferImageCopy[faces.Length];
            for (int i = 0; i < faces.Length; i++)
            {
                copyRegions[i] = new BufferImageCopy
                {
                    BufferOffset = offset,
                    BufferRowLength = 0,
                    BufferImageHeight = 0,
                    ImageSubresource = new ImageSubresourceLayers(
                            aspectMask: aspects, mipLevel: 0, baseArrayLayer: i, layerCount: 1),
                    ImageOffset = new Offset3D(x: 0, y: 0, z: 0),
                    ImageExtent = new Extent3D(
                        width: size.X,
                        height: size.Y,
                        depth: 1)
                };
                offset += faces[i].Write(stagingBuffer, offset);
            }

            //Copy our staging buffer to the image
            executor.ExecuteBlocking(commandBuffer =>
            {
                commandBuffer.CmdCopyBufferToImage(
                    srcBuffer: stagingBuffer.VulkanBuffer,
                    dstImage: image,
                    dstImageLayout: ImageLayout.TransferDstOptimal,
                    regions: copyRegions);
            });
        }
    }
}