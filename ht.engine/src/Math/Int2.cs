using System;
using System.Runtime.InteropServices;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Int2 : IEquatable<Int2>
    {
        public static readonly Int2 Zero = new Int2(x: 0, y: 0);
        public static readonly Int2 One = new Int2(x: 1, y: 1);
        public static readonly Int2 Up = new Int2(x: 0, y: 1);
        public static readonly Int2 Down = new Int2(x: 0, y: -1);
        public static readonly Int2 Right = new Int2(x: 1, y: 0);
        public static readonly Int2 Left = new Int2(x: -1, y: 0);

        public readonly int X;
        public readonly int Y;

        public Int2(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Int2 Clamp(Int2 min, Int2 max)
            => new Int2(X.Clamp(min.X, max.X), Y.Clamp(min.Y, max.Y));

        public static Int2 Max(Int2 left, Int2 right)
            => new Int2(System.Math.Max(left.X, right.X), System.Math.Max(left.Y, right.Y));

        public static Int2 Min(Int2 left, Int2 right)
            => new Int2(System.Math.Min(left.X, right.X), System.Math.Min(left.Y, right.Y));
        
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

        public static bool operator ==(Int2 a, Int2 b) => a.Equals(b);

        public static bool operator !=(Int2 a, Int2 b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is Int2 && Equals((Int2)obj);

        public bool Equals(Int2 other) => other.X == X && other.Y == Y;

        public override int GetHashCode() => X ^ Y;

        public override string ToString() => $"(X: {X}, Y: {Y})";

        public static explicit operator Int2(Float2 other) => new Int2((int)other.X, (int)other.Y);
    }
}