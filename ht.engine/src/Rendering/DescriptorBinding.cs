using System;

namespace HT.Engine.Rendering
{
    /// <summary>
    /// Describes how many things you want to bind to your shader, order of the bindings is fixed so
    /// our descriptor-manager can reuse more. Order is allways first all uniform buffers and then 
    /// all image sampler, all with consecutive binding numbers
    /// </summary>
    internal readonly struct DescriptorBinding : IEquatable<DescriptorBinding>
    {
        //Properties
        internal int TotalBindings => UniformBufferCount + ImageSamplerCount;

        //Data
        internal readonly int UniformBufferCount;
        internal readonly int ImageSamplerCount;

        internal DescriptorBinding(int uniformBufferCount, int imageSamplerCount)
        {
            UniformBufferCount = uniformBufferCount;
            ImageSamplerCount = imageSamplerCount;
        }

        //Equality
        public static bool operator ==(DescriptorBinding a, DescriptorBinding b) => a.Equals(b);

        public static bool operator !=(DescriptorBinding a, DescriptorBinding b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is DescriptorBinding && Equals((DescriptorBinding)obj);

        public bool Equals(DescriptorBinding other) => 
            other.UniformBufferCount == UniformBufferCount && 
            other.ImageSamplerCount == ImageSamplerCount;

        public override int GetHashCode() =>
            UniformBufferCount.GetHashCode() ^
            ImageSamplerCount.GetHashCode();

        public override string ToString() => 
            $"(UniformBufferCount: {UniformBufferCount}, ImageSamplerCount: {ImageSamplerCount})";
    }
}