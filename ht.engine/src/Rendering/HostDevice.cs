using System;
using System.Collections.Generic;

using HT.Engine.Math;
using HT.Engine.Utils;
using VulkanCore.Khr;
using VulkanCore.Mvk;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class HostDevice
    {
        //Properties
        public string Name => properties.DeviceName;
        public bool IsDiscreteGPU => properties.DeviceType == PhysicalDeviceType.DiscreteGpu;

        //Internal properties
        internal PhysicalDeviceLimits Limits => properties.Limits;

        //Data
        private readonly PhysicalDevice physicalDevice;
        private readonly SurfaceType surfaceType;
        private readonly Logger logger;
        private readonly PhysicalDeviceProperties properties;
        private readonly PhysicalDeviceMemoryProperties deviceMemoryProperties;
        private readonly PhysicalDeviceFeatures supportedFeatures;
        private readonly ExtensionProperties[] availableExtensions;
        private readonly QueueFamilyProperties[] queueFamilies;

        internal HostDevice(
            PhysicalDevice vulkanPhysicaldevice,
            SurfaceType surfaceType,
            Logger logger = null)
        {
            if (vulkanPhysicaldevice == null)
                throw new ArgumentNullException(nameof(vulkanPhysicaldevice));
            this.physicalDevice = vulkanPhysicaldevice;
            this.surfaceType = surfaceType;
            this.logger = logger;
            this.properties = vulkanPhysicaldevice.GetProperties();
            this.deviceMemoryProperties = vulkanPhysicaldevice.GetMemoryProperties();
            this.supportedFeatures = vulkanPhysicaldevice.GetFeatures();
            this.availableExtensions = vulkanPhysicaldevice.EnumerateExtensionProperties();
            this.queueFamilies = vulkanPhysicaldevice.GetQueueFamilyProperties();
            
            logger?.Log(nameof(HostDevice), $"Found device: {Name}");
            logger?.LogList(nameof(HostDevice), $"{Name} available extensions:", availableExtensions);
        }

        internal int GetMemoryType(MemoryProperties properties, int supportedTypesFilter = ~0)
        {
            for (int i = 0; i < deviceMemoryProperties.MemoryTypes.Length; i++)
            {
                if (supportedTypesFilter.HasBitSet(i) &&
                    (deviceMemoryProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
                    return i;
            }
            throw new Exception(
                $"[{nameof(HostDevice)}] Device {Name} has no memory-type that fits requirements");
        }

        internal SurfaceCapabilitiesKhr GetCurrentCapabilities(SurfaceKhr surface)
            => physicalDevice.GetSurfaceCapabilitiesKhr(surface);

        internal (Format imageFormat, ColorSpaceKhr colorspace) GetSurfaceFormat(SurfaceKhr surface)
        {
            SurfaceFormatKhr[] formats = physicalDevice.GetSurfaceFormatsKhr(surface);

            //If the device only returns 1 options for this surface and it contains 'Undefined' it 
            //means that the device doesn't care and we can pick.
            if (formats.Length == 1 && formats[0].Format == VulkanCore.Format.Undefined)
                return (Format.B8G8R8A8UNorm, ColorSpaceKhr.SRgbNonlinear);

            //If the device has preference then we check if it supports the combo we prefer
            for (int i = 0; i < formats.Length; i++)
            {
                if (formats[i].Format == Format.B8G8R8A8UNorm &&
                    formats[i].ColorSpace == ColorSpaceKhr.SRgbNonlinear)
                    return (formats[i].Format, formats[i].ColorSpace);
            }
            
            //If our preference is not there then we take the first that is supported
            if (formats.Length > 0) return (formats[0].Format, formats[0].ColorSpace);

            throw new Exception(
                $"[{nameof(HostDevice)}] Device {Name} doesn't support any format for use with given surface");
        }

        internal PresentModeKhr GetPresentMode(SurfaceKhr surface)
        {
            PresentModeKhr[] modes = physicalDevice.GetSurfacePresentModesKhr(surface);

            //If mailbox is present then go for that, it is basically having 1 frame being displayed
            //and multiple frames in the background being rendered to, and also allows to redraw 
            //those in the background, this allows for things like triple-buffering
            for (int i = 0; i < modes.Length; i++)
                if (modes[i] == PresentModeKhr.Mailbox)
                    return PresentModeKhr.Mailbox;

            //If mailbox is not supported then we go for Fifo
            //Fifo is basically uses 2 frame's, 1 thats being displayed right now and one that is 
            //being rendered to. When rendering is done but the previous frame is not done being 
            //displayed then the program has to wait.
            //According to spec this must be available on all platforms
            return PresentModeKhr.Fifo;
        }

        internal int GetSwapchainCount(SurfaceKhr surface)
        {
            SurfaceCapabilitiesKhr capabilities = GetCurrentCapabilities(surface);

            //1 more then min to support triple buffering
            int count = capabilities.MinImageCount + 1;
            
            //If the capabilities specify a max then clamp to that
            if (capabilities.MaxImageCount > 0)
                count = IntUtils.Min(count, capabilities.MaxImageCount);
            return count;
        }

        internal bool IsFormatSupported(Format format, ImageTiling tiling, FormatFeatures features)
        {
            var properties = physicalDevice.GetFormatProperties(format);
            switch (tiling)
            {
                case ImageTiling.Linear:
                    return (properties.LinearTilingFeatures & features) == features;
                case ImageTiling.Optimal:
                    return (properties.OptimalTilingFeatures & features) == features;
            }
            return false;
        }

        internal (Device logicalDevice, Queue graphicsQueue, Queue presentQueue, IList<string>) CreateLogicalDevice(
            SurfaceKhr surface, HostDeviceRequirements deviceRequirements)
        {
            if (!AreRequirementsMet(deviceRequirements))
                throw new Exception(
                    $"[{nameof(HostDevice)}] Device '{Name}' does not support all device-requirements");

            string[] requiredExtensions = GetRequiredExtensions(surfaceType);
            if (!AreExtensionsAvailable(requiredExtensions))
                throw new Exception(
                    $"[{nameof(HostDevice)}] Device '{Name}' does not support required extensions");
            var extensionsToEnable = new List<string>(requiredExtensions);

            //Add any optional extensions IF its supported by this host
            string[] optionalExtensions = GetOptionalExtensions(surfaceType);
            for (int i = 0; i < optionalExtensions.Length; i++)
                if (IsExtensionAvailable(optionalExtensions[i]))
                    extensionsToEnable.Add(optionalExtensions[i]);

            int? graphicsQueueFamilyIndex = GetGraphicsQueueFamilyIndex();
            if (graphicsQueueFamilyIndex == null)
                throw new Exception(
                    $"[{nameof(HostDevice)}] Device '{Name}' does not support graphics");

            int? presentQueueFamilyIndex = GetPresentQueueFamilyIndex(surface);
            if (presentQueueFamilyIndex == null)
                throw new Exception(
                    $"[{nameof(HostDevice)}] Device '{Name}' does not support presenting to the given surface");

            List<VulkanCore.DeviceQueueCreateInfo> queueCreateInfos = new List<DeviceQueueCreateInfo>();
            queueCreateInfos.Add(new VulkanCore.DeviceQueueCreateInfo(
                graphicsQueueFamilyIndex.Value, queueCount: 1, queuePriorities: 1f));
            //If the present queue and graphics queues are not the same we also need to create a present queue
            if (graphicsQueueFamilyIndex.Value != presentQueueFamilyIndex.Value)
            {
                queueCreateInfos.Add(new VulkanCore.DeviceQueueCreateInfo(
                    presentQueueFamilyIndex.Value, queueCount: 1, queuePriorities: 1f));
            }
            
            VulkanCore.DeviceCreateInfo createInfo = new VulkanCore.DeviceCreateInfo(
                queueCreateInfos: queueCreateInfos.ToArray(),
                enabledExtensionNames: extensionsToEnable.ToArray(),
                enabledFeatures: deviceRequirements.GetRequiredFeatures()
            );
            Device logicalDevice = physicalDevice.CreateDevice(createInfo);
            Queue graphicsQueue = logicalDevice.GetQueue(graphicsQueueFamilyIndex.Value, queueIndex: 0);
            Queue presentQueue = logicalDevice.GetQueue(presentQueueFamilyIndex.Value, queueIndex: 0);

            logger?.Log(nameof(HostDevice), $"Created logical-device ({Name})");
            logger?.LogList(nameof(HostDevice), $"Enabled extensions for logical-device:", extensionsToEnable);

            //Note: If we are running on molten-vk then we can set some specific mvk device config
            if (extensionsToEnable.Contains("VK_MVK_moltenvk"))
            {
                var deviceConfig = logicalDevice.GetMVKDeviceConfiguration();
                deviceConfig.DebugMode = DebugUtils.IS_DEBUG;
                deviceConfig.PerformanceTracking = DebugUtils.IS_DEBUG;
                deviceConfig.PerformanceLoggingFrameCount = DebugUtils.IS_DEBUG ? 300 : 0;
                logicalDevice.SetMVKDeviceConfiguration(deviceConfig);
            }

            return (logicalDevice, graphicsQueue, presentQueue, extensionsToEnable);
        }

        internal bool AreRequirementsMet(HostDeviceRequirements requirements)
            => requirements.DoesSupportRequirements(supportedFeatures);

        internal bool IsSurfaceSupported(SurfaceKhr surface)
        {
            //Verify that all our required extensions are available, that we have a graphics queue 
            //and a present queue
            return  AreExtensionsAvailable(GetRequiredExtensions(surfaceType)) &&
                    GetGraphicsQueueFamilyIndex() != null &&
                    GetPresentQueueFamilyIndex(surface) != null;
        }

        private int? GetTransferQueueFamilyIndex()
        {
            for (int i = 0; i < queueFamilies.Length; i++)
                if (queueFamilies[i].QueueFlags.HasFlag(Queues.Transfer))
                    return i;
            return null;
        }

        private int? GetGraphicsQueueFamilyIndex()
        {
            for (int i = 0; i < queueFamilies.Length; i++)
                if (queueFamilies[i].QueueFlags.HasFlag(Queues.Graphics))
                    return i;
            return null;
        }

        private int? GetPresentQueueFamilyIndex(SurfaceKhr surface)
        {
            for (int i = 0; i < queueFamilies.Length; i++)
            {
                //Verify that this queue-family supports presenting to the platform compositor
                switch (surfaceType)
                {
                    //On windows test if this queue supports presentation to the win32 compositor
                    case SurfaceType.HkrWin32: 
                        if (!physicalDevice.GetWin32PresentationSupportKhr(i))
                            continue;
                        break;
                    //On MacOS it is enough to know that the MVK extension is supported, if so we 
                    //can also present to the compositor
                }
                //Verify that the given surface is compatible with this queue-family
                if (physicalDevice.GetSurfaceSupportKhr(i, surface))
                    return i;
            }
            return null;
        }

        private bool AreExtensionsAvailable(string[] extensions)
        {
            for (int i = 0; i < extensions.Length; i++)
                if (!IsExtensionAvailable(extensions[i]))
                    return false;
            return true;
        }

        private bool IsExtensionAvailable(string extensionName)
        {
            for (int i = 0; i < availableExtensions.Length; i++)
                if (availableExtensions[i].ExtensionName == extensionName)
                    return true;
            return false;
        }

        private static string[] GetRequiredExtensions(SurfaceType surfaceType)
        {
            switch (surfaceType)
            {
                case SurfaceType.HkrWin32: return new [] { "VK_KHR_swapchain" };
                case SurfaceType.MvkMacOS: return new [] { "VK_KHR_swapchain" };
            }
            throw new Exception($"[{nameof(HostDevice)}] Unknown surfaceType: {surfaceType}");
        }

        private static string[] GetOptionalExtensions(SurfaceType surfaceType)
        {
            #if DEBUG
                return new [] { "VK_EXT_debug_marker" };
            #else
                return new string[0];
            #endif
        }
    }
}