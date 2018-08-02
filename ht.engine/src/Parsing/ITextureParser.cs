using System;
using HT.Engine.Resources;

namespace HT.Engine.Parsing
{
    public interface ITextureParser : IDisposable
    {
         ITexture Parse();
    }
}