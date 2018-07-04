using System;
using System.Runtime.CompilerServices;

using HT.Engine.Math;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering.Memory
{
    internal sealed class StagingBuffer : IDisposable
    {
        private readonly TransientExecutor executor;
        private readonly long size;
        private readonly VulkanCore.Buffer buffer;
        private readonly Block memory;

        private bool disposed;

        internal StagingBuffer(
            Device logicalDevice,
            Pool memoryPool,
            TransientExecutor executor,
            long size = 1_048_576)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (memoryPool == null)
                throw new ArgumentNullException(nameof(memoryPool));
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));
            this.executor = executor;
            this.size = size;

            //Create the buffer
            buffer = logicalDevice.CreateBuffer(new BufferCreateInfo(
                size: size,
                usages: BufferUsages.TransferSrc,
                flags: BufferCreateFlags.None,
                sharingMode: SharingMode.Exclusive
            ));
            //Allocate memory for this buffer
            memory = memoryPool.AllocateAndBind(buffer, Chunk.Location.Host);
        }

        internal void Upload<T>(
            T[] data, Image destination, ImageSubresourceLayers subresource, Int2 imageExtents)
            where T : struct
        {
            ThrowIfDisposed();

            //Write to our staging buffer
            Write(data);

            //Copy our staging buffer to the image
            executor.ExecuteBlocking(commandBuffer =>
            {
                commandBuffer.CmdCopyBufferToImage(
                    srcBuffer: buffer,
                    dstImage: destination,
                    dstImageLayout: ImageLayout.TransferDstOptimal,
                    regions: new BufferImageCopy {
                        BufferOffset = 0,
                        BufferRowLength = 0,
                        BufferImageHeight = 0,
                        ImageSubresource = subresource,
                        ImageOffset = new Offset3D(x: 0, y: 0, z: 0),
                        ImageExtent = new Extent3D(
                            width: imageExtents.X,
                            height: imageExtents.Y,
                            depth: 1)});
            });        
        }

        internal void Upload<T>(T[] data, DeviceBuffer destination)
            where T : struct
        {
            ThrowIfDisposed();

            //Write to our staging buffer
            int dataSize = Write(data);

            //Copy our staging buffer to the destination
            executor.ExecuteBlocking(commandBuffer =>
            {
                commandBuffer.CmdCopyBuffer(
                    srcBuffer: buffer,
                    dstBuffer: destination.Buffer,
                    new BufferCopy(size: dataSize, srcOffset: 0, dstOffset: 0));
            });
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            buffer.Dispose();
            memory.Free();
            disposed = true;
        }

        private int Write<T>(T[] data)
            where T : struct
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length == 0)
                throw new ArgumentException(
                    $"[{nameof(Pool)}] Given data-array is empty", nameof(data));

            int dataSize = data.GetSize();
            if (dataSize > size)
                throw new ArgumentException(
                    $"[{nameof(StagingBuffer)}] Data does not fit in given region", nameof(data));

            //Copy the data into the buffer
            unsafe
            {
                //Map the memory to a cpu pointer
                void* stagingPointer = memory.Map().ToPointer();
                
                //Copy the data over
                void* dataPointer = Unsafe.AsPointer(ref data[0]);
                System.Buffer.MemoryCopy(dataPointer, stagingPointer, dataSize, dataSize);

                //Release the cpu memory
                memory.Unmap();
            }
            return dataSize;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(StagingBuffer)}] Allready disposed");
        }
    }
}