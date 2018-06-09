using System;

using VulkanCore.Khr;

namespace HT.Engine.Rendering
{
    public sealed class Surface : IDisposable
    {
        private readonly VulkanCore.Khr.SurfaceKhr khrSurface;
        private readonly SurfaceType type;
        private bool disposed;

        internal Surface(VulkanCore.Khr.SurfaceKhr khrSurface, SurfaceType type)
        {
            this.khrSurface = khrSurface;
            this.type = type;
        }

        public bool DoesQueueSupportSurface(PhysicalDevice device, int queueFamilyIndex)
        {
            ThrowIfDisposed();
            switch(type)
            {
                //On windows test if this queue supports presentation to the win32 compositor
                case SurfaceType.HkrWin32: 
                    bool isCompositorSupported = device.VulkanPhysicalDevice.GetWin32PresentationSupportKhr(queueFamilyIndex);
                    if(!isCompositorSupported)
                        return false;
                    break;
                //Note: on macOS there is no way (or need) to check for compositor support
            }
            //Test if the native surface is supported by this device
            return device.VulkanPhysicalDevice.GetSurfaceSupportKhr(queueFamilyIndex, khrSurface);
        }

        public void Dispose()
        {
            if(!disposed)
            {
                khrSurface.Dispose();
                disposed = true;
            }            
        }

        private void ThrowIfDisposed()
        {
            if(disposed)
                throw new Exception($"[{nameof(Surface)}] Allready disposed!");
        }
    }
}