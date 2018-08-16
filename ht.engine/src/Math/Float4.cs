using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    public readonly struct Float4 : IFloatSet, IEquatable<Float4>
    {
        public const int SIZE = sizeof(float) * 4;

        //Presets
        public static readonly Float4 Zero = new Float4(0f, 0f, 0f, 0f);
        public static readonly Float4 Max = new Float4(
            float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue);
        public static readonly Float4 Min = new Float4(
            float.MinValue, float.MinValue, float.MinValue, float.MinValue);
        public static readonly Float4 One = new Float4(1f, 1f, 1f, 1f);

        //Component swizzling
        public Float3 XYZ => new Float3(X, Y, Z);
        public Float2 XY => new Float2(X, Y);

        //Component index accessor
        public int ComponentCount => 4;

        public float this[int i]
        {
            get 
            {
                switch (i)
                {
                    case 0: return X;
                    case 1: return Y;
                    case 2: return Z;
                    case 3: return W;
                }
                throw new ArgumentOutOfRangeException(
                    $"[{nameof(Float4)}] No component at: {i}", nameof(i));
            }
        }
        
        //Alternative names to the components
        public float R => X;
        public float G => Y;
        public float B => Z;
        public float A => W;

        //Data
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly float W;

        public Float4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        //Tuple deconstruction syntax
        public void Deconstruct(out float x, out float y, out float z, out float w)
        {
            x = X;
            y = Y;
            z = Z;
            w = W;
        }

        //Utilities
        public Float3 PerspectiveDivide() => XYZ / W;

        //Creation
        public static Float4 CreateFrom32Bit(byte x, byte y, byte z, byte w)
            //Note: Need to investigate if there is a after approx of / 255
            => new Float4(x / 255f, y / 255f, z / 255f, w / 255f);


        //Arithmetic operators
        public static Float4 operator +(Float4 left, Float4 right)
            => new Float4(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);

        public static Float4 operator -(Float4 left, Float4 right)
            => new Float4(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);

        public static Float4 operator *(Float4 left, Float4 right)
            => new Float4(left.X * right.X, left.Y * right.Y, left.Z * right.Z, left.W * right.W);

        public static Float4 operator *(Float4 left, float right)
            => new Float4(left.X * right, left.Y * right, left.Z * right, left.W * right);

        public static Float4 operator *(float left, Float4 right)
            => new Float4(left * right.X, left * right.Y, left * right.Z, left * right.W);

        public static Float4 operator /(Float4 left, float right)
            => new Float4(left.X / right, left.Y / right, left.Z / right, left.W / right);

        public static Float4 operator -(Float4 val) => new Float4(-val.X, -val.Y, -val.Z, -val.W);

        //Equality
        public static bool operator ==(Float4 a, Float4 b) => a.Equals(b);

        public static bool operator !=(Float4 a, Float4 b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is Float4 && Equals((Float4)obj);

        public bool Equals(Float4 other) => 
            other.X == X &&
            other.Y == Y &&
            other.Z == Z &&
            other.W == W;

        public override int GetHashCode() => 
            X.GetHashCode() ^
            Y.GetHashCode() ^
            Z.GetHashCode() ^
            W.GetHashCode();

        public bool Approx(Float4 other, float maxDifference = .0001f) =>
            X.Approx(other.X, maxDifference) && 
            Y.Approx(other.Y, maxDifference) && 
            Z.Approx(other.Z, maxDifference) &&
            W.Approx(other.W, maxDifference);

        public override string ToString() => $"(X: {X}, Y: {Y}, Z: {Z}, W: {W})";

        //Conversions
        public static implicit operator Float4((float x, float y, float z, float w) tuple)
            => new Float4(tuple.x, tuple.y, tuple.z, tuple.w);
    }
}