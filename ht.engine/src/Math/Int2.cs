using System.Runtime.InteropServices;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Int2
    {
        public static Int2 Zero => new Int2(x: 0, y: 0);
        public static Int2 One => new Int2(x: 1, y: 1);

        public readonly int X;
        public readonly int Y;

        public Int2(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"(X: {X}, Y: {Y})";
    }
}