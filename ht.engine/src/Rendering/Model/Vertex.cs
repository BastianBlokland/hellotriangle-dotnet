using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;
using VulkanCore;

namespace HT.Engine.Rendering.Model
{
    [StructLayout(LayoutKind.Sequential, Size = SIZE)]
    internal struct Vertex : IEquatable<Vertex>
    {
        public const int SIZE = Float3.SIZE * 2 + Float2.SIZE;

        //Data
        public readonly Float3 Position;
        public readonly Float3 Normal;
        public readonly Float2 Uv;

        public Vertex(Float3 position, Float3 normal, Float2 uv)
        {
            Position = position;
            Normal = normal;
            Uv = uv;
        }

        //Equality
        public static bool operator ==(Vertex a, Vertex b) => a.Equals(b);

        public static bool operator !=(Vertex a, Vertex b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is Vertex && Equals((Vertex)obj);

        public bool Equals(Vertex other) => 
            other.Position == Position && 
            other.Normal == Normal &&
            other.Uv == Uv;

        public override int GetHashCode() =>
            Position.GetHashCode() ^ 
            Normal.GetHashCode() ^
            Uv.GetHashCode();

        public override string ToString() => 
            $"(Position: {Position}, Normal: {Normal}, Uv: {Uv})";

        //Shader bindings
        internal static VertexInputBindingDescription GetBindingDescription()
            => new VertexInputBindingDescription(
                binding: 0,
                stride: SIZE,
                inputRate: VertexInputRate.Vertex);

        internal static VertexInputAttributeDescription[] GetAttributeDescriptions()
            => new []
            {
                //Position
                new VertexInputAttributeDescription(
                    location: 0,
                    binding: 0,
                    format: Format.R32G32B32SFloat, //float3
                    offset: 0 //In bytes from the beginning of the struct
                ),
                //Normal
                new VertexInputAttributeDescription(
                    location: 1,
                    binding: 0,
                    format: Format.R32G32B32SFloat, //float3
                    offset: Float3.SIZE //In bytes from the beginning of the struct
                ),
                //Uv
                new VertexInputAttributeDescription(
                    location: 2,
                    binding: 0,
                    format: Format.R32G32SFloat, //float2
                    offset: Float3.SIZE * 2 //In bytes from the beginning of the struct
                )
            };
    }
}