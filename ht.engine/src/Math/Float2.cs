using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    public readonly struct Float2 : IFloatSet, IEquatable<Float2>
    {
        public const int SIZE = sizeof(float) * 2;

        //Presets
        public static readonly Float2 Zero = new Float2(0f, 0f);
        public static readonly Float2 Max = new Float2(float.MaxValue, float.MaxValue);
        public static readonly Float2 Min = new Float2(float.MinValue, float.MinValue);
        public static readonly Float2 One = new Float2(1f, 1f);
        public static readonly Float2 Up = new Float2(0f, 1f);
        public static readonly Float2 Down = new Float2(0f, -1f);
        public static readonly Float2 Right = new Float2(1f, 0f);
        public static readonly Float2 Left = new Float2(-1f, 0f);

        //Component swizzling
        public Float3 XY0 => new Float3(X, Y, 0f);
        public Float3 X0Y => new Float3(X, 0f, Y);

        //Component index accessor
        public int ComponentCount => 2;

        public float this[int i]
        {
            get 
            {
                switch (i)
                {
                    case 0: return X;
                    case 1: return Y;
                }
                throw new ArgumentOutOfRangeException(
                    $"[{nameof(Float2)}] No component at: {i}", nameof(i));
            }
        }

        //Alternative names to the components
        public float R => X;
        public float G => Y;

        //Data
        public readonly float X;
        public readonly float Y;

        public Float2(float x, float y)
        {
            X = x;
            Y = y;
        }

        //Tuple deconstruction syntax
        public void Deconstruct(out float x, out float y)
        {
            x = X;
            y = Y;
        }

        public Float2 Round() => new Float2(X.Round(), Y.Round());

        public Int2 RoundToInt() => new Int2(X.RoundToInt(), Y.RoundToInt());

        //Arithmetic methods
        public static Float2 GetMax(Float2 left, Float2 right)
            => new Float2(System.Math.Max(left.X, right.X), System.Math.Max(left.Y, right.Y));

        public static Float2 GetMin(Float2 left, Float2 right)
            => new Float2(System.Math.Min(left.X, right.X), System.Math.Min(left.Y, right.Y));

        //Arithmetic operators
        public static Float2 operator +(Float2 left, Float2 right)
            => new Float2(left.X + right.X, left.Y + right.Y);

        public static Float2 operator -(Float2 left, Float2 right)
            => new Float2(left.X - right.X, left.Y - right.Y);

        public static Float2 operator *(Float2 left, Float2 right)
            => new Float2(left.X * right.X, left.Y * right.Y);

        public static Float2 operator *(Float2 left, float right)
            => new Float2(left.X * right, left.Y * right);

        public static Float2 operator *(float left, Float2 right)
            => new Float2(left * right.X, left * right.Y);

        public static Float2 operator /(Float2 left, float right)
            => new Float2(left.X / right, left.Y / right);

        public static Float2 operator -(Float2 val) => new Float2(-val.X, -val.Y);

        //Equality
        public static bool operator ==(Float2 a, Float2 b) => a.Equals(b);

        public static bool operator !=(Float2 a, Float2 b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is Float2 && Equals((Float2)obj);

        public bool Equals(Float2 other) => other.X == X && other.Y == Y;

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();

        public bool Approx(Float2 other, float maxDifference = .0001f)
            => X.Approx(other.X, maxDifference) && Y.Approx(other.Y, maxDifference);

        public override string ToString() => $"(X: {X}, Y: {Y})";

        //Conversions
        public static explicit operator Float2(Int2 other)
            => new Float2(other.X, other.Y);
            
        public static implicit operator Float2((float x, float y) tuple)
            => new Float2(tuple.x, tuple.y);
    }
}