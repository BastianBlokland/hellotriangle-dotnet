using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HT.Engine.Utils;

using VulkanCore;

namespace HT.Engine.Rendering.Memory
{
    internal sealed class HostBuffer : IBuffer, IShaderInput, IDisposable
    {
        //Properties
        public DescriptorType DescriptorType => DescriptorType.UniformBuffer;
        public VulkanCore.Buffer VulkanBuffer => buffer;
        public long Size => size;

        //Data
        private readonly VulkanCore.Buffer buffer;
        private readonly Block memory;
        private readonly long size;
        private readonly long alignment;

        private bool disposed;

        internal HostBuffer(
            Device logicalDevice,
            Pool memoryPool,
            BufferUsages usages,
            long size,
            long alignment = 1)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (memoryPool == null)
                throw new ArgumentNullException(nameof(memoryPool));
            this.size = size;
            this.alignment = alignment;

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

        public WriteDescriptorSet CreateDescriptorWrite(DescriptorSet set, int binding)
            => new WriteDescriptorSet(
                dstSet: set,
                dstBinding: binding,
                dstArrayElement: 0,
                descriptorCount: 1,
                descriptorType: DescriptorType,
                bufferInfo: new [] { new DescriptorBufferInfo(buffer, offset: 0, range: size) });

        public void Dispose()
        {
            ThrowIfDisposed();

            buffer.Dispose();
            memory.Free();
            disposed = true;
        }

        internal int Write<T>(T data, long offset = 0)
            where T : struct
        {
            ThrowIfDisposed();

            //Calculate padding to adhere to the alignment
            long padding = GetPadding(offset, alignment);
            long paddedOffset = offset + padding;

            int dataSize = Unsafe.SizeOf<T>();
            if (dataSize + paddedOffset > memory.Size)
                throw new ArgumentException(
                    $"[{nameof(HostBuffer)}] Data does not fit in memory region", nameof(data));

            //Copy the data into the buffer
            unsafe
            {
                //Map the memory to a cpu pointer
                void* stagingPointer = memory.Map(paddedOffset).ToPointer();
                
                //Copy the data over
                void* dataPointer = Unsafe.AsPointer(ref data);
                System.Buffer.MemoryCopy(dataPointer, stagingPointer, dataSize, dataSize);

                //Flush the data (so its visible to the gpu)
                memory.Flush();

                //Release the cpu memory
                memory.Unmap();
            }
            return dataSize;
        }

        internal int Write<T>(T[] data, long offset = 0) where T : struct => 
            Write((ReadOnlySpan<T>)data, offset);
        
        internal unsafe int Write<T>(ReadOnlySpan<T> data, long offset = 0)
            where T : struct
        {
            ThrowIfDisposed();

            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length == 0)
                throw new ArgumentException(
                    $"[{nameof(Pool)}] Given data-array is empty", nameof(data));

            //Calculate padding to adhere to the alignment
            long padding = GetPadding(offset, alignment);
            long paddedOffset = offset + padding;

            int dataSize = data.GetSize();
            if (paddedOffset + dataSize > memory.Size)
                throw new ArgumentException(
                    $"[{nameof(HostBuffer)}] Data ({paddedOffset + dataSize}) does not fit in memory region", nameof(data));

            //Map the memory to a cpu pointer
            void* stagingPointer = memory.Map(paddedOffset).ToPointer();
            
            //Copy the data over
            void* dataPointer = Unsafe.AsPointer(ref MemoryMarshal.GetReference(data));
            System.Buffer.MemoryCopy(dataPointer, stagingPointer, dataSize, dataSize);

            //Flush the data (so its visible to the gpu)
            memory.Flush();

            //Release the cpu memory
            memory.Unmap();
            return dataSize;
        }

        private long GetPadding(long offset, long alignment)
        {
            long remainder = offset % alignment;
            return remainder == 0 ? 0 : (alignment - remainder);
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(HostBuffer)}] Allready disposed");
        }
    }
}