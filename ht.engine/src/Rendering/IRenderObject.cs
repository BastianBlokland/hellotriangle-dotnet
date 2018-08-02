using System;

namespace HT.Engine.Rendering
{
    public interface IRenderObject : IDisposable
    {
        int RenderOrder { get; }
    }
}