using System;

using HT.Engine.Resources;

namespace HT.Engine.Rendering
{
    public readonly struct TextureInfo
    {
        public readonly ITexture Texture;
        public readonly bool UseMipMaps;
        public readonly bool Repeat;

        public TextureInfo(ITexture texture, bool useMipMaps, bool repeat)
        {
            Texture = texture;
            UseMipMaps = useMipMaps;
            Repeat = repeat;
        }
    }
}