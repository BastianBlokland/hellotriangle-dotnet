using System;

using VulkanCore;
using VulkanCore.Khr;

namespace HT.Engine.Rendering
{
    public sealed class Surface : IDisposable
    {
        internal SurfaceKhr KhrSurface => khrSurface;

        private readonly SurfaceKhr khrSurface;
        private readonly SurfaceType type;
        private bool disposed;

        internal Surface(SurfaceKhr khrSurface, SurfaceType type)
        {
            this.khrSurface = khrSurface;
            this.type = type;
        }

        public bool DoesQueueSupportSurface(GraphicsDevice device, int queueFamilyIndex)
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

        /// <summary>
        /// Get presentation mode to use for this device / surface combo
        /// Either uses mailbox or fifo, both of these are non-tearing modes, mailbox just has lower latency
        /// </summary>
        internal PresentModeKhr GetPresentMode(GraphicsDevice device)
        {
            PresentModeKhr[] modes = device.VulkanPhysicalDevice.GetSurfacePresentModesKhr(khrSurface);
            //If mailbox is present then go for that, it is basically having 1 frame being displayed and
            //multiple frames in the background being rendered to, and also allows to redraw those in the background,
            //this allows for things like triple-buffering
            for (int i = 0; i < modes.Length; i++)
                if(modes[i] == PresentModeKhr.Mailbox)
                    return PresentModeKhr.Mailbox;

            //If mailbox is not supported then we go for Fifo
            //Fifo is basically uses 2 frame's, 1 thats being displayed right now and one that is being rendered to
            //When rendering is done but the previous frame is not done being display then the program has to wait
            //According to spec this must be available on all platforms
            return PresentModeKhr.Fifo;
        }

        /// <summary>
        /// Get format for surface image and color that the device can display on this surface
        /// </summary>
        internal (Format imageFormat, ColorSpaceKhr colorSpace) GetFormat(GraphicsDevice device)
        {
            ThrowIfDisposed();
            SurfaceFormatKhr[] formats = device.VulkanPhysicalDevice.GetSurfaceFormatsKhr(khrSurface);

            //If the device only returns 1 options for this surface and it contains 'Undefined' it means that the 
            //device doesn't care and we can pick.
            if(formats.Length == 1 && formats[0].Format == VulkanCore.Format.Undefined)
                return (Format.B8G8R8A8UNorm, formats[0].ColorSpace);

            //If the device has preference then we check if it contains 'B8G8R8A8UNorm' if so we use that
            for (int i = 0; i < formats.Length; i++)
                if(formats[i].Format == Format.B8G8R8A8UNorm)
                    return (formats[i].Format, formats[i].ColorSpace);
            
            //If our preference is not there then we take the first that is supported
            if(formats.Length > 0)
                return (formats[0].Format, formats[0].ColorSpace);

            throw new Exception($"[{nameof(Surface)}] Device {device.Name} doesn't support any format for use with our surface");
        }

        private void ThrowIfDisposed()
        {
            if(disposed)
                throw new Exception($"[{nameof(Surface)}] Allready disposed");
        }
    }
}