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
        private readonly PhysicalDeviceMemoryProperties memoryProperties;
        private readonly ExtensionProperties[] availableExtensions;
        private readonly int graphicsFamilyQueueIndex = -1;

        internal HostDevice(PhysicalDevice vulkanPhysicalDevice, SurfaceType surfaceType, Logger logger = null)
        {
            this.physicalDevice = vulkanPhysicalDevice;
            this.surfaceType = surfaceType;
            this.logger = logger;
            this.properties = vulkanPhysicalDevice.GetProperties();
            this.memoryProperties = vulkanPhysicalDevice.GetMemoryProperties();
            this.availableExtensions = vulkanPhysicalDevice.EnumerateExtensionProperties();

            //Find the graphics queue
            var queueFamilies = vulkanPhysicalDevice.GetQueueFamilyProperties();
            for (int i = 0; i < queueFamilies.Length; i++)
            {   
                bool isGraphicsQueue = queueFamilies[i].QueueFlags.HasFlag(Queues.Graphics);
                if(isGraphicsQueue)
                    graphicsFamilyQueueIndex = i;
            }

            logger?.Log(nameof(HostDevice), $"Found device: {Name}");
            logger?.LogList(nameof(HostDevice), $"{Name} available extensions:", availableExtensions);
        }

        internal (Device logicalDevice, Queue graphicsQueue) CreateLogicalDevice()
        {
            //Verify that this physicalDevice supports graphics
            if(graphicsFamilyQueueIndex < 0)
                new Exception($"[{nameof(HostDevice)}] Device '{Name}' does not support graphics");

            string[] requiredExtensions = GetRequiredExtensions(surfaceType);

            //Verify that all the required extensions are available
            for (int i = 0; i < requiredExtensions.Length; i++)
                if(!IsExtensionAvailable(requiredExtensions[i]))
                    new Exception($"[{nameof(HostDevice)}] Device '{Name}' does not support required extension: {requiredExtensions[i]}");
            
            VulkanCore.DeviceQueueCreateInfo[] queueCreateInfos = new []
            {
                //Create a graphics queue
                new VulkanCore.DeviceQueueCreateInfo(graphicsFamilyQueueIndex, queueCount: 1, queuePriorities: 1f)
            };
            VulkanCore.DeviceCreateInfo createInfo = new VulkanCore.DeviceCreateInfo
            (
                queueCreateInfos: queueCreateInfos,
                enabledExtensionNames: requiredExtensions,
                enabledFeatures: new PhysicalDeviceFeatures() //Require no special features atm
            );
            Device logicalDevice = physicalDevice.CreateDevice(createInfo);
            Queue graphicsQueue = logicalDevice.GetQueue(graphicsFamilyQueueIndex, queueIndex: 0);

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

            return (logicalDevice, graphicsQueue);
        }

        internal bool IsSurfaceSupported(SurfaceKhr surface)
        {
            //Verify that we have a graphics queue-family on this device
            if(graphicsFamilyQueueIndex < 0)
                return false;

            //Verify that all the required extensions are present on this device
            string[] requiredExtensions = GetRequiredExtensions(surfaceType);
            for (int i = 0; i < requiredExtensions.Length; i++)
                if(!IsExtensionAvailable(requiredExtensions[i]))
                    return false;
            
            //Verify that this device supports presenting to the platform compositor
            switch(surfaceType)
            {
                //On windows test if this queue supports presentation to the win32 compositor
                case SurfaceType.HkrWin32: 
                    if(!physicalDevice.GetWin32PresentationSupportKhr(graphicsFamilyQueueIndex))
                        return false;
                    break;
                //On MacOS it is enough to know that the MVK extension is supported, if so we can
                //also present to the compositor
            }

            //Verify that the given surface is compatible with this device
            return physicalDevice.GetSurfaceSupportKhr(graphicsFamilyQueueIndex, surface);
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
                case SurfaceType.MvkMacOS: return new [] { "VK_KHR_swapchain", "VK_MVK_macos_surface", "VK_MVK_moltenvk" };
            }
            throw new Exception($"[{nameof(HostDevice)}] Unknown surfaceType: {surfaceType}");
        }
    }
}