namespace HT.Engine.Math.Extensions
{
    public static class FloatExtensions
    {
        public static int AsInt(this float val) => Conversions.IntFloatUnion.FloatAsInt(val);
    }
}