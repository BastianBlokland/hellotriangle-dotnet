using System;

using HT.Engine.Math;

namespace HT.Engine.Rendering
{
    public sealed class Texture
    {
        //Helper properties
        public int Width => width;
        public int Height => height;

        //Data
        private readonly Float4[] pixels; //stored row by row
        private readonly int width;
        private readonly int height;

        public Texture(Float4[] pixels, int width, int height)
        {
            if (pixels == null)
                throw new ArgumentNullException(nameof(pixels));
            if (pixels.Length != width * height)
                throw new ArgumentException(
                    $"[{nameof(Texture)}] Invalid count, expected: {width * height}, got: {pixels.Length}", nameof(pixels));
            this.pixels = pixels;
            this.width = width;
            this.height = height;
        }
    }
}