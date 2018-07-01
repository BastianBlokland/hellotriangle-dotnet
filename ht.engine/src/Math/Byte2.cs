using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    public readonly struct Byte2 : IByteSet, IEquatable<Byte2>
    {
        public const int SIZE = sizeof(byte) * 2;

        //Presets
        public static readonly Byte2 Zero = new Byte2(0, 0);
        public static readonly Byte2 One = new Byte2(1, 1);
        public static readonly Byte2 Max = new Byte2(255, 255);

        //Component index accessor
        public int ComponentCount => 2;

        public byte this[int i]
        {
            get 
            {
                switch (i)
                {
                    case 0: return X;
                    case 1: return Y;
                }
                throw new ArgumentOutOfRangeException(
                    $"[{nameof(Byte2)}] No component at: {i}", nameof(i));
            }
        }

        //Conversion properties
        public Float2 Normalized => new Float2(X / 255f, Y / 255f);

        //Alternative names to the components
        public byte R => X;
        public byte G => Y;

        //Data
        public readonly byte X;
        public readonly byte Y;

        public Byte2(byte x, byte y)
        {
            X = x;
            Y = y;
        }

        //Tuple deconstruction syntax
        public void Deconstruct(out byte x, out byte y)
        {
            x = X;
            y = Y;
        }

        //Arithmetic operators
        public static Byte2 operator +(Byte2 left, Byte2 right)
            => new Byte2(
                (byte)(left.X + right.X), 
                (byte)(left.Y + right.Y));

        public static Byte2 operator -(Byte2 left, Byte2 right)
            => new Byte2(
                (byte)(left.X - right.X),
                (byte)(left.Y - right.Y));

        public static Byte2 operator *(Byte2 left, Byte2 right)
            => new Byte2(
                (byte)(left.X * right.X),
                (byte)(left.Y * right.Y));

        public static Byte2 operator *(Byte2 left, float right)
            => new Byte2(
                (byte)(left.X * right),
                (byte)(left.Y * right));

        public static Byte2 operator *(float left, Byte2 right)
            => new Byte2(
                (byte)(left * right.X),
                (byte)(left * right.Y));

        public static Byte2 operator /(Byte2 left, float right)
            => new Byte2(
                (byte)(left.X / right), 
                (byte)(left.Y / right));

        //Equality
        public static bool operator ==(Byte2 a, Byte2 b) => a.Equals(b);

        public static bool operator !=(Byte2 a, Byte2 b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is Byte2 && Equals((Byte2)obj);

        public bool Equals(Byte2 other) => other.X == X && other.Y == Y;

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();

        public override string ToString() => $"(X: {X}, Y: {Y})";

        //Conversions
        public static implicit operator Byte2((byte x, byte y) tuple)
            => new Byte2(tuple.x, tuple.y);
    }
}