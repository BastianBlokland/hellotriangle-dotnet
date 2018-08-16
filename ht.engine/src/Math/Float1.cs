using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    public readonly struct Float1 : IFloatSet, IEquatable<Float1>
    {
        public const int SIZE = sizeof(float);

        //Presets
        public static readonly Float1 Zero = 0f;
        public static readonly Float1 Max = new Float1(float.MaxValue);
        public static readonly Float1 Min = new Float1(float.MinValue);
        public static readonly Float1 One = 1f;

        //Component index accessor
        public int ComponentCount => 1;
        
        public float this[int i]
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

        //Alternative names to the components
        public float R => X;

        //Data
        public readonly float X;

        public Float1(float x)
        {
            X = x;
        }

        //Equality
        public static bool operator ==(Float1 a, Float1 b) => a.Equals(b);

        public static bool operator !=(Float1 a, Float1 b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is Float1 && Equals((Float1)obj);

        public bool Equals(Float1 other) => other.X == X;

        public override int GetHashCode() => X.GetHashCode();

        public bool Approx(Float1 other, float maxDifference = .0001f)
            => X.Approx(other.X, maxDifference);

        public override string ToString() => $"(X: {X})";

        //Conversions
        public static implicit operator Float1(float val) => new Float1(val);
        public static implicit operator float(Float1 val) => val.X;
    }
}