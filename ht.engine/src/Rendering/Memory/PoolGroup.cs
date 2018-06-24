using System;
using System.Collections.Generic;

using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering.Memory
{
    internal sealed class PoolGroup : IDisposable
    {
        private readonly Device logicalDevice;
        private readonly HostDevice hostDevice;
        private readonly List<Pool> pools = new List<Pool>();
        private bool disposed;

        public PoolGroup(Device logicalDevice, HostDevice hostDevice)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (hostDevice == null)
                throw new ArgumentNullException(nameof(hostDevice));
            this.logicalDevice = logicalDevice;
            this.hostDevice = hostDevice;
        }

        public void AllocateAndBind(VulkanCore.Image image)
        {
            ThrowIfDisposed();

            var memRequirements = image.GetMemoryRequirements();
            Pool pool = GetPool(memRequirements);
            var memRegion = pool.Allocate(memRequirements);
            image.BindMemory(pool.Memory, memRegion.Offset);
        }

        public void AllocateAndBind(VulkanCore.Buffer buffer)
        {
            ThrowIfDisposed();

            var memRequirements = buffer.GetMemoryRequirements();
            Pool pool = GetPool(memRequirements);
            var memRegion = pool.Allocate(memRequirements);
            buffer.BindMemory(pool.Memory, memRegion.Offset);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            pools.DisposeAll();
            disposed = true;
        }

        private Pool GetPool(MemoryRequirements requirements)
        {
            //Get existing pool
            for (int i = 0; i < pools.Count; i++)
                if (pools[i].IsSupported(requirements))
                    return pools[i];
            
            //Create a new pool for this requirement
            Pool newPool = new Pool(logicalDevice, hostDevice, requirements.MemoryTypeBits);
            pools.Add(newPool);
            return newPool;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(PoolGroup)}] Allready disposed");
        }
    }
}