using System;

using VulkanCore.Khr;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class GraphicsDevice : IDisposable
    {
        public string Name => properties.DeviceName;
        public bool DiscreteGPU => properties.DeviceType == PhysicalDeviceType.DiscreteGpu;
        public bool SupportsGraphics => graphicsFamilyQueueIndex >= 0;

        internal PhysicalDevice VulkanPhysicalDevice => vulkanPhysicalDevice;
        internal Device VulkanDevice
        {
            get
            {
                if(!initialized)
                    throw new Exception($"[{nameof(GraphicsDevice)}] {nameof(VulkanDevice)} is only available after initialization");
                return vulkanDevice;
            }
        }
        internal CommandPool VulkanCommandPool
        {
            get
            {
                if(!initialized)
                    throw new Exception($"[{nameof(GraphicsDevice)}] {nameof(VulkanCommandPool)} is only available after initialization");
                return commandPool;
            }
        }
        internal Queue GraphicsQueue
        {
            get
            {
                if(!initialized)
                    throw new Exception($"[{nameof(GraphicsDevice)}] {nameof(GraphicsQueue)} is only available after initialization");
                return graphicsQueue;
            }
        }
        internal CommandPool CommandPool
        {
            get
            {
                if(!initialized)
                    throw new Exception($"[{nameof(GraphicsDevice)}] {nameof(CommandPool)} is only available after initialization");
                return commandPool;
            }
        }

        //Info about the physical device
        private readonly PhysicalDevice vulkanPhysicalDevice;
        private readonly PhysicalDeviceProperties properties;
        private readonly PhysicalDeviceFeatures features;
        private readonly PhysicalDeviceMemoryProperties memoryProperties;
        private readonly int graphicsFamilyQueueIndex = -1;
        
        //Info about the initialized device (requires initialization)
        private bool initialized;
        private Device vulkanDevice;
        private Queue graphicsQueue;
        private CommandPool commandPool;

        internal GraphicsDevice(PhysicalDevice vulkanPhysicalDevice)
        {
            this.vulkanPhysicalDevice = vulkanPhysicalDevice;
            this.properties = vulkanPhysicalDevice.GetProperties();
            this.features = vulkanPhysicalDevice.GetFeatures();
            this.memoryProperties = vulkanPhysicalDevice.GetMemoryProperties();

            //Find the graphics queue
            var queueFamilies = vulkanPhysicalDevice.GetQueueFamilyProperties();
            for (int i = 0; i < queueFamilies.Length; i++)
            {   
                bool isGraphicsQueue = queueFamilies[i].QueueFlags.HasFlag(VulkanCore.Queues.Graphics);
                if(isGraphicsQueue)
                    graphicsFamilyQueueIndex = i;
            }
        }

        public void Initialize()
        {
            if(initialized)
                throw new Exception($"[{nameof(GraphicsDevice)}] Allready initialized");
            
            VulkanCore.DeviceQueueCreateInfo[] queueCreateInfos = new []
            {
                //Create a graphics queue
                new VulkanCore.DeviceQueueCreateInfo(graphicsFamilyQueueIndex, queueCount: 1, queuePriorities: 1f)
            };
            VulkanCore.DeviceCreateInfo createInfo = new VulkanCore.DeviceCreateInfo
            (
                queueCreateInfos: queueCreateInfos,
                enabledExtensionNames: new [] { "VK_KHR_swapchain" },
                enabledFeatures: features //Enable all the features
            );
            vulkanDevice = vulkanPhysicalDevice.CreateDevice(createInfo);

            //Get the queue we created with this device
            graphicsQueue = vulkanDevice.GetQueue(graphicsFamilyQueueIndex, queueIndex: 0);

            //Create a commandpool for the graphics queue
            commandPool = vulkanDevice.CreateCommandPool(new CommandPoolCreateInfo(graphicsFamilyQueueIndex, CommandPoolCreateFlags.ResetCommandBuffer));
            initialized = true;
        }

        public bool IsSurfaceSupported(Surface surface) =>  SupportsGraphics && 
                                                            surface.DoesQueueSupportSurface(this, graphicsFamilyQueueIndex);

        public void Dispose()
        {
            if(initialized)
            {
                commandPool.Dispose();
                vulkanDevice.Dispose();
                initialized = false;
            }
        }

        internal Format FindDepthStencilFormat()
        {
            //Find a depth format that is supported, prefer the high precision ones
            //and fall back to a lower precision if not available
            Format[] preferredFormats = new []
            {
                Format.D32SFloatS8UInt,
                Format.D32SFloat,
                Format.D24UNormS8UInt,
                Format.D16UNormS8UInt,
                Format.D16UNorm
            };
            for (int i = 0; i < preferredFormats.Length; i++)
            {
                //Test if this format can be used as a depth + stencil format
                var properties = vulkanPhysicalDevice.GetFormatProperties(preferredFormats[i]);
                if(properties.OptimalTilingFeatures.HasFlag(FormatFeatures.DepthStencilAttachment))
                    return preferredFormats[i];
            }
            throw new Exception($"[{nameof(GraphicsDevice)}] Device {Name} doesn't support any depth + stencil format");
        }
    }
}