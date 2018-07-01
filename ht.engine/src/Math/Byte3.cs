using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

using static System.Math;
using static System.MathF;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    public readonly struct Byte3 : IByteSet, IEquatable<Byte3>
    {
        public const int SIZE = sizeof(byte) * 3;

        //Presets
        public static readonly Byte3 Zero = new Byte3(0, 0, 0);
        public static readonly Byte3 One = new Byte3(1, 1, 1);
        public static readonly Byte3 Max = new Byte3(255, 255, 255);

        //Component index accessor
        public int ComponentCount => 3;

        public byte this[int i]
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
                    $"[{nameof(Byte3)}] No component at: {i}", nameof(i));
            }
        }

        //Conversion properties
        public Float3 Normalized => new Float3(X / 255f, Y / 255f, Z / 255f);

        //Alternative names to the components
        public byte R => X;
        public byte G => Y;
        public byte B => Z;

        //Data
        public readonly byte X;
        public readonly byte Y;
        public readonly byte Z;

        public Byte3(byte x, byte y, byte z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        //Tuple deconstruction syntax
        public void Deconstruct(out byte x, out byte y, out byte z)
        {
            x = X;
            y = Y;
            z = Z;
        }

        //Arithmetic operators
        public static Byte3 operator +(Byte3 left, Byte3 right)
            => new Byte3(
                (byte)(left.X + right.X),
                (byte)(left.Y + right.Y),
                (byte)(left.Z + right.Z));

        public static Byte3 operator -(Byte3 left, Byte3 right)
            => new Byte3(
                (byte)(left.X - right.X),
                (byte)(left.Y - right.Y),
                (byte)(left.Z - right.Z));

        public static Byte3 operator *(Byte3 left, Byte3 right)
            => new Byte3(
                (byte)(left.X * right.X),
                (byte)(left.Y * right.Y),
                (byte)(left.Z * right.Z));

        public static Byte3 operator *(Byte3 left, float right)
            => new Byte3(
                (byte)(left.X * right),
                (byte)(left.Y * right),
                (byte)(left.Z * right));

        public static Byte3 operator *(float left, Byte3 right)
            => new Byte3(
                (byte)(left * right.X),
                (byte)(left * right.Y),
                (byte)(left * right.Z));

        public static Byte3 operator /(Byte3 left, float right)
            => new Byte3(
                (byte)(left.X / right),
                (byte)(left.Y / right),
                (byte)(left.Z / right));

        //Equality
        public static bool operator ==(Byte3 a, Byte3 b) => a.Equals(b);

        public static bool operator !=(Byte3 a, Byte3 b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is Byte3 && Equals((Byte3)obj);

        public bool Equals(Byte3 other) => other.X == X && other.Y == Y && other.Z == Z;

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();

        public override string ToString() => $"(X: {X}, Y: {Y}, Z: {Z})";

        //Conversions
        public static implicit operator Byte3((byte x, byte y, byte z) tuple)
            => new Byte3(tuple.x, tuple.y, tuple.z);
    }
}