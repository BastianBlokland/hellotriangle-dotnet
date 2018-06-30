namespace HT.Engine.Math
{
    public static class IntExtensions
    {
        public static int ClampPositive(this int val) => val < 0 ? 0 : val;

        public static int ClampMin(this int val, int min) => val < min ? min : val;
        
        public static int Clamp(this int val, int min, int max)
            => val < min ? min : (val > max ? max : val);

        public static bool HasBitSet(this int val, int bit) => (val & (1 << bit)) != 0;

        public static float AsFloat(this int val) => Conversions.IntFloatUnion.IntAsFloat(val);
    }
}