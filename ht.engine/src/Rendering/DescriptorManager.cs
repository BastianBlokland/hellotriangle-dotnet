using System;
using System.Collections.Generic;

using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering
{
    internal sealed class DescriptorManager : IDisposable
    {
        internal readonly struct Block
        {
            //Properties
            internal DescriptorSetLayout Layout => Container.Layout;
            internal DescriptorSet Set => Container.GetDescriptorSet(this);

            //Data
            internal readonly Chunk Container;
            internal readonly int Id;

            public Block(Chunk container, int id)
            {
                Container = container;
                Id = id;
            }

            public void Update(
                Memory.DeviceBuffer[] buffers,
                DeviceSampler[] samplers,
                DeviceTexture[] textures) => Container.UpdateSet(this, buffers, samplers, textures);
            public void Free() => Container.Free(this);
        }

        internal sealed class Chunk : IDisposable
        {
            //Properties
            internal DescriptorBinding Binding => binding;
            internal DescriptorSetLayout Layout => layout;

            //Data
            private readonly DescriptorBinding binding;
            private readonly DescriptorPool pool;
            private readonly DescriptorSetLayout layout;
            private readonly DescriptorSet[] sets;
            private readonly bool[] isFree;

            internal Chunk(Device logicalDevice, DescriptorBinding binding, int size = 25)
            {
                this.binding = binding;
                
                //Create a pool that contains enough resources for 'size' times what a giving binding requires
                pool = logicalDevice.CreateDescriptorPool(new DescriptorPoolCreateInfo(
                    maxSets: size,
                    poolSizes: new []
                    {
                        new DescriptorPoolSize(
                            DescriptorType.UniformBuffer, binding.UniformBufferCount * size),
                        new DescriptorPoolSize(
                            DescriptorType.CombinedImageSampler, binding.ImageSamplerCount * size),
                    },
                    flags: DescriptorPoolCreateFlags.None));
                
                //Create a layout that matches the giving binding
                layout = CreateLayout(logicalDevice, binding);

                //Even tho we use the say layout for the entire pool the 'VkDescriptorSetAllocateInfo'
                //expects a layout per allocation so we create a array of layouts all pointing to the 
                //same layout
                var layouts = new DescriptorSetLayout[size];
                for (int i = 0; i < layouts.Length; i++)
                    layouts[i] = layout;

                //Pre-allocate all the sets from the pool
                sets = pool.AllocateSets(new DescriptorSetAllocateInfo(
                    descriptorSetCount: size,
                    setLayouts: layouts));

                //Mark all the sets as being free
                isFree = new bool[size];
                for (int i = 0; i < isFree.Length; i++)
                    isFree[i] = true;
            }

            internal Block? TryAllocate()
            {
                for (int i = 0; i < isFree.Length; i++)
                {
                    if (isFree[i])
                    {
                        isFree[i] = false;
                        return new Block(this, id: i);
                    }
                }
                return null;
            }

            internal void Free(Block block)
            {
                if (block.Container != this)
                    throw new ArgumentException(
                        $"[{nameof(Chunk)}] Given block was not allocated from this pool", nameof(block));
                if (isFree[block.Id])
                    throw new ArgumentException(
                        $"[{nameof(Chunk)}] Given block was allready freed", nameof(block));
                isFree[block.Id] = true;
            }

            internal DescriptorSet GetDescriptorSet(Block block)
            {
                if (block.Container != this)
                    throw new ArgumentException(
                        $"[{nameof(Chunk)}] Given block was not allocated from this pool", nameof(block));
                return sets[block.Id];
            }
            
            internal void UpdateSet(
                Block block,
                Memory.DeviceBuffer[] buffers,
                DeviceSampler[] samplers,
                DeviceTexture[] textures)
            {
                if (block.Container != this)
                    throw new ArgumentException(
                        $"[{nameof(Chunk)}] Given block was not allocated from this pool", nameof(block));
                if (buffers.Length != binding.UniformBufferCount)
                    throw new ArgumentException(
                        $"[{nameof(Chunk)}] Incorrect number of buffers provided", nameof(buffers));
                if (samplers.Length != binding.ImageSamplerCount)
                    throw new ArgumentException(
                        $"[{nameof(Chunk)}] Incorrect number of samplers provided", nameof(samplers));
                if (textures.Length != binding.ImageSamplerCount)
                    throw new ArgumentException(
                        $"[{nameof(Chunk)}] Incorrect number of images provided", nameof(textures));

                var set = sets[block.Id];
                var writes = new WriteDescriptorSet[binding.TotalBindings];
                for (int i = 0; i < writes.Length; i++)
                {
                    bool isBuffer = i < binding.UniformBufferCount;
                    writes[i] = new WriteDescriptorSet(
                        dstSet: set,
                        dstBinding: i,
                        dstArrayElement: 0,
                        descriptorCount: 1,
                        descriptorType: isBuffer ?
                            DescriptorType.UniformBuffer :
                            DescriptorType.CombinedImageSampler,
                        imageInfo: isBuffer ? null : new [] {
                            new DescriptorImageInfo(
                                sampler: samplers[i - binding.UniformBufferCount].Sampler,
                                imageView: textures[i - binding.UniformBufferCount].View,
                                imageLayout: ImageLayout.ShaderReadOnlyOptimal) },
                        bufferInfo: !isBuffer ? null : new [] {
                            new DescriptorBufferInfo(
                                buffer: buffers[i].Buffer,
                                offset: 0,
                                range: buffers[i].Size) },
                        texelBufferView: null);
                }
                pool.UpdateSets(writes, descriptorCopies: null);
            }

            public void Dispose()
            {
                layout.Dispose();
                pool.Dispose();
            }

            private static DescriptorSetLayout CreateLayout(Device logicalDevice, DescriptorBinding binding)
            {
                var bindings = new DescriptorSetLayoutBinding[binding.TotalBindings];
                for (int i = 0; i < bindings.Length; i++)
                {
                    bindings[i] = new DescriptorSetLayoutBinding(
                        binding: i,
                        descriptorType: i < binding.UniformBufferCount ? 
                            DescriptorType.UniformBuffer :
                            DescriptorType.CombinedImageSampler,
                        descriptorCount: 1,
                        //NOTE: At the moment all resources are bound in both the vertex and the fragment
                        //shader, this is the most user friendly setup. Need to do some profiling to 
                        //see if this is hurting the performance
                        stageFlags: ShaderStages.Vertex | ShaderStages.Fragment);
                }
                return logicalDevice.CreateDescriptorSetLayout(new DescriptorSetLayoutCreateInfo(
                    bindings));
            }
        }

        //Data
        private readonly Device logicalDevice;
        private readonly Logger logger;
        private readonly List<Chunk> chunks = new List<Chunk>();
        private bool disposed;

        internal DescriptorManager(Device logicalDevice, Logger logger = null)
        {  
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            this.logger = logger;
            this.logicalDevice = logicalDevice;
        }

        internal Block Allocate(DescriptorBinding binding)
        {
            ThrowIfDisposed();

            //Try to allocate a descriptor block from a existing chunk
            for (int i = 0; i < chunks.Count; i++)
            {
                if (chunks[i].Binding == binding)
                {
                    Block? block = chunks[i].TryAllocate();
                    if (block != null)
                        return block.Value;
                }
            }

            //If there is no existing chunk that matches given binding, then we allocate a new chunk
            Chunk newChunk = new Chunk(logicalDevice, binding);
            chunks.Add(newChunk);

            logger?.Log(nameof(DescriptorManager), 
                $"New descriptor-chuck allocated, binding: '{binding}'");
            return newChunk.TryAllocate().Value;
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            chunks.DisposeAll();
            disposed = true;
        }        

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(DescriptorManager)}] Allready disposed");
        }
    }
}