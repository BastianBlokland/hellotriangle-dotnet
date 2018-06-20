using System;
using System.Runtime.InteropServices;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential, Size = SIZE)]
    public struct IntRect : IEquatable<IntRect>
    {
        public const int SIZE = Int2.SIZE * 2;

        public static readonly IntRect Zero = new IntRect();

        public Int2 Size => new Int2(Width, Height);
        public int Width => Max.X - Min.X;
        public int Height => Max.Y - Min.Y;

        public readonly Int2 Min;
        public readonly Int2 Max;

        public IntRect(int minX, int minY, int maxX, int maxY) 
            : this(new Int2(minX, minY), new Int2(maxX, maxY)) {}

        public IntRect(int minX, int minY, Int2 max) 
            : this(new Int2(minX, minY), max) {}

        public IntRect(Int2 min, Int2 max)
        {
            if (min.X > max.X || min.Y > max.Y)
                throw new ArgumentException($"[{nameof(IntRect)}] Is inside out!");
            Min = min;
            Max = max;
        }

        public static bool operator ==(IntRect a, IntRect b) => a.Equals(b);

        public static bool operator !=(IntRect a, IntRect b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is IntRect && Equals((IntRect)obj);

        public bool Equals(IntRect other) => other.Min == Min && other.Max == Max;

        public override int GetHashCode() => Min.GetHashCode() ^ Max.GetHashCode();

        public override string ToString() => $"(Min: {Min}, Max: {Max})";
    }
}