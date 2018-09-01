using System;

using HT.Engine.Math;
using HT.Engine.Utils;

namespace HT.Engine.Resources
{
    public static class TextureUtils
    {
        public static ByteTexture CreateSolidTexture(Byte4 color)
            => new ByteTexture(new [] { color }, (x: 1, y: 1));

        public static FloatTexture CreateSolidTexture(Float4 color)
            => new FloatTexture(new [] { color }, (x: 1, y: 1));

        public static FloatTexture CreateRandomFloatTexture(
            IRandom random, Float4 min, Float4 max, Int2 size)
        {
            Float4[] pixels = new Float4[size.X * size.Y];
            for (int y = 0; y < size.Y; y++)
            for (int x = 0; x < size.X; x++)
                pixels[y * size.X + x] = random.GetBetween(min, max);
            return new FloatTexture(pixels, size);
        }
    }
}