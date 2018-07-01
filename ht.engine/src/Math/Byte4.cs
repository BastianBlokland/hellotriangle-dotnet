using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    public readonly struct Byte4 : IByteSet, IEquatable<Byte4>
    {
        public const int SIZE = sizeof(byte) * 4;

        //Presets
        public static readonly Byte4 Zero = new Byte4(0, 0, 0, 0);
        public static readonly Byte4 One = new Byte4(1, 1, 1, 1);
        public static readonly Byte4 Max = new Byte4(255, 255, 255, 255);

        //Component index accessor
        public int ComponentCount => 4;

        public byte this[int i]
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
                    $"[{nameof(Byte4)}] No component at: {i}", nameof(i));
            }
        }

        //Conversion properties
        public Float4 Normalized => new Float4(X / 255f, Y / 255f, Z / 255f, W / 255f);
        
        //Alternative names to the components
        public byte R => X;
        public byte G => Y;
        public byte B => Z;
        public byte A => W;

        //Data
        public readonly byte X;
        public readonly byte Y;
        public readonly byte Z;
        public readonly byte W;

        public Byte4(byte x, byte y, byte z, byte w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        //Tuple deconstruction syntax
        public void Deconstruct(out byte x, out byte y, out byte z, out byte w)
        {
            x = X;
            y = Y;
            z = Z;
            w = W;
        }

        //Arithmetic operators
        public static Byte4 operator +(Byte4 left, Byte4 right)
            => new Byte4(
                (byte)(left.X + right.X),
                (byte)(left.Y + right.Y),
                (byte)(left.Z + right.Z),
                (byte)(left.W + right.W));

        public static Byte4 operator -(Byte4 left, Byte4 right)
            => new Byte4(
                (byte)(left.X - right.X), 
                (byte)(left.Y - right.Y),
                (byte)(left.Z - right.Z), 
                (byte)(left.W - right.W));

        public static Byte4 operator *(Byte4 left, Byte4 right)
            => new Byte4(
                (byte)(left.X * right.X),
                (byte)(left.Y * right.Y),
                (byte)(left.Z * right.Z),
                (byte)(left.W * right.W));

        public static Byte4 operator *(Byte4 left, float right)
            => new Byte4(
                (byte)(left.X * right),
                (byte)(left.Y * right),
                (byte)(left.Z * right),
                (byte)(left.W * right));

        public static Byte4 operator *(float left, Byte4 right)
            => new Byte4(
                (byte)(left * right.X),
                (byte)(left * right.Y),
                (byte)(left * right.Z),
                (byte)(left * right.W));

        public static Byte4 operator /(Byte4 left, float right)
            => new Byte4(
                (byte)(left.X / right),
                (byte)(left.Y / right),
                (byte)(left.Z / right),
                (byte)(left.W / right));

        //Equality
        public static bool operator ==(Byte4 a, Byte4 b) => a.Equals(b);

        public static bool operator !=(Byte4 a, Byte4 b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is Byte4 && Equals((Byte4)obj);

        public bool Equals(Byte4 other) => 
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

        //Conversions
        public static implicit operator Byte4((byte x, byte y, byte z, byte w) tuple)
            => new Byte4(tuple.x, tuple.y, tuple.z, tuple.w);
    }
}