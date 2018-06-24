namespace HT.Engine.Math
{
    public static class FloatExtensions
    {
        public static int AsInt(this float val) => Conversions.IntFloatUnion.FloatAsInt(val);

        public static bool Approx(this float val, float other, float maxDifference = .0001f)
            => FloatUtils.Approx(val, other, maxDifference);
    }
}