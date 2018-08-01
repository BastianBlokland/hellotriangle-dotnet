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
            IInternalTexture texture,
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
            
            var aspects = ImageAspects.Color;
            var image = CreateImage(
                logicalDevice,
                texture.Format,
                texture.Size,
                ImageUsages.TransferDst | ImageUsages.Sampled,
                cubeMap: texture.IsCubeMap);
            var memory = memoryPool.AllocateAndBind(image, Chunk.Location.Device);
            
            //Transition the image to a layout where it can receive data
            TransitionImageLayout(
                image: image, 
                subresource: new ImageSubresourceLayers(
                    aspectMask: aspects,
                    mipLevel: 0,
                    baseArrayLayer: 0,
                    layerCount: texture.IsCubeMap ? 6 : 1),
                oldLayout: ImageLayout.Undefined,
                newLayout: ImageLayout.TransferDstOptimal,
                executor: executor);
            //Upload the data
            texture.Upload(stagingBuffer, executor, image, aspects);
            //Transition the image to a layout so it can be read from
            TransitionImageLayout(
                image: image, 
                subresource: new ImageSubresourceLayers(
                    aspectMask: aspects,
                    mipLevel: 0,
                    baseArrayLayer: 0,
                    layerCount: texture.IsCubeMap ? 6 : 1),
                oldLayout: ImageLayout.TransferDstOptimal,
                newLayout: ImageLayout.ShaderReadOnlyOptimal,
                executor: executor);

            var view = CreateView(image, texture.Format, aspects, cubeMap: texture.IsCubeMap);
            return new DeviceTexture(texture.Format, aspects, image, memory, view);
        }

        internal static DeviceTexture CreateDepthTexture(
            Int2 size,
            Device logicalDevice,
            Memory.Pool memoryPool,
            TransientExecutor executor)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (memoryPool == null)
                throw new ArgumentNullException(nameof(memoryPool));
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));

            var aspects = ImageAspects.Depth;
            var image = CreateImage(
                logicalDevice, DepthFormat, size, ImageUsages.DepthStencilAttachment, cubeMap: false);
            var memory = memoryPool.AllocateAndBind(image, Chunk.Location.Device);
            
            //Transition the image to the depth attachment layout
            TransitionImageLayout(
                image: image, 
                subresource: new ImageSubresourceLayers(
                    aspectMask: aspects,
                    mipLevel: 0,
                    baseArrayLayer: 0,
                    layerCount: 1),
                oldLayout: ImageLayout.Undefined,
                newLayout: ImageLayout.DepthStencilAttachmentOptimal,
                executor: executor);

            var view = CreateView(image, DepthFormat, aspects, cubeMap: false);
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

        private static Image CreateImage(
            Device logicalDevice,
            Format format,
            Int2 size,
            ImageUsages usage,
            bool cubeMap)
            => logicalDevice.CreateImage(new ImageCreateInfo {
                Flags = cubeMap ? ImageCreateFlags.CubeCompatible : ImageCreateFlags.None,
                ImageType = ImageType.Image2D,
                Format = format,
                Extent = new Extent3D(size.X, size.Y, 1),
                MipLevels = 1,
                ArrayLayers = cubeMap ? 6 : 1,
                Samples = SampleCounts.Count1,
                Tiling = ImageTiling.Optimal,
                Usage = usage,
                SharingMode = SharingMode.Exclusive,
                InitialLayout = ImageLayout.Undefined});

        private static ImageView CreateView(
            Image image,
            Format format,
            ImageAspects aspects,
            bool cubeMap)
            => image.CreateView(new ImageViewCreateInfo(
                format: format,
                subresourceRange: new ImageSubresourceRange(
                    aspectMask: aspects, 
                    baseMipLevel: 0,
                    levelCount: 1,
                    baseArrayLayer: 0,
                    layerCount: cubeMap ? 6 : 1),
                viewType: cubeMap ? ImageViewType.ImageCube : ImageViewType.Image2D,
                components: new ComponentMapping(
                    r: ComponentSwizzle.R,
                    g: ComponentSwizzle.G,
                    b: ComponentSwizzle.B,
                    a: ComponentSwizzle.A)));
        
        private static void TransitionImageLayout(
            Image image, 
            ImageSubresourceLayers subresource,
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
            if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
            {
                sourceAccess = Accesses.TransferWrite;
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
                    aspectMask: subresource.AspectMask,
                    baseMipLevel: subresource.MipLevel,
                    levelCount: 1,
                    baseArrayLayer: subresource.BaseArrayLayer,
                    layerCount: subresource.LayerCount),
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
    }
}