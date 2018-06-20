using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;
using VulkanCore;

namespace HT.Engine.Rendering
{
    [StructLayout(LayoutKind.Sequential, Size = SIZE)]
    public struct Vertex : IEquatable<Vertex>
    {
        public const int SIZE = Float3.SIZE + Float4.SIZE;

        public readonly Float3 Position;
        public readonly Float4 Color;

        public Vertex(Float3 position, Float4 color)
        {
            Position = position;
            Color = color;
        }

        public static bool operator ==(Vertex a, Vertex b) => a.Equals(b);

        public static bool operator !=(Vertex a, Vertex b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is Vertex && Equals((Vertex)obj);

        public bool Equals(Vertex other) => other.Position == Position && other.Color == Color;

        public override int GetHashCode() => Position.GetHashCode() ^ Color.GetHashCode();

        public override string ToString() => $"(Position: {Position}, Color: {Color})";

        internal VertexInputBindingDescription GetBindingDescription()
            => new VertexInputBindingDescription(
                binding: 0,
                stride: SIZE,
                inputRate: VertexInputRate.Vertex);

        internal VertexInputAttributeDescription[] GetAttributeDescriptions()
            => new []
            {
                new VertexInputAttributeDescription(
                    location: 0,
                    binding: 0,
                    format: Format.R32G32B32SFloat, //float3
                    offset: 0 //In bytes from the beginning of the struct
                ),
                new VertexInputAttributeDescription(
                    location: 1,
                    binding: 0,
                    format: Format.R32G32B32A32SFloat, //float4
                    offset: Float3.SIZE //In bytes from the beginning of the struct
                )
            };
    }
}