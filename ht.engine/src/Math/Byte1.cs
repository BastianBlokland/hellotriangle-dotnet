using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    public readonly struct Byte1 : IByteSet, IEquatable<Byte1>
    {
        public const int SIZE = sizeof(byte);

        //Presets
        public static readonly Byte1 Zero = 0;
        public static readonly Byte1 Max = new Byte1(byte.MaxValue);
        public static readonly Byte1 One = 1;

        //Component index accessor
        public int ComponentCount => 1;
        
        public byte this[int i]
        {
            get 
            {
                switch (i)
                {
                    case 0: return X;
                }
                throw new ArgumentOutOfRangeException(
                    $"[{nameof(Float1)}] No component at: {i}", nameof(i));
            }
        }

        //Conversion properties
        public float Normalized => X / 255f;

        //Alternative names to the components
        public byte R => X;

        //Data
        public readonly byte X;

        public Byte1(byte x)
        {
            X = x;
        }

        //Equality
        public static bool operator ==(Byte1 a, Byte1 b) => a.Equals(b);

        public static bool operator !=(Byte1 a, Byte1 b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is Byte1 && Equals((Byte1)obj);

        public bool Equals(Byte1 other) => other.X == X;

        public override int GetHashCode() => X.GetHashCode();

        public override string ToString() => $"(X: {X})";

        //Conversions
        public static implicit operator Byte1(byte val) => new Byte1(val);
        public static implicit operator byte(Byte1 val) => val.X;
    }
}