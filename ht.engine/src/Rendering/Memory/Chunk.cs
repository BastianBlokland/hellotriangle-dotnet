using System;

using HT.Engine.Math;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering.Memory
{
    internal sealed class Chunk : IDisposable
    {
        //Helper properties
        internal DeviceMemory Memory => memory;
        internal int MemoryTypeIndex => memoryTypeIndex;
        internal long TotalSize => totalSize;
        internal long FreeSize
        {
            get
            {
                long result = 0;
                for (int i = 0; i < freeBlocks.Count; i++)
                    result += freeBlocks.Data[i].Size;
                return result;
            }
        }

        //Data
        private readonly long totalSize;
        private readonly ResizeArray<Block> freeBlocks = new ResizeArray<Block>(initialCapacity: 100);
        private readonly int memoryTypeIndex;
        private readonly DeviceMemory memory;

        private bool disposed;

        internal Chunk(
            Device logicalDevice,
            HostDevice hostDevice,
            int supportedMemoryTypesFilter,
            long size = 134_217_728)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (hostDevice == null)
                throw new ArgumentNullException(nameof(hostDevice));
            totalSize = size;

            //Add a block the size of the entire chunk to the free set
            freeBlocks.Add(new Block(container: this, offset: 0, size: size));

            //Find the memory type on the gpu to place this pool in
            memoryTypeIndex = hostDevice.GetMemoryType(
                properties: MemoryProperties.DeviceLocal,
                supportedTypesFilter: supportedMemoryTypesFilter);
            //Allocate the memory
            memory = logicalDevice.AllocateMemory(new MemoryAllocateInfo(
                allocationSize: size,
                memoryTypeIndex: memoryTypeIndex));
        }

        internal bool IsSupported(MemoryRequirements requirements)
            => requirements.MemoryTypeBits.HasBitSet(memoryTypeIndex);

        internal Block? TryAllocate(MemoryRequirements requirements)
        {
            ThrowIfDisposed();

            //Verify that the memory backing this chunk is supported by the requirement
            if (!requirements.MemoryTypeBits.HasBitSet(memoryTypeIndex))
                throw new Exception(
                    $"[{nameof(Chunk)}] Given requirements does not match the memory backing this pool");

            //Find a free block that fits our requested size (with the specified alignment)
            for (int i = 0; i < freeBlocks.Count; i++)
            {
                Block block = freeBlocks.Data[i];

                //Calculate padding to satisfy the alignment
                long padding = GetPadding(block.Offset, requirements.Alignment);
                long paddedSize = requirements.Size + padding;

                long remainingSize = block.Size - paddedSize;
                if (remainingSize < 0) //Doesn't fit
                    continue;
                
                if (padding == 0 && remainingSize == 0) //Perfect case all space is used
                    freeBlocks.RemoveAt(i);
                else
                {
                    //If there is space remaining at the end, then resize the existing block to
                    //become that remaining size
                    if (remainingSize > 0)
                        freeBlocks.Data[i] = new Block(this,
                            offset: block.Offset + paddedSize, size: remainingSize);

                    //We need to add the padding somewhere
                    if (padding > 0)
                    {
                        var paddingBlock = new Block(this, offset: block.Offset, size: padding);
                        //Either put the padding block in the existing block if there was no space
                        //at the end, or simply add it as a new element
                        if (remainingSize == 0)
                            freeBlocks.Data[i] = paddingBlock;
                        else
                            freeBlocks.Add(paddingBlock);
                    }
                }
                return new Block(this, offset: block.Offset + padding, size: requirements.Size);
            }
            //No block that fits the requested size
            return null;
        }

        internal void Free(Block block)
        {
            ThrowIfDisposed();

            if (block.Container != this)
                throw new ArgumentException(
                    $"[{nameof(Chunk)}] Given block does not belong to this chunk");
            
            //Check if either of out neighbors is also free so we can merge onto there
            for (int i = 0; i < freeBlocks.Count; i++)
            {
                Block freeBlock = freeBlocks.Data[i];

                //If this block is right before the given one
                if (freeBlock.Offset + freeBlock.Size == block.Offset)
                {
                    freeBlock = new Block(
                        container: this, 
                        offset: freeBlock.Offset,
                        size: freeBlock.Size + block.Size);
                    return;
                }
                //If this block is right after the given on
                if (block.Offset + block.Size == freeBlock.Offset)
                {
                    freeBlock = new Block(
                        container: this,
                        offset: freeBlock.Offset - block.Size,
                        size: freeBlock.Size + block.Size);
                    return;
                }
            }
            //There was no block to join to to we just add ourselves
            freeBlocks.Add(block);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            memory.Dispose();
            disposed = true;
        }

        private long GetPadding(long offset, long alignment)
        {
            long remainder = offset % alignment;
            if (remainder == 0)
                return 0;
            return alignment - remainder;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(Chunk)}] Allready disposed");
        }
    }
}