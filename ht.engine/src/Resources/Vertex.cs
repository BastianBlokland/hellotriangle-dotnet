using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;
using VulkanCore;

namespace HT.Engine.Resources
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    public readonly struct Vertex : IEquatable<Vertex>
    {
        public const int SIZE = Float3.SIZE * 2 + Float4.SIZE + Float2.SIZE * 2;

        //Data
        public readonly Float3 Position;
        public readonly Float4 Color;
        public readonly Float3 Normal;
        public readonly Float2 Uv1;
        public readonly Float2 Uv2;

        public Vertex(Float3 position, Float4 color, Float3 normal, Float2 uv1, Float2 uv2)
        {
            Position = position;
            Color = color;
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
            other.Color == Color &&
            other.Normal == Normal &&
            other.Uv1 == Uv1 &&
            other.Uv2 == Uv2;

        public override int GetHashCode() =>
            Position.GetHashCode() ^
            Color.GetHashCode() ^
            Normal.GetHashCode() ^
            Uv1.GetHashCode() ^
            Uv2.GetHashCode();

        public bool Approx(Vertex other) =>
            Position.Approx(other.Position) &&
            Color.Approx(other.Color) &&
            Normal.Approx(other.Normal) &&
            Uv1.Approx(other.Uv1) &&
            Uv2.Approx(other.Uv2);

        public override string ToString() => 
            $"(Position: {Position}, Color: {Color}, Normal: {Normal}, Uv1: {Uv1}, Uv2: {Uv2})";

        //Shader bindings
        internal static VertexInputBindingDescription GetBindingDescription()
            => new VertexInputBindingDescription(
                binding: 0,
                stride: SIZE,
                inputRate: VertexInputRate.Vertex);

        internal static VertexInputAttributeDescription[] GetAttributeDescriptions()
        {
            var attributes = new VertexInputAttributeDescription[5];
            int offset = 0;
            for (int i = 0; i < attributes.Length; i++)
            {
                switch(i)
                {
                case 0: //Position
                    attributes[i] = new VertexInputAttributeDescription(
                        location: i,
                        binding: 0,
                        format: Format.R32G32B32SFloat, //float3
                        offset: offset); //In bytes from the beginning of the struct
                    offset += Float3.SIZE;
                    break;
                case 1: //Color
                    attributes[i] = new VertexInputAttributeDescription(
                        location: i,
                        binding: 0,
                        format: Format.R32G32B32A32SFloat, //float4
                        offset: offset); //In bytes from the beginning of the struct
                    offset += Float4.SIZE;
                    break;
                case 2: //Normal
                    attributes[i] = new VertexInputAttributeDescription(
                        location: i,
                        binding: 0,
                        format: Format.R32G32B32SFloat, //float3
                        offset: offset); //In bytes from the beginning of the struct
                    offset += Float3.SIZE;
                    break;
                case 3: //Uv1
                    attributes[i] = new VertexInputAttributeDescription(
                        location: i,
                        binding: 0,
                        format: Format.R32G32SFloat, //float2
                        offset: offset); //In bytes from the beginning of the struct
                    offset += Float2.SIZE;
                    break;
                case 4: //Uv2
                    attributes[i] = new VertexInputAttributeDescription(
                        location: i,
                        binding: 0,
                        format: Format.R32G32SFloat, //float2
                        offset: offset); //In bytes from the beginning of the struct
                    offset += Float2.SIZE;
                    break;
                }
            }
            return attributes;
        }
    }
}