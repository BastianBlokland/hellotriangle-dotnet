using System;

using HT.Engine.Platform;
using VulkanCore;
using VulkanCore.Khr;

namespace HT.Engine.Rendering
{
    public sealed class Window : IDisposable
    {
        public event Action CloseRequested;

        private readonly INativeWindow nativeWindow;
        private readonly SurfaceKhr surface;
        private readonly Device logicalDevice;
        private readonly Queue graphicsQueue;

        private bool disposed;

        internal Window(INativeWindow nativeWindow, SurfaceKhr surface, HostDevice device)
        {
            this.nativeWindow = nativeWindow;
            this.surface = surface;

            //Subscribe to callbacks for the native window
            nativeWindow.CloseRequested += NativeCloseRequested;

            //Create a logical device (and queues on the device)
            (logicalDevice, graphicsQueue) = device.CreateLogicalDevice();
        }

        public void Dispose()
        {
            if(!disposed)
            {
                logicalDevice.Dispose();
                nativeWindow.Dispose();
                disposed = true;
            }
        }

        private void NativeCloseRequested() => CloseRequested?.Invoke();
    }
}