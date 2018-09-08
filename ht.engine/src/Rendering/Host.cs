using System;
using System.Collections.Generic;

using HT.Engine.Math;
using HT.Engine.Platform;
using HT.Engine.Utils;
using VulkanCore;
using VulkanCore.Mvk;
using VulkanCore.Khr;
using VulkanCore.Ext;

namespace HT.Engine.Rendering
{
    public sealed class Host : IDisposable
    {
        private readonly Platform.INativeApp nativeApp;
        private readonly string appName;
        private readonly int appVersion;
        private readonly Logger logger;
        private readonly LayerProperties[] availableLayers;
        private readonly ExtensionProperties[] availbleExtensions;
        private readonly Instance instance;
        private readonly DebugReportCallbackExt debugCallback;
        private readonly HostDevice[] hostDevices;
        private bool disposed;

        public Host(
            Platform.INativeApp nativeApp,
            string appName,
            int appVersion,
            Logger logger = null)
        {
            if (nativeApp == null)
                throw new ArgumentNullException(nameof(nativeApp));
            this.nativeApp = nativeApp;
            this.appName = appName;
            this.appVersion = appVersion;
            this.logger = logger;

            availableLayers = Instance.EnumerateLayerProperties();
            logger?.LogList(nameof(Host), "Available layers:", availableLayers);
            availbleExtensions = Instance.EnumerateExtensionProperties();
            logger?.LogList(nameof(Host), "Available extensions:", availbleExtensions);

            //Verify that all the required layers are available on this host
            var layersToEnable = new List<string>(GetRequiredLayers(nativeApp.SurfaceType));
            for (int i = 0; i < layersToEnable.Count; i++)
                if (!IsLayerAvailable(layersToEnable[i]))
                    throw new Exception(
                        $"[{nameof(Host)}] Required layer '{layersToEnable[i]}' is not available");

            //Verify that all the required extensions are available on this host
            var extensionsToEnable = new List<string>(GetRequiredExtensions(nativeApp.SurfaceType));
            for (int i = 0; i < extensionsToEnable.Count; i++)
                if (!IsExtensionAvailable(extensionsToEnable[i]))
                    throw new Exception(
                        $"[{nameof(Host)}] Required extension '{extensionsToEnable[i]}' is not available");

            //Add any optional layers IF its supported by this host
            string[] optionalLayers = GetOptionalLayers(nativeApp.SurfaceType);
            for (int i = 0; i < optionalLayers.Length; i++)
                if (IsLayerAvailable(optionalLayers[i]))
                    layersToEnable.Add(optionalLayers[i]);

            //Add any optional extensions IF its supported by this host
            string[] optionalExtensions = GetOptionalExtensions(nativeApp.SurfaceType);
            for (int i = 0; i < optionalExtensions.Length; i++)
                if (IsExtensionAvailable(optionalExtensions[i]))
                    extensionsToEnable.Add(optionalExtensions[i]);

            InstanceCreateInfo createInfo = new InstanceCreateInfo(
                appInfo: new ApplicationInfo(
                    applicationName: appName,
                    applicationVersion: appVersion,
                    engineName: Info.NAME,
                    engineVersion: Info.VERSION,
                    apiVersion: new VulkanCore.Version(major: 1, minor: 0, patch: 69)
                ),
                enabledLayerNames: layersToEnable.ToArray(),
                enabledExtensionNames: extensionsToEnable.ToArray()
            );
            instance = new Instance(createInfo);

            logger?.Log(nameof(Host), "Created instance");
            logger?.LogList(nameof(Host), "Enabled layers:", layersToEnable);
            logger?.LogList(nameof(Host), "Enabled extensions:", extensionsToEnable);

            #if DEBUG
            if (extensionsToEnable.Contains("VK_EXT_debug_report"))
            {
                debugCallback = instance.CreateDebugReportCallbackExt(
                    new DebugReportCallbackCreateInfoExt(
                        //We want to handle everthing except for info reports
                        flags: DebugReportFlagsExt.All & ~DebugReportFlagsExt.Information,
                        callback: OnDebugReport));
                logger?.Log(nameof(Host), "Enabled debug callback");
            }
            #else
                debugCallback = null;
            #endif

            //Get all the devices in this host
            PhysicalDevice[] physicalDevices = instance.EnumeratePhysicalDevices();
            hostDevices = new HostDevice[physicalDevices.Length];
            for (int i = 0; i < hostDevices.Length; i++)
                hostDevices[i] = new HostDevice(physicalDevices[i], nativeApp.SurfaceType, logger);
        }

        public Window CreateWindow(
            Int2 windowSize,
            HostDeviceRequirements deviceRequirements,
            bool preferDiscreteDevice = true)
        {
            ThrowIfDisposed();
            INativeWindow nativeWindow = nativeApp.CreateWindow(
                size: windowSize,
                minSize: (x: 150, y: 150),
                title: string.Empty);
            SurfaceKhr surface = CreateSurface(nativeWindow);
            HostDevice graphicsDevice = FindSuitableDevice(
                surface,
                deviceRequirements,
                preferDiscreteDevice);
            return new Window(
                title: $"{appName} - {appVersion}",
                nativeWindow, surface, graphicsDevice, deviceRequirements, logger);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                debugCallback?.Dispose();
                instance.Dispose();
                logger?.Log(nameof(Host), "Destroyed instance");
                disposed = true;
            }
        }

        private SurfaceKhr CreateSurface(INativeWindow nativeWindow)
        {
            ThrowIfDisposed();
            switch (nativeApp.SurfaceType)
            {
                case SurfaceType.MvkMacOS:
                    return instance.CreateMacOSSurfaceMvk(
                        new MacOSSurfaceCreateInfoMvk(nativeWindow.OSViewHandle));
                case SurfaceType.HkrWin32:
                    return instance.CreateWin32SurfaceKhr(
                        new Win32SurfaceCreateInfoKhr(
                            nativeWindow.OSInstanceHandle,
                            nativeWindow.OSViewHandle));
            }
            throw new Exception(
                $"[{nameof(Host)}] Unable to create surface for unknown surfaceType: {nativeApp.SurfaceType}");
        }

        private HostDevice FindSuitableDevice(
            SurfaceKhr surface,
            HostDeviceRequirements deviceRequirements,
            bool preferDiscreteDevice = true)
        {
            ThrowIfDisposed();
            List<HostDevice> supportedDevices = new List<HostDevice>();

            //Find all devices that support the given surface
            for (int i = 0; i < hostDevices.Length; i++)
            {
                if (hostDevices[i].IsSurfaceSupported(surface) && 
                    hostDevices[i].AreRequirementsMet(deviceRequirements))
                {
                    supportedDevices.Add(hostDevices[i]);
                }
            }

            if (supportedDevices.IsEmpty())
                throw new Exception($"[{nameof(Host)}] Unable to find a supported device");

            //If we have a supported discreate gpu and we prefer a discrete one then we pick that
            for (int i = 0; i < supportedDevices.Count; i++)
                if (supportedDevices[i].IsDiscreteGPU == preferDiscreteDevice)
                    return supportedDevices[i];

            return supportedDevices[0];
        }

        private bool IsLayerAvailable(string layerName)
        {
            for (int i = 0; i < availableLayers.Length; i++)
                if (availableLayers[i].LayerName == layerName)
                    return true;
            return false;
        }

        private bool IsExtensionAvailable(string extensionName)
        {
            for (int i = 0; i < availbleExtensions.Length; i++)
                if (availbleExtensions[i].ExtensionName == extensionName)
                    return true;
            return false;
        }

        #if DEBUG
        private bool OnDebugReport(DebugReportCallbackInfo args)
        {
            logger?.Log(nameof(Host), $"[{args.Flags}] [{args.LayerPrefix}] {args.Message}");
            return false; //Returning false will keep the app running, returning true will abort
        }
        #endif

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(Host)}] Allready disposed");
        }

        private static string[] GetRequiredLayers(SurfaceType surfaceType)
        {
            switch (surfaceType)
            {
                case SurfaceType.HkrWin32: return new string[0];
                case SurfaceType.MvkMacOS: return new string[0];
            }
            throw new Exception($"[{nameof(Host)}] Unknown surfaceType: {surfaceType}");
        }

        private static string[] GetOptionalLayers(SurfaceType surfaceType)
        {
            #if DEBUG
                return new [] { "VK_LAYER_LUNARG_standard_validation" };
            #else
                return new string[0];
            #endif
        }

        private static string[] GetRequiredExtensions(SurfaceType surfaceType)
        {
            switch (surfaceType)
            {
                case SurfaceType.HkrWin32:
                    return new [] { "VK_KHR_surface", "VK_KHR_win32_surface" };
                case SurfaceType.MvkMacOS:
                    return new [] { "VK_KHR_surface", "VK_MVK_macos_surface" };
            }
            throw new Exception($"[{nameof(Host)}] Unknown surfaceType: {surfaceType}");
        }

        private static string[] GetOptionalExtensions(SurfaceType surfaceType)
        {
            #if DEBUG
                return new [] { "VK_EXT_debug_report" };
            #else
                return new string[0];
            #endif
        }
    }
}