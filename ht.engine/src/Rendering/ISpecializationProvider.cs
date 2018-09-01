using VulkanCore;

namespace HT.Engine.Rendering
{
    internal interface ISpecializationProvider
    {
        SpecializationInfo GetSpecialization();
    }
}