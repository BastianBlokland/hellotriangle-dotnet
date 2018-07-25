using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Resources
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    public readonly struct InstanceData : IEquatable<InstanceData>
    {
        public const int SIZE = Float4x4.SIZE;
        
        //Data
        public readonly Float4x4 ModelMatrix;
        
        public InstanceData(Float4x4 modelMatrix) => ModelMatrix = modelMatrix;

        //Equality
        public static bool operator ==(InstanceData a, InstanceData b) => a.Equals(b);

        public static bool operator !=(InstanceData a, InstanceData b) => !a.Equals(b);

        public override bool Equals(object obj) 
            => obj is InstanceData && Equals((InstanceData)obj);

        public bool Equals(InstanceData other) => other.ModelMatrix == ModelMatrix;

        public override int GetHashCode() => ModelMatrix.GetHashCode();

        public override string ToString() => 
$@"(
    ModelMatrix:
    {ModelMatrix}
)";

        internal static void AddAttributeDescriptions(
            int binding,
            ResizeArray<VertexInputAttributeDescription> attributes)
        {
            //Float4x4 matrix takes 4 'locations' and needs to be bound as 4 separate float4's
            attributes.Add(new VertexInputAttributeDescription(
                location: attributes.Count,
                binding: binding,
                format: Format.R32G32B32A32SFloat, //float4
                offset: 0)); //In bytes from the beginning of the struct
            attributes.Add(new VertexInputAttributeDescription(
                location: attributes.Count,
                binding: binding,
                format: Format.R32G32B32A32SFloat, //float4
                offset: Float4.SIZE)); //In bytes from the beginning of the struct
            attributes.Add(new VertexInputAttributeDescription(
                location: attributes.Count,
                binding: binding,
                format: Format.R32G32B32A32SFloat, //float4
                offset: Float4.SIZE * 2)); //In bytes from the beginning of the struct
            attributes.Add(new VertexInputAttributeDescription(
                location: attributes.Count,
                binding: binding,
                format: Format.R32G32B32A32SFloat, //float4
                offset: Float4.SIZE * 3)); //In bytes from the beginning of the struct
        }
    }
}