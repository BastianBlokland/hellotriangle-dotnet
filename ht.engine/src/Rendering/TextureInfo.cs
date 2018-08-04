using System;

using HT.Engine.Resources;

namespace HT.Engine.Rendering
{
    public readonly struct TextureInfo
    {
        public readonly ITexture Texture;
        public readonly bool UseMipMaps;

        public TextureInfo(ITexture texture, bool useMipMaps)
        {
            Texture = texture;
            UseMipMaps = useMipMaps;
        }
    }
}