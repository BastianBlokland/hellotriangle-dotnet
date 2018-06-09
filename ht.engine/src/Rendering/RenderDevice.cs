using System;

namespace HT.Engine.Rendering
{
    public sealed class RenderDevice : IDisposable
    {
        public readonly PhysicalDevice PhysicalDevice;

        private readonly VulkanCore.Device vulkanDevice;
        private bool disposed;

        public RenderDevice(PhysicalDevice physicalDevice)
        {
            if(!physicalDevice.SupportsGraphics)
                throw new Exception($"[{nameof(RenderDevice)}] physicalDevice {physicalDevice.Name} doesn't support graphics");
            PhysicalDevice = physicalDevice;
            
            VulkanCore.DeviceQueueCreateInfo[] queueCreateInfos = new []
            {
                //Create a graphics queue
                new VulkanCore.DeviceQueueCreateInfo(physicalDevice.GraphicsFamilyQueueIndex, queueCount: 1, queuePriorities: 1f)
            };
            VulkanCore.DeviceCreateInfo createInfo = new VulkanCore.DeviceCreateInfo
            (
                queueCreateInfos: queueCreateInfos,
                enabledExtensionNames: new [] { "VK_KHR_swapchain" },
                enabledFeatures: physicalDevice.Features //Enable all the features
            );
            vulkanDevice = physicalDevice.VulkanPhysicalDevice.CreateDevice(createInfo);
        }

        public void Dispose()
        {
            if(!disposed)
            {
                vulkanDevice.Dispose();
                disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if(disposed)
                throw new Exception($"[{nameof(RenderDevice)}] Allready disposed!");
        }
    }
}