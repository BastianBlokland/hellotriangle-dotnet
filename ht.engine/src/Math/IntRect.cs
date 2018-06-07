using System;
using System.Runtime.InteropServices;

using static System.Math;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public struct IntRect
    {
        public static IntRect Zero => new IntRect();

        public Int2 Size => new Int2(Width, Height);
        public int Width => Max.X - Min.X;
        public int Height => Max.Y - Min.Y;

        public readonly Int2 Min;
        public readonly Int2 Max;

        public IntRect(int minX, int minY, int maxX, int maxY) : this(new Int2(minX, minY), new Int2(maxX, maxY)) {}

        public IntRect(Int2 min, Int2 max)
        {
            if(min.X > max.X || min.Y > max.Y)
                throw new ArgumentException($"[{nameof(IntRect)}] Is inside out!");
            Min = min;
            Max = max;
        }

        public override string ToString() => $"(Min: {Min}, Max: {Max})";
    }
}