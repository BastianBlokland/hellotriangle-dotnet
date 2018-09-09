using System;

namespace HT.Engine.Math
{
    public static class FloatExtensions
    {
        public static int AsInt(this float val) => Convert.FloatAsInt(ref val);

        public static Half ToHalf(this float val) => Half.FromFloat(val);

        public static float Round(this float val) => MathF.Round(val);

        public static int RoundToInt(this float val) => (int)MathF.Round(val);

        public static bool Approx(this float val, float other, float maxDifference = .0001f)
            => FloatUtils.Approx(val, other, maxDifference);

        public static Float2 XX(this float val) => new Float2(val, val);

        public static Float3 XXX(this float val) => new Float3(val, val, val);
    }
}