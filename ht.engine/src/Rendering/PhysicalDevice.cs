using System;

using VulkanCore.Khr;

namespace HT.Engine.Rendering
{
    public sealed class PhysicalDevice
    {
        public string Name => properties.DeviceName;
        public bool DiscreteGPU => properties.DeviceType == VulkanCore.PhysicalDeviceType.DiscreteGpu;
        public int ID => properties.DeviceId;
        public int VendorID => properties.VendorId;
        public bool SupportsGraphics => graphicsFamilyQueueIndex >= 0;

        internal VulkanCore.PhysicalDevice VulkanPhysicalDevice => physicalDevice;
        internal VulkanCore.PhysicalDeviceFeatures Features => features;
        internal int GraphicsFamilyQueueIndex => graphicsFamilyQueueIndex;

        private readonly VulkanCore.PhysicalDevice physicalDevice;
        private readonly VulkanCore.PhysicalDeviceProperties properties;
        private readonly VulkanCore.PhysicalDeviceFeatures features;
        private readonly VulkanCore.PhysicalDeviceMemoryProperties memoryProperties;
        private readonly int graphicsFamilyQueueIndex = -1;

        internal PhysicalDevice(VulkanCore.PhysicalDevice physicalDevice)
        {
            this.physicalDevice = physicalDevice;
            this.properties = physicalDevice.GetProperties();
            this.features = physicalDevice.GetFeatures();
            this.memoryProperties = physicalDevice.GetMemoryProperties();

            //Find the graphics queue
            var queueFamilies = physicalDevice.GetQueueFamilyProperties();
            for (int i = 0; i < queueFamilies.Length; i++)
            {   
                bool isGraphicsQueue = queueFamilies[i].QueueFlags.HasFlag(VulkanCore.Queues.Graphics);
                if(isGraphicsQueue)
                    graphicsFamilyQueueIndex = i;
            }
        }

        public bool IsSurfaceSupported(Surface surface) 
            => SupportsGraphics && surface.DoesQueueSupportSurface(this, graphicsFamilyQueueIndex);
    }
}