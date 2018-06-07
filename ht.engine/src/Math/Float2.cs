using System.Runtime.InteropServices;

namespace HT.Engine.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Float2
    {
        public static Float2 Zero => new Float2(x: 0f, y: 0f);
        public static Float2 One => new Float2(x: 1f, y: 1f);

        public readonly float X;
        public readonly float Y;

        public Float2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"(X: {X}, Y: {Y})";
    }
}