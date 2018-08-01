using HT.Engine.Math;

namespace HT.Engine.Resources
{
    public interface ITexture
    {
         Int2 Size { get; }
         bool IsCubeMap { get; }
    }
}