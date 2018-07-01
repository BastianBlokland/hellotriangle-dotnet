using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;
using VulkanCore;

namespace HT.Engine.Rendering.Model
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    internal readonly struct Vertex : IEquatable<Vertex>
    {
        public const int SIZE = Float3.SIZE * 2 + Float2.SIZE * 2;

        //Data
        public readonly Float3 Position;
        public readonly Float3 Normal;
        public readonly Float2 Uv1;
        public readonly Float2 Uv2;

        internal Vertex(Float3 position, Float3 normal, Float2 uv1, Float2 uv2)
        {
            Position = position;
            Normal = normal;
            Uv1 = uv1;
            Uv2 = uv2;
        }

        //Equality
        public static bool operator ==(Vertex a, Vertex b) => a.Equals(b);

        public static bool operator !=(Vertex a, Vertex b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is Vertex && Equals((Vertex)obj);

        public bool Equals(Vertex other) => 
            other.Position == Position && 
            other.Normal == Normal &&
            other.Uv1 == Uv1 &&
            other.Uv2 == Uv2;

        public override int GetHashCode() =>
            Position.GetHashCode() ^ 
            Normal.GetHashCode() ^
            Uv1.GetHashCode() ^
            Uv2.GetHashCode();

        public bool Approx(Vertex other) =>
            Position.Approx(other.Position) &&
            Normal.Approx(other.Normal) &&
            Uv1.Approx(other.Uv1) &&
            Uv2.Approx(other.Uv2);

        public override string ToString() => 
            $"(Position: {Position}, Normal: {Normal}, Uv1: {Uv1}, Uv2: {Uv2})";

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
                //Uv1
                new VertexInputAttributeDescription(
                    location: 2,
                    binding: 0,
                    format: Format.R32G32SFloat, //float2
                    offset: Float3.SIZE * 2 //In bytes from the beginning of the struct
                ),
                //Uv2
                new VertexInputAttributeDescription(
                    location: 3,
                    binding: 0,
                    format: Format.R32G32SFloat, //float2
                    offset: Float3.SIZE * 2 + Float2.SIZE //In bytes from the beginning of the struct
                )
            };
    }
}