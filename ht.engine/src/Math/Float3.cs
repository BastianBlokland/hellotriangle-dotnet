using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

using static System.Math;
using static System.MathF;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    public readonly struct Float3 : IFloatSet, IEquatable<Float3>
    {
        public const int SIZE = sizeof(float) * 3;

        //Presets
        public static readonly Float3 Zero = new Float3(0f, 0f, 0f);
        public static readonly Float3 One = new Float3(1f, 1f, 1f);
        public static readonly Float3 Up = new Float3(0f, 1f, 0f);
        public static readonly Float3 Down = new Float3(0f, -1f, 0f);
        public static readonly Float3 Right = new Float3(1f, 0f, 0f);
        public static readonly Float3 Left = new Float3(-1f, 0f, 0f);
        public static readonly Float3 Forward = new Float3(0f, 0f, 1f);
        public static readonly Float3 Backward = new Float3(0f, 0f, -1f);

        //Properties
        public float SquareMagnitude => X * X + Y * Y + Z * Z;
        public float Magnitude => Sqrt(SquareMagnitude);

        //Component swizzling
        public Float2 XY => new Float2(X, Y);
        public Float2 XZ => new Float2(X, Z);
        public Float3 YZX => new Float3(Y, Z, X);
        public Float3 ZXY => new Float3(Z, X, Y);
        public Float4 XYZ1 => new Float4(X, Y, Z, 1f);

        //Component index accessor
        public int ComponentCount => 3;

        public float this[int i]
        {
            get 
            {
                switch (i)
                {
                    case 0: return X;
                    case 1: return Y;
                    case 2: return Z;
                }
                throw new ArgumentOutOfRangeException(
                    $"[{nameof(Float3)}] No component at: {i}", nameof(i));
            }
        }

        //Alternative names to the components
        public float R => X;
        public float G => Y;
        public float B => Z;

        //Data
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public Float3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        //Tuple deconstruction syntax
        public void Deconstruct(out float x, out float y, out float z)
        {
            x = X;
            y = Y;
            z = Z;
        }

        //Arithmetic methods
        public static float SquareDistance(Float3 a, Float3 b) => (b - a).SquareMagnitude;
        public static float Distance(Float3 a, Float3 b) => (b - a).Magnitude;

        public static Float3 Normalize(Float3 val)
        {
            float length = val.Magnitude;
            if (length <= 0f)
                throw new Exception($"[{nameof(Float3)}] Length must be larger then 0");
            if (length == 1f)
                return val;
            return val / length;
        }

        public static Float3 FastNormalize(Float3 val, int precision = 2)
        {
            float sqrLength = val.SquareMagnitude;
            #if DEBUG
            if (sqrLength <= 0f)
                throw new Exception($"[{nameof(Float3)}] Length must be larger then 0");
            #endif
            if (sqrLength == 1f)
                return val;
            return val * FloatUtils.FastInverseSquareRoot(sqrLength, precision);
        }

        public static Float3 Cross(Float3 a, Float3 b) => new Float3(
            x: a.Y * b.Z - a.Z * b.Y,
            y: a.Z * b.X - a.X * b.Z,
            z: a.X * b.Y - a.Y * b.X);

        public static float Dot(Float3 a, Float3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        //Arithmetic operators
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

        public static Float3 operator -(Float3 val) => new Float3(-val.X, -val.Y, -val.Z);

        //Equality
        public static bool operator ==(Float3 a, Float3 b) => a.Equals(b);

        public static bool operator !=(Float3 a, Float3 b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is Float3 && Equals((Float3)obj);

        public bool Equals(Float3 other) => other.X == X && other.Y == Y && other.Z == Z;

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();

        public bool Approx(Float3 other, float maxDifference = .0001f) =>
            X.Approx(other.X, maxDifference) && 
            Y.Approx(other.Y, maxDifference) && 
            Z.Approx(other.Z, maxDifference);

        public override string ToString() => $"(X: {X}, Y: {Y}, Z: {Z})";

        //Conversions
        public static implicit operator Float3((float x, float y, float z) tuple)
            => new Float3(tuple.x, tuple.y, tuple.z);
    }
}