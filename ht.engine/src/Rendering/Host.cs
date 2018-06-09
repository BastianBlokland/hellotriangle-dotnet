using System;
using System.Linq;
using System.Collections.Generic;

using VulkanCore.Mvk;
using VulkanCore.Khr;

namespace HT.Engine.Rendering
{
    public sealed class Host : IDisposable
    {
        private readonly VulkanCore.Instance instance;
        private readonly PhysicalDevice[] devices;
        private bool disposed;

        public Host(Platform.INativeApp nativeApp, string applicationName, int applicationVersion)
        {
            VulkanCore.InstanceCreateInfo createInfo = new VulkanCore.InstanceCreateInfo
            (
                appInfo: new VulkanCore.ApplicationInfo
                (
                    applicationName: applicationName, 
                    applicationVersion: applicationVersion, 
                    engineName: Info.NAME,
                    engineVersion: Info.VERSION,
                    apiVersion: new VulkanCore.Version(major: 1, minor: 0, patch: 69)
                ),
                enabledLayerNames: GetLayerNames(nativeApp),
                enabledExtensionNames: GetExtensionNames(nativeApp)
            );
            instance = new VulkanCore.Instance(createInfo);

            //Get devices
            VulkanCore.PhysicalDevice[] physicalDevices = instance.EnumeratePhysicalDevices();
            devices = new PhysicalDevice[physicalDevices.Length];
            for (int i = 0; i < devices.Length; i++)
                devices[i] = new PhysicalDevice(physicalDevices[i]);
        }

        public Surface CreateMacOSSurface(IntPtr metalViewHandle)
        {
            ThrowIfDisposed();
            var createInfo = new MacOSSurfaceCreateInfoMvk(metalViewHandle);
            return new Surface(instance.CreateMacOSSurfaceMvk(createInfo), SurfaceType.MvkMacOS);
        }

        public Surface CreateWin32Surface(IntPtr instanceHandle, IntPtr nativeWindowHandle)
        {
            ThrowIfDisposed();
            var createInfo = new Win32SurfaceCreateInfoKhr(instanceHandle, nativeWindowHandle);
            return new Surface(instance.CreateWin32SurfaceKhr(createInfo), SurfaceType.HkrWin32);
        }

        public PhysicalDevice FindSuitableDevice(Surface surface)
        {
            ThrowIfDisposed();
            //If we have a supported discrete gpu then we pick that
            for (int i = 0; i < devices.Length; i++)
                if(devices[i].DiscreteGPU && devices[i].IsSurfaceSupported(surface))
                    return devices[i];
            //Otherwise we pick the first supported device
            for (int i = 0; i < devices.Length; i++)
                if(devices[i].IsSurfaceSupported(surface))
                    return devices[i];
            throw new Exception($"[{nameof(Host)}] Unable to find a supported device");
        }

        public void Dispose()
        {
            if(!disposed)
            {
                instance.Dispose();
                disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if(disposed)
                throw new Exception($"[{nameof(Host)}] Allready disposed!");
        }

        private static string[] GetLayerNames(Platform.INativeApp nativeApp) => new string[0];
        private static string[] GetExtensionNames(Platform.INativeApp nativeApp) => new [] 
        { 
            "VK_KHR_surface", //Generic surface-extension
            GetSurfaceExtension(nativeApp.SurfaceType) //Platform-specific surface-extension
        };

        private static string GetSurfaceExtension(SurfaceType surfaceType)
        {
            switch(surfaceType)
            {
                case SurfaceType.MvkMacOS: return "VK_MVK_macos_surface";
                case SurfaceType.HkrWin32: return "VK_KHR_win32_surface";
            }
            throw new Exception($"[{nameof(Host)}] No surface-extension known for surfaceType: {surfaceType}");
        }
    }
}