using System;
using System.Runtime.InteropServices;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential, Size = SIZE)]
    public struct Float3 : IEquatable<Float3>
    {
        public const int SIZE = sizeof(float) * 3;

        public static readonly Float3 Zero = new Float3(x: 0f, y: 0f, z: 0f);
        public static readonly Float3 One = new Float3(x: 1f, y: 1f, z: 1f);
        public static readonly Float3 Up = new Float3(x: 0f, y: 1f, z: 0f);
        public static readonly Float3 Down = new Float3(x: 0f, y: -1f, z: 0f);
        public static readonly Float3 Right = new Float3(x: 1f, y: 0f, z: 0f);
        public static readonly Float3 Left = new Float3(x: -1f, y: 0f, z: 0f);
        public static readonly Float3 Forward = new Float3(x: 0f, y: 0f, z: 1f);
        public static readonly Float3 Backward = new Float3(x: 0f, y: 0f, z: -1f);

        //Alternative names to the components
        public float R => X;
        public float G => Y;
        public float B => Z;

        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public Float3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void Deconstruct(out float x, out float y, out float z)
        {
            x = X;
            y = Y;
            z = Z;
        }

        public static Float3 operator +(Float3 left, Float3 right)
            => new Float3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

        public static Float3 operator -(Float3 left, Float3 right)
            => new Float3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

        public static Float3 operator *(Float3 left, Float3 right)
            => new Float3(left.X * right.X, left.Y * right.Y, left.Z * right.Z);

        public static Float3 operator *(Float3 left, float right)
            => new Float3(left.X * right, left.Y * right, left.Z * right);

        public static Float3 operator *(float left, Float3 right)
            => new Float3(left * right.X, left * right.Y, left * right.Z);

        public static Float3 operator /(Float3 left, float right)
            => new Float3(left.X / right, left.Y / right, left.Z / right);

        public static bool operator ==(Float3 a, Float3 b) => a.Equals(b);

        public static bool operator !=(Float3 a, Float3 b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is Float3 && Equals((Float3)obj);

        public bool Equals(Float3 other) => other.X == X && other.Y == Y && other.Z == Z;

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();

        public override string ToString() => $"(X: {X}, Y: {Y}, Z: {Z})";

        public static implicit operator Float3((float x, float y, float z) tuple)
            => new Float3(tuple.x, tuple.y, tuple.z);
    }
}