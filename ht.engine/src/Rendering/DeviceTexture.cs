using System;

using HT.Engine.Math;
using HT.Engine.Rendering.Memory;
using HT.Engine.Resources;
using VulkanCore;

using static System.Math;

namespace HT.Engine.Rendering
{
    //GPU size representation of a texture
    //NOTE: Does not hold on to the cpu representation of the texture so it can be garbage collected
    internal sealed class DeviceTexture : IDisposable
    {
        //Properties
        internal ImageView View => view;
        internal int MipLevels => mipLevels;

        //Data
        private readonly Format format;
        private readonly int mipLevels;
        private readonly ImageAspects aspects;
        private readonly Image image;
        private readonly Memory.Block memory;
        private readonly ImageView view;
        private bool disposed;

        internal static DeviceTexture UploadTexture(
            IInternalTexture texture,
            bool generateMipMaps,
            Device logicalDevice,
            Memory.Pool memoryPool,
            Memory.HostBuffer stagingBuffer,
            TransientExecutor executor)
        {
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (memoryPool == null)
                throw new ArgumentNullException(nameof(memoryPool));
            if (stagingBuffer == null)
                throw new ArgumentNullException(nameof(stagingBuffer));
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));
            
            int mipLevels = generateMipMaps ? CalculateMipLevels(texture.Size) : 1;
            int layers = texture.IsCubeMap ? 6 : 1;
            var aspects = ImageAspects.Color;
            var image = CreateImage(
                logicalDevice,
                texture.Format,
                SampleCounts.Count1,
                texture.Size,
                mipLevels,
                //Also include 'TransferSrc' because we read from the image to generate the mip-maps
                ImageUsages.TransferSrc | ImageUsages.TransferDst | ImageUsages.Sampled,
                cubeMap: texture.IsCubeMap);
            var memory = memoryPool.AllocateAndBind(image, Chunk.Location.Device);
            
            //Transition the entire image (all mip-levels and layers) to 'TransferDstOptimal'
            TransitionImageLayout(image, aspects,
                baseMipLevel: 0, mipLevels, baseLayer: 0, layers,
                oldLayout: ImageLayout.Undefined,
                newLayout: ImageLayout.TransferDstOptimal,
                executor: executor);

            //Upload the data to mipmap 0
            texture.Upload(stagingBuffer, executor, image, aspects);
            
            //Create the other mipmap levels
            Int2 prevMipSize = texture.Size;
            for (int i = 1; i < mipLevels; i++)
            {
                Int2 curMipSize = new Int2(
                    prevMipSize.X > 1 ? prevMipSize.X / 2 : 1,
                    prevMipSize.Y > 1 ? prevMipSize.Y / 2 : 1);

                //Move the previous mip-level to a transfer source layout
                TransitionImageLayout(image, aspects,
                    baseMipLevel: i - 1, mipLevels: 1,
                    baseLayer: 0, layers,
                    oldLayout: ImageLayout.TransferDstOptimal,
                    newLayout: ImageLayout.TransferSrcOptimal,
                    executor: executor);

                //Blit the previous mip-level to the current at half the size
                BlitImage(aspects,
                    fromImage: image,
                    fromRegion: new IntRect(min: Int2.Zero, max: prevMipSize),
                    fromMipLevel: i - 1,
                    fromLayerCount: layers,
                    toImage: image,
                    toRegion: new IntRect(min: Int2.Zero, max: curMipSize),
                    toMipLevel: i,
                    toLayerCount: layers,
                    executor);

                //Transition the previous mip-level to the shader-read layout
                TransitionImageLayout(image, aspects,
                    baseMipLevel: i - 1, mipLevels: 1,
                    baseLayer: 0, layers,
                    oldLayout: ImageLayout.TransferSrcOptimal,
                    newLayout: ImageLayout.ShaderReadOnlyOptimal,
                    executor: executor);

                //Update the prev mip-size
                prevMipSize = curMipSize;
            }

            //Transition the last mip-level to the shader-read layout
            TransitionImageLayout(image, aspects,
                baseMipLevel: mipLevels - 1, mipLevels: 1,
                baseLayer: 0, layers,
                oldLayout: ImageLayout.TransferDstOptimal,
                newLayout: ImageLayout.ShaderReadOnlyOptimal,
                executor: executor);

            var view = CreateView(
                image, texture.Format, mipLevels, aspects, cubeMap: texture.IsCubeMap);
            return new DeviceTexture(texture.Format, mipLevels, aspects, image, memory, view);
        }

        internal static DeviceTexture CreateDepthTarget(
            Int2 size,
            Format format,
            SampleCounts samples,
            Device logicalDevice,
            Memory.Pool memoryPool,
            TransientExecutor executor,
            bool allowSampling = false)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (memoryPool == null)
                throw new ArgumentNullException(nameof(memoryPool));
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));

            var usage = allowSampling
                ? ImageUsages.DepthStencilAttachment | ImageUsages.Sampled
                : ImageUsages.DepthStencilAttachment | ImageUsages.TransientAttachment;
            var aspects = ImageAspects.Depth;
            var image = CreateImage(
                logicalDevice, 
                format,
                samples,
                size,
                mipLevels: 1,
                usage,
                cubeMap: false);
            var memory = memoryPool.AllocateAndBind(image, Chunk.Location.Device);
            
            //Transition the image to the depth attachment layout
            TransitionImageLayout(image, aspects,
                baseMipLevel: 0,
                mipLevels: 1,
                baseLayer: 0,
                layers: 1,
                oldLayout: ImageLayout.Undefined,
                newLayout: ImageLayout.DepthStencilAttachmentOptimal,
                executor: executor);

            var view = CreateView(image, format, mipLevels: 1, aspects, cubeMap: false);
            return new DeviceTexture(format, mipLevels: 1, aspects, image, memory, view);
        }

        internal static DeviceTexture CreateColorTarget(
            Int2 size,
            Format format,
            SampleCounts samples,
            Device logicalDevice,
            Memory.Pool memoryPool,
            TransientExecutor executor,
            bool allowSampling = false)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (memoryPool == null)
                throw new ArgumentNullException(nameof(memoryPool));
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));

            var usage = allowSampling
                ? ImageUsages.ColorAttachment | ImageUsages.Sampled
                : ImageUsages.ColorAttachment | ImageUsages.TransientAttachment;
            var aspects = ImageAspects.Color;
            var image = CreateImage(
                logicalDevice, 
                format,
                samples,
                size,
                mipLevels: 1,
                usage,
                cubeMap: false);
            var memory = memoryPool.AllocateAndBind(image, Chunk.Location.Device);
            
            //Transition the image to the depth attachment layout
            TransitionImageLayout(image, aspects,
                baseMipLevel: 0,
                mipLevels: 1,
                baseLayer: 0,
                layers: 1,
                oldLayout: ImageLayout.Undefined,
                newLayout: ImageLayout.ColorAttachmentOptimal,
                executor: executor);

            var view = CreateView(image, format, mipLevels: 1, aspects, cubeMap: false);
            return new DeviceTexture(format, mipLevels: 1, aspects, image, memory, view);
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
            int mipLevels,
            ImageAspects aspects,
            Image image,
            Block memory,
            ImageView view)
        {
            this.format = format;
            this.mipLevels = mipLevels;
            this.aspects = aspects;
            this.image = image;
            this.memory = memory;
            this.view = view;
        }

        private static Image CreateImage(
            Device logicalDevice,
            Format format,
            SampleCounts samples,
            Int2 size,
            int mipLevels,
            ImageUsages usage,
            bool cubeMap)
            => logicalDevice.CreateImage(new ImageCreateInfo {
                Flags = cubeMap ? ImageCreateFlags.CubeCompatible : ImageCreateFlags.None,
                ImageType = ImageType.Image2D,
                Format = format,
                Extent = new Extent3D(size.X, size.Y, 1),
                MipLevels = mipLevels,
                ArrayLayers = cubeMap ? 6 : 1,
                Samples = samples,
                Tiling = ImageTiling.Optimal,
                Usage = usage,
                SharingMode = SharingMode.Exclusive,
                InitialLayout = ImageLayout.Undefined});

        private static ImageView CreateView(
            Image image,
            Format format,
            int mipLevels,
            ImageAspects aspects,
            bool cubeMap)
            => image.CreateView(new ImageViewCreateInfo(
                format: format,
                subresourceRange: new ImageSubresourceRange(
                    aspectMask: aspects, 
                    baseMipLevel: 0,
                    levelCount: mipLevels,
                    baseArrayLayer: 0,
                    layerCount: cubeMap ? 6 : 1),
                viewType: cubeMap ? ImageViewType.ImageCube : ImageViewType.Image2D,
                components: new ComponentMapping(
                    r: ComponentSwizzle.R,
                    g: ComponentSwizzle.G,
                    b: ComponentSwizzle.B,
                    a: ComponentSwizzle.A)));
        
        private static void BlitImage(
            ImageAspects aspectMask,
            Image fromImage,
            IntRect fromRegion,
            int fromMipLevel,
            int fromLayerCount,
            Image toImage,
            IntRect toRegion,
            int toMipLevel,
            int toLayerCount,
            TransientExecutor executor)
        {
            ImageBlit blit = new ImageBlit
            {
                SrcSubresource = new ImageSubresourceLayers(
                    aspectMask,
                    fromMipLevel,
                    baseArrayLayer: 0,
                    fromLayerCount),
                SrcOffset1 = new Offset3D(fromRegion.Min.X, fromRegion.Min.Y, 0),
                SrcOffset2 = new Offset3D(fromRegion.Max.X, fromRegion.Max.Y, 1),
                DstSubresource = new ImageSubresourceLayers(
                    aspectMask,
                    toMipLevel,
                    baseArrayLayer: 0,
                    toLayerCount),
                DstOffset1 = new Offset3D(toRegion.Min.X, toRegion.Min.Y, 0),
                DstOffset2 = new Offset3D(toRegion.Max.X, toRegion.Max.Y, 1)
            };

            //Execute the blit
            executor.ExecuteBlocking(commandBuffer =>
            {
                commandBuffer.CmdBlitImage(
                    srcImage: fromImage,
                    srcImageLayout: ImageLayout.TransferSrcOptimal,
                    dstImage: toImage,
                    dstImageLayout: ImageLayout.TransferDstOptimal,
                    regions:  new [] { blit },
                    filter: Filter.Linear);
            });
        }

        private static void TransitionImageLayout(
            Image image,
            ImageAspects aspectMask,
            int baseMipLevel,
            int mipLevels,
            int baseLayer,
            int layers,
            ImageLayout oldLayout,
            ImageLayout newLayout,
            TransientExecutor executor)
        {
            //Get where this transition has to wait and what has to wait for this transition
            Accesses sourceAccess, destinationAccess;
            PipelineStages sourcePipelineStages, destinationPipelineStages;
            if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
            {
                sourceAccess = Accesses.None;
                destinationAccess = Accesses.TransferWrite;
                sourcePipelineStages = PipelineStages.TopOfPipe;
                destinationPipelineStages = PipelineStages.Transfer;
            }
            else
            if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.DepthStencilAttachmentOptimal)
            {
                sourceAccess = Accesses.None;
                destinationAccess = Accesses.DepthStencilAttachmentRead | Accesses.DepthStencilAttachmentWrite;
                sourcePipelineStages = PipelineStages.TopOfPipe;
                destinationPipelineStages = PipelineStages.EarlyFragmentTests;
            }
            else
            if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.ColorAttachmentOptimal)
            {
                sourceAccess = Accesses.None;
                destinationAccess = Accesses.ColorAttachmentRead | Accesses.ColorAttachmentWrite;
                sourcePipelineStages = PipelineStages.TopOfPipe;
                destinationPipelineStages = PipelineStages.ColorAttachmentOutput;
            }
            else
            if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.TransferSrcOptimal)
            {
                sourceAccess = Accesses.TransferWrite;
                destinationAccess = Accesses.TransferRead;
                sourcePipelineStages = PipelineStages.Transfer;
                destinationPipelineStages = PipelineStages.Transfer;
            }
            else
            if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
            {
                sourceAccess = Accesses.TransferWrite;
                destinationAccess = Accesses.ShaderRead;
                sourcePipelineStages = PipelineStages.Transfer;
                destinationPipelineStages = PipelineStages.FragmentShader;
            }
            else
            if (oldLayout == ImageLayout.TransferSrcOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
            {
                sourceAccess = Accesses.TransferRead;
                destinationAccess = Accesses.ShaderRead;
                sourcePipelineStages = PipelineStages.Transfer;
                destinationPipelineStages = PipelineStages.FragmentShader;
            }
            else
                throw new Exception(
                    $"[{nameof(DeviceTexture)}] Unsupported image transition: from: {oldLayout} to: {newLayout}");
            
            //Create the transition barrier
            var imageMemoryBarrier = new ImageMemoryBarrier(
                image: image,
                subresourceRange: new ImageSubresourceRange(
                    aspectMask: aspectMask,
                    baseMipLevel: baseMipLevel,
                    levelCount: mipLevels,
                    baseArrayLayer: baseLayer,
                    layerCount: layers),
                srcAccessMask: sourceAccess,
                dstAccessMask: destinationAccess,
                oldLayout: oldLayout,
                newLayout: newLayout);

            //Execute the barrier
            executor.ExecuteBlocking(commandBuffer =>
            {
                commandBuffer.CmdPipelineBarrier(
                    srcStageMask: sourcePipelineStages,
                    dstStageMask: destinationPipelineStages,
                    dependencyFlags: Dependencies.None,
                    memoryBarriers: null,
                    bufferMemoryBarriers: null,
                    imageMemoryBarriers: new [] { imageMemoryBarrier });
            });
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(DeviceTexture)}] Allready disposed");
        }

        private static int CalculateMipLevels(Int2 size)
        //Calculate how many times the size can be divided by 2
        //(and then + 1 because the original size also takes one)
            => (int)Floor(Log(Max(size.X, size.Y), 2)) + 1;
    }
}