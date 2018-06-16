using System;

using HT.Engine.Utils;
using VulkanCore.Khr;
using VulkanCore.Mvk;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class HostDevice
    {
        public string Name => properties.DeviceName;
        public bool IsDiscreteGPU => properties.DeviceType == PhysicalDeviceType.DiscreteGpu;

        private readonly PhysicalDevice physicalDevice;
        private readonly SurfaceType surfaceType;
        private readonly Logger logger;
        private readonly PhysicalDeviceProperties properties;
        private readonly ExtensionProperties[] availableExtensions;
        private readonly QueueFamilyProperties[] queueFamilies;

        internal HostDevice(PhysicalDevice vulkanPhysicalDevice, SurfaceType surfaceType, Logger logger = null)
        {
            this.physicalDevice = vulkanPhysicalDevice;
            this.surfaceType = surfaceType;
            this.logger = logger;
            this.properties = vulkanPhysicalDevice.GetProperties();
            this.availableExtensions = vulkanPhysicalDevice.EnumerateExtensionProperties();
            this.queueFamilies = vulkanPhysicalDevice.GetQueueFamilyProperties();

            logger?.Log(nameof(HostDevice), $"Found device: {Name}");
            logger?.LogList(nameof(HostDevice), $"{Name} available extensions:", availableExtensions);
        }

        internal (Device logicalDevice, Queue graphicsQueue, Queue presentQueue) CreateLogicalDevice(SurfaceKhr surface)
        {
            string[] requiredExtensions = GetRequiredExtensions(surfaceType);
            if(!AreExtensionsAvailable(requiredExtensions))
                throw new Exception($"[{nameof(HostDevice)}] Device '{Name}' does not support required extensions");
            
            int? graphicsQueueFamilyIndex = GetGraphicsQueueFamilyIndex();
            if(graphicsQueueFamilyIndex == null)
                throw new Exception($"[{nameof(HostDevice)}] Device '{Name}' does not support graphics");

            int? presentQueueFamilyIndex = GetPresentQueueFamilyIndex(surface);
            if(presentQueueFamilyIndex == null)
                throw new Exception($"[{nameof(HostDevice)}] Device '{Name}' does not support presenting to the given surface");

            VulkanCore.DeviceQueueCreateInfo[] queueCreateInfos = new []
            {
                //Create a graphics queue
                new VulkanCore.DeviceQueueCreateInfo(graphicsQueueFamilyIndex.Value, queueCount: 1, queuePriorities: 1f),
                //Create a present queue
                new VulkanCore.DeviceQueueCreateInfo(presentQueueFamilyIndex.Value, queueCount: 1, queuePriorities: 1f)
            };
            VulkanCore.DeviceCreateInfo createInfo = new VulkanCore.DeviceCreateInfo
            (
                queueCreateInfos: queueCreateInfos,
                enabledExtensionNames: requiredExtensions,
                enabledFeatures: new PhysicalDeviceFeatures() //No special features required atm
            );
            Device logicalDevice = physicalDevice.CreateDevice(createInfo);
            Queue graphicsQueue = logicalDevice.GetQueue(graphicsQueueFamilyIndex.Value, queueIndex: 0);
            Queue presentQueue = logicalDevice.GetQueue(presentQueueFamilyIndex.Value, queueIndex: 0);

            logger?.Log(nameof(HostDevice), $"Created logical-device ({Name})");
            logger?.LogList(nameof(HostDevice), $"Enabled extensions for logical-device:", requiredExtensions);

            //Note: If we are running on molten-vk then we can set some specific mvk device config
            if(requiredExtensions.Contains("VK_MVK_moltenvk"))
            {
                var deviceConfig = logicalDevice.GetMVKDeviceConfiguration();
                deviceConfig.DebugMode = DebugUtils.IS_DEBUG;
                deviceConfig.PerformanceTracking = DebugUtils.IS_DEBUG;
                deviceConfig.PerformanceLoggingFrameCount = DebugUtils.IS_DEBUG ? 300 : 0;
                logicalDevice.SetMVKDeviceConfiguration(deviceConfig);
            }

            return (logicalDevice, graphicsQueue, presentQueue);
        }

        internal bool IsSurfaceSupported(SurfaceKhr surface)
        {
            //Verify that all our required extensions are available, that we have a graphics queue and a present queue
            return  AreExtensionsAvailable(GetRequiredExtensions(surfaceType)) &&
                    GetGraphicsQueueFamilyIndex() != null &&
                    GetPresentQueueFamilyIndex(surface) != null;
        }

        private int? GetGraphicsQueueFamilyIndex()
        {
            for (int i = 0; i < queueFamilies.Length; i++)
                if(queueFamilies[i].QueueFlags.HasFlag(Queues.Graphics))
                    return i;
            return null;
        }

        private int? GetPresentQueueFamilyIndex(SurfaceKhr surface)
        {
            for (int i = 0; i < queueFamilies.Length; i++)
            {
                //Verify that this queue-family supports presenting to the platform compositor
                switch(surfaceType)
                {
                    //On windows test if this queue supports presentation to the win32 compositor
                    case SurfaceType.HkrWin32: 
                        if(!physicalDevice.GetWin32PresentationSupportKhr(i))
                            continue;
                        break;
                    //On MacOS it is enough to know that the MVK extension is supported, if so we can
                    //also present to the compositor
                }
                //Verify that the given surface is compatible with this queue-family
                if(physicalDevice.GetSurfaceSupportKhr(i, surface))
                    return i;
            }
            return null;
        }

        private bool AreExtensionsAvailable(string[] extensions)
        {
            for (int i = 0; i < extensions.Length; i++)
                if(!IsExtensionAvailable(extensions[i]))
                    return false;
            return true;
        }

        private bool IsExtensionAvailable(string extensionName)
        {
            for (int i = 0; i < availableExtensions.Length; i++)
                if(availableExtensions[i].ExtensionName == extensionName)
                    return true;
            return false;
        }

        private static string[] GetRequiredExtensions(SurfaceType surfaceType)
        {
            switch(surfaceType)
            {
                case SurfaceType.HkrWin32: return new [] { "VK_KHR_swapchain" };
                case SurfaceType.MvkMacOS: return new [] { "VK_KHR_swapchain", "VK_MVK_moltenvk" };
            }
            throw new Exception($"[{nameof(HostDevice)}] Unknown surfaceType: {surfaceType}");
        }
    }
}