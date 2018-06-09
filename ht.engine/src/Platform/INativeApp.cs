using System;
using System.Collections.Generic;
using HT.Engine.Math;

namespace HT.Engine.Platform
{
    /// <summary>
    /// Handle to a native platform application
    /// </summary>
    public interface INativeApp : IDisposable
    {
        IEnumerable<INativeWindow> Windows { get; }

        INativeWindow CreateWindow(Int2 size, Int2 minSize, string title);
        void DestroyWindow(INativeWindow window);

        void Update();
    }
}