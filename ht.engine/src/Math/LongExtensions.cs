namespace HT.Engine.Math
{
    public static class LongExtensions
    {
        /// <summary>
        /// Splits a 64 bit long into two 16 bit shorts (only first 32 bits used)
        /// </summary>
        public static void Split(this long val, out short a, out short b)
        {
            a = (short)(val & 0xffffffff);
            b = (short)(val >> 16);
        }
    }
}