namespace HT.Engine.Math
{
    public static class LongExtensions
    {
        /// <summary>
        /// Splits a 64 bit long into two 16 bit shorts (only first 32 bits used)
        /// </summary>
        public static void Split(this long val, out short a, out short b)
        {
            a = (short)(val & 0xffff);
            b = (short)((val >> 16) & 0xffff);
        }

        /// <summary>
        /// Splits a 64 bit long into four 16 bit shorts
        /// </summary>
        public static void Split(this long val, out short a, out short b, out short c, out short d)
        {
            a = (short)(val & 0xffff);
            b = (short)((val >> 16) & 0xffff);
            c = (short)((val >> 32) & 0xffff);
            d = (short)((val >> 48) & 0xffff);
        }
    }
}