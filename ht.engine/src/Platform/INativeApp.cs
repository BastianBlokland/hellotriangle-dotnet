using System;

using HT.Engine.Math;

namespace HT.Engine.Platform
{
    /// <summary>
    /// Handle to a native platform application
    /// </summary>
    public interface INativeApp : IDisposable
    {
        INativeWindow CreateWindow(Int2 size, string title);
        void DestroyWindow(INativeWindow window);

        void Update();
    }
}