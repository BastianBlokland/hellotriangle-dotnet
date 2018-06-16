using System;

using HT.Engine.Math;
using HT.Engine.Platform;
using HT.Engine.Utils;
using VulkanCore;
using VulkanCore.Mvk;
using VulkanCore.Khr;

namespace HT.Engine.Rendering
{
    public sealed class Host : IDisposable
    {
        private readonly Platform.INativeApp nativeApp;
        private readonly Logger logger;
        private readonly LayerProperties[] availableLayers;
        private readonly ExtensionProperties[] availbleExtensions;
        private readonly Instance instance;
        private bool disposed;

        public Host(Platform.INativeApp nativeApp, string applicationName, int applicationVersion, Logger logger = null)
        {
            this.nativeApp = nativeApp;
            this.logger = logger;

            availableLayers = Instance.EnumerateLayerProperties();
            logger?.LogList(nameof(Host), "Available layers:", availableLayers);
            availbleExtensions = Instance.EnumerateExtensionProperties();
            logger?.LogList(nameof(Host), "Available extensions:", availbleExtensions);

            //Verify that all the required layers are available on this host
            string[] requiredLayers = GetRequiredLayerNames(nativeApp.SurfaceType);
            for (int i = 0; i < requiredLayers.Length; i++)
                if(!IsLayerAvailable(requiredLayers[i]))
                    throw new Exception($"[{nameof(Host)}] Required layer '{requiredLayers[i]}' is not available");

            //Verify that all the required extensions are available on this host
            string[] requiredExtensions = GetRequiredExtensionNames(nativeApp.SurfaceType);
            for (int i = 0; i < requiredExtensions.Length; i++)
                if(!IsExtensionAvailable(requiredExtensions[i]))
                    throw new Exception($"[{nameof(Host)}] Required extension '{requiredExtensions[i]}' is not available");

            InstanceCreateInfo createInfo = new InstanceCreateInfo
            (
                appInfo: new ApplicationInfo
                (
                    applicationName: applicationName,
                    applicationVersion: applicationVersion,
                    engineName: Info.NAME,
                    engineVersion: Info.VERSION,
                    apiVersion: new VulkanCore.Version(major: 1, minor: 0, patch: 69)
                ),
                enabledLayerNames: requiredLayers,
                enabledExtensionNames: requiredExtensions
            );
            instance = new Instance(createInfo);
            logger?.Log(nameof(Host), "Created instance");
        }

        public void Dispose()
        {
            if(!disposed)
            {
                instance.Dispose();
                disposed = true;

                logger?.Log(nameof(Host), "Destroyed instance");
            }
        }

        private SurfaceKhr CreateSurface(INativeWindow nativeWindow)
        {
            ThrowIfDisposed();
            switch(nativeApp.SurfaceType)
            {
                case SurfaceType.MvkMacOS: return instance.CreateMacOSSurfaceMvk(new MacOSSurfaceCreateInfoMvk(nativeWindow.OSViewHandle));
                case SurfaceType.HkrWin32: return instance.CreateWin32SurfaceKhr(new Win32SurfaceCreateInfoKhr(nativeWindow.OSInstanceHandle, nativeWindow.OSViewHandle));
            }
            throw new Exception($"[{nameof(Host)}] Unable to create surface for unknown surfaceType: {nativeApp.SurfaceType}");
        }

        private bool IsLayerAvailable(string layerName)
        {
            for (int i = 0; i < availableLayers.Length; i++)
                if(availableLayers[i].LayerName == layerName)
                    return true;
            return false;
        }

        private bool IsExtensionAvailable(string extensionName)
        {
            for (int i = 0; i < availbleExtensions.Length; i++)
                if(availbleExtensions[i].ExtensionName == extensionName)
                    return true;
            return false;
        }

        private void ThrowIfDisposed()
        {
            if(disposed)
                throw new Exception($"[{nameof(Host)}] Allready disposed");
        }

        private static string[] GetRequiredLayerNames(SurfaceType surfaceType)
        {
            switch(surfaceType)
            {
                case SurfaceType.HkrWin32: return new string[0];
                case SurfaceType.MvkMacOS: return new [] { "MoltenVK" };
            }
            throw new Exception($"[{nameof(Host)}] Unknown surfaceType: {surfaceType}");
        }

        private static string[] GetRequiredExtensionNames(SurfaceType surfaceType)
        {
            switch(surfaceType)
            {
                case SurfaceType.HkrWin32: return new [] { "VK_KHR_surface", "VK_KHR_win32_surface" };
                case SurfaceType.MvkMacOS: return new [] { "VK_KHR_surface", "VK_MVK_macos_surface" };
            }
            throw new Exception($"[{nameof(Host)}] Unknown surfaceType: {surfaceType}");
        }
    }
}