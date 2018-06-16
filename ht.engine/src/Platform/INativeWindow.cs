using System;

using HT.Engine.Math;

namespace HT.Engine.Platform
{
    /// <summary>
    /// Handle to a native platform window
    /// </summary>
    public interface INativeWindow : IDisposable
    {
        event Action Disposed;
        event Action CloseRequested;
        event Action Resized;
        event Action Moved;

        string Title { get; set; }
        bool Minimized { get; }
        bool Maximized { get; }
        bool IsMovingOrResizing { get; }
        IntRect ClientRect { get; }

        Rendering.Surface CreateSurface(Rendering.Host host);
    }
}