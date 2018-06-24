namespace HT.Engine.Math
{
    public static class ByteExtensions
    {
        public static bool HasBitSet(this byte val, int bit) => (val & (1 << bit)) != 0;
    }
}