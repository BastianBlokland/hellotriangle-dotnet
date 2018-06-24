namespace HT.Engine.Math
{
    public static class ByteExtensions
    {
        public static bool HasBit(this byte val, int bit) => (val & (1 << bit)) != 0;
    }
}