using System;
using System.Diagnostics;
using System.Collections.Generic;

using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering
{
    internal sealed class ShaderInputManager : IDisposable
    {
        internal readonly struct Block
        {
            //Properties
            internal bool Valid => Container != null;
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

            public void Update(ReadOnlySpan<IShaderInput> inputs) => Container.UpdateSet(this, inputs);

            public void Free() => Container.Free(this);
        }

        internal sealed class Chunk : IDisposable
        {
            //Properties
            internal DescriptorSetLayout Layout => layout;

            //Data
            private readonly DescriptorType[] types;
            private readonly DescriptorPool pool;
            private readonly DescriptorSetLayout layout;
            private readonly DescriptorSet[] sets;
            private readonly bool[] isFree;

            internal Chunk(Device logicalDevice, ReadOnlySpan<IShaderInput> inputs, int size = 5)
            {
                types = new DescriptorType[inputs.Length];
                for (int i = 0; i < types.Length; i++)
                    types[i] = inputs[i].DescriptorType;

                //Gather how many inputs of each type we have
                var poolSizes = new ResizeArray<DescriptorPoolSize>();
                for (int i = 0; i < types.Length; i++)
                {
                    for (int j = 0; j < poolSizes.Count; j++)
                    {
                        if (poolSizes.Data[j].Type == types[i])
                        {
                            poolSizes.Data[j].DescriptorCount += size;
                            continue;
                        }
                    }
                    poolSizes.Add(new DescriptorPoolSize(types[i], size));
                }

                //Create a pool for 'size' types the amount of resources of one set
                pool = logicalDevice.CreateDescriptorPool(new DescriptorPoolCreateInfo(
                    maxSets: size,
                    poolSizes: poolSizes.ToArray(),
                    flags: DescriptorPoolCreateFlags.None));
                
                //Create a layout that matches the inputs
                layout = CreateLayout(logicalDevice, inputs);

                //Even tho we use the same layout for the entire pool the 'VkDescriptorSetAllocateInfo'
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

            internal bool IsCompatible(ReadOnlySpan<IShaderInput> inputs)
            {
                if (types.Length != inputs.Length)
                    return false;
                for (int i = 0; i < types.Length; i++)
                    if (types[i] != inputs[i].DescriptorType)
                        return false;
                return true;
            }

            internal Block? TryAllocate(ReadOnlySpan<IShaderInput> inputs)
            {
                if (!IsCompatible(inputs))
                    return null;

                for (int i = 0; i < isFree.Length; i++)
                {
                    if (isFree[i])
                    {
                        isFree[i] = false;
                        Block block = new Block(this, id: i);
                        UpdateSet(block, inputs);
                        return block;
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
            
            internal void UpdateSet(Block block, ReadOnlySpan<IShaderInput> inputs)
            {
                if (block.Container != this)
                    throw new ArgumentException(
                        $"[{nameof(Chunk)}] Given block was not allocated from this pool", nameof(block));
                if (!IsCompatible(inputs))
                    throw new ArgumentException(
                        $"[{nameof(Chunk)}] Given inputs are not compatible with this block", nameof(inputs));

                var set = sets[block.Id];
                var writes = new WriteDescriptorSet[inputs.Length];
                for (int i = 0; i < inputs.Length; i++)
                    writes[i] = inputs[i].CreateDescriptorWrite(set, binding: i);
                pool.UpdateSets(writes, descriptorCopies: null);
            }

            public void Dispose()
            {
                layout.Dispose();
                pool.Dispose();
            }

            private static DescriptorSetLayout CreateLayout(
                Device logicalDevice, ReadOnlySpan<IShaderInput> inputs)
            {
                var bindings = new DescriptorSetLayoutBinding[inputs.Length];
                for (int i = 0; i < bindings.Length; i++)
                {
                    bindings[i] = new DescriptorSetLayoutBinding(
                        binding: i,
                        descriptorType: inputs[i].DescriptorType,
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

        internal ShaderInputManager(Device logicalDevice, Logger logger = null)
        {  
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            this.logger = logger;
            this.logicalDevice = logicalDevice;
        }

        internal Block Allocate(ReadOnlySpan<IShaderInput> inputs)
        {
            ThrowIfDisposed();

            //Try to allocate a descriptor block from a existing chunk
            for (int i = 0; i < chunks.Count; i++)
            {
                Block? block = chunks[i].TryAllocate(inputs);
                if (block != null)
                    return block.Value;
            }

            //If there is no existing chunk that matches given binding, then we allocate a new chunk
            Chunk newChunk = new Chunk(logicalDevice, inputs);
            chunks.Add(newChunk);

            logger?.Log(nameof(ShaderInputManager), 
                $"New descriptor-chuck allocated, inputCount: '{inputs.Length}'");
            return newChunk.TryAllocate(inputs).Value;
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            chunks.DisposeAll();
            disposed = true;
        }        

        [Conditional("DEBUG")]
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(ShaderInputManager)}] Allready disposed");
        }
    }
}