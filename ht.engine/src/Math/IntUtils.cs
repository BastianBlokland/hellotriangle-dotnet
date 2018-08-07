using HT.Engine.Math;

namespace HT.Engine.Math
{
    public static class IntUtils
    {
        public static int Min(this int a, int b) => a < b ? a : b;
        public static int Max(this int a, int b) => a > b ? a : b;
    }
}