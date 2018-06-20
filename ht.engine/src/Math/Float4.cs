using System;
using System.Runtime.InteropServices;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential, Size = SIZE)]
    public struct Float4 : IEquatable<Float4>
    {
        public const int SIZE = sizeof(float) * 4;

        public static readonly Float4 Zero = new Float4(x: 0f, y: 0f, z: 0f, w: 0f);
        public static readonly Float4 One = new Float4(x: 1f, y: 1f, z: 1f, w: 1f);

        //Alternative names to the components
        public float R => X;
        public float G => Y;
        public float B => Z;
        public float A => W;

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

        public override string ToString() => $"(X: {X}, Y: {Y}, Z: {Z}, W: {W})";
    }
}