namespace HT.Engine.Math
{
    public static class IntExtensions
    {
        public static int Clamp(this int val, int min, int max)
            => val < min ? min : (val > max ? max : val);

        public static float AsFloat(this int val) => Conversions.IntFloatUnion.IntAsFloat(val);
    }
}