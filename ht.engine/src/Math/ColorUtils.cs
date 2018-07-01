namespace HT.Engine.Math
{
    public static class ColorUtils
    {
        public static readonly Byte4 White = new Byte4(255, 255, 255, 255);
        public static readonly Byte4 Silver = new Byte4(192, 192, 192, 255);
        public static readonly Byte4 Gray = new Byte4(128, 128, 128, 255);
        public static readonly Byte4 Black = new Byte4(0, 0, 0, 255);
        public static readonly Byte4 Red = new Byte4(255, 0, 0, 255);
        public static readonly Byte4 Maroon = new Byte4(128, 0, 0, 255);
        public static readonly Byte4 Yellow = new Byte4(255, 255, 0, 255);
        public static readonly Byte4 Olive = new Byte4(128, 128, 0, 255);
        public static readonly Byte4 Lime = new Byte4(0, 255, 0, 255);
        public static readonly Byte4 Green = new Byte4(0, 128, 0, 255);
        public static readonly Byte4 Aqua = new Byte4(0, 255, 255, 255);
        public static readonly Byte4 Teal = new Byte4(0, 128, 128, 255);
        public static readonly Byte4 Blue = new Byte4(0, 0, 255, 255);
        public static readonly Byte4 Navy = new Byte4(0, 0, 128, 255);
        public static readonly Byte4 Fuchsia = new Byte4(255, 0, 255, 255);
        public static readonly Byte4 Purple = new Byte4(128, 0, 128, 255);

        private static readonly Byte4[] colors = new []
        {
            White, Silver, Gray, Black, Red, Maroon, Yellow, Olive, Lime, Green, Aqua, Teal,
            Blue, Navy, Fuchsia, Purple
        };

        public static Byte4 GetColor(int hash)
            => colors[System.Math.Abs(hash % colors.Length)];
    }
}