using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    public readonly struct Int2 : IEquatable<Int2>
    {
        public const int SIZE = sizeof(int) * 2;

        //Presets
        public static readonly Int2 Zero = new Int2(0, 0);
        public static readonly Int2 One = new Int2(1, 1);
        public static readonly Int2 Up = new Int2(0, 1);
        public static readonly Int2 Down = new Int2(0, -1);
        public static readonly Int2 Right = new Int2(1, 0);
        public static readonly Int2 Left = new Int2(-1, 0);

        //Component index accessor
        public int this[int i]
        {
            get 
            {
                switch (i)
                {
                    case 0: return X;
                    case 1: return Y;
                }
                throw new ArgumentOutOfRangeException(
                    $"[{nameof(Int2)}] No component at: {i}", nameof(i));
            }
        }

        //Data
        public readonly int X;
        public readonly int Y;

        public Int2(int x, int y)
        {
            X = x;
            Y = y;
        }

        //Tuple deconstruction syntax
        public void Deconstruct(out int x, out int y)
        {
            x = X;
            y = Y;
        }

        //Arithmetic methods
        public Int2 Clamp(Int2 min, Int2 max)
            => new Int2(X.Clamp(min.X, max.X), Y.Clamp(min.Y, max.Y));

        public static Int2 Max(Int2 left, Int2 right)
            => new Int2(System.Math.Max(left.X, right.X), System.Math.Max(left.Y, right.Y));

        public static Int2 Min(Int2 left, Int2 right)
            => new Int2(System.Math.Min(left.X, right.X), System.Math.Min(left.Y, right.Y));

        //Arithmetic operators
        public static Int2 operator +(Int2 left, Int2 right)
            => new Int2(left.X + right.X, left.Y + right.Y);

        public static Int2 operator -(Int2 left, Int2 right)
            => new Int2(left.X - right.X, left.Y - right.Y);

        public static Int2 operator *(Int2 left, Int2 right)
            => new Int2(left.X * right.X, left.Y * right.Y);

        public static Int2 operator *(Int2 left, int right)
            => new Int2(left.X * right, left.Y * right);

        public static Int2 operator *(int left, Int2 right)
            => new Int2(left * right.X, left * right.Y);

        public static Int2 operator /(Int2 left, int right)
            => new Int2(left.X / right, left.Y / right);

        public static Int2 operator -(Int2 val) => new Int2(-val.X, -val.Y);

        //Equality
        public static bool operator ==(Int2 a, Int2 b) => a.Equals(b);

        public static bool operator !=(Int2 a, Int2 b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is Int2 && Equals((Int2)obj);

        public bool Equals(Int2 other) => other.X == X && other.Y == Y;

        public override int GetHashCode() => X ^ Y;

        public override string ToString() => $"(X: {X}, Y: {Y})";

        //Conversions
        public static explicit operator Int2(Float2 other) => new Int2((int)other.X, (int)other.Y);

        public static implicit operator Int2((int x, int y) tuple)
            => new Int2(tuple.x, tuple.y);
    }
}