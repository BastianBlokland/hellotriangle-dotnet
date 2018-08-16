using VulkanCore;

namespace HT.Engine.Rendering
{
    public readonly struct HostDeviceRequirements
    {
        internal PhysicalDeviceFeatures GetRequiredFeatures()
            => new PhysicalDeviceFeatures
            {
                SamplerAnisotropy = true,
                DepthBiasClamp = true,
                DepthClamp = true
            };

        internal bool DoesSupportRequirements(PhysicalDeviceFeatures features) => 
            features.SamplerAnisotropy &&
            features.DepthBiasClamp &&
            features.DepthClamp;
    }
}