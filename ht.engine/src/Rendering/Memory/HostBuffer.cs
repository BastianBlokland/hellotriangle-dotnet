using System;
using System.Runtime.CompilerServices;

using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering.Memory
{
    internal sealed class HostBuffer : IBuffer, IDisposable
    {
        //Properties
        public VulkanCore.Buffer VulkanBuffer => buffer;
        public long Size => size;

        //Data
        private readonly VulkanCore.Buffer buffer;
        private readonly Block memory;
        private readonly long size;

        private bool disposed;

        internal HostBuffer(
            Device logicalDevice,
            Pool memoryPool,
            BufferUsages usages,
            long size)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (memoryPool == null)
                throw new ArgumentNullException(nameof(memoryPool));
            this.size = size;

            //Create the buffer
            buffer = logicalDevice.CreateBuffer(new BufferCreateInfo(
                size: size,
                usages: usages,
                flags: BufferCreateFlags.None,
                sharingMode: SharingMode.Exclusive
            ));
            //Allocate memory for this buffer
            memory = memoryPool.AllocateAndBind(buffer, Chunk.Location.Host);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            buffer.Dispose();
            memory.Free();
            disposed = true;
        }

        public int Write<T>(T data)
            where T : struct
        {
            ThrowIfDisposed();

            int dataSize = Unsafe.SizeOf<T>();
            if (dataSize > memory.Size)
                throw new ArgumentException(
                    $"[{nameof(HostBuffer)}] Data does not fit in memory region", nameof(data));

            //Copy the data into the buffer
            unsafe
            {
                //Map the memory to a cpu pointer
                void* stagingPointer = memory.Map().ToPointer();
                
                //Copy the data over
                void* dataPointer = Unsafe.AsPointer(ref data);
                System.Buffer.MemoryCopy(dataPointer, stagingPointer, dataSize, dataSize);

                //Release the cpu memory
                memory.Unmap();
            }
            return dataSize;
        }

        public int Write<T>(T[] data)
            where T : struct
        {
            ThrowIfDisposed();

            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length == 0)
                throw new ArgumentException(
                    $"[{nameof(Pool)}] Given data-array is empty", nameof(data));

            int dataSize = data.GetSize();
            if (dataSize > memory.Size)
                throw new ArgumentException(
                    $"[{nameof(HostBuffer)}] Data does not fit in memory region", nameof(data));

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
                throw new Exception($"[{nameof(HostBuffer)}] Allready disposed");
        }
    }
}