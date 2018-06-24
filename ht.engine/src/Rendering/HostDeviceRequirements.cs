using VulkanCore;

namespace HT.Engine.Rendering
{
    public struct HostDeviceRequirements
    {
        public readonly bool SamplerAnisotropy;

        public HostDeviceRequirements(bool samplerAnisotropy)
            => SamplerAnisotropy = samplerAnisotropy;

        internal PhysicalDeviceFeatures GetRequiredFeatures()
            => new PhysicalDeviceFeatures
            {
                SamplerAnisotropy = true 
            };
    }
}