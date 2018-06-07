using System;

using HT.Engine.Math;

namespace HT.Engine.Platform
{
    /// <summary>
    /// Handle to a native platform window
    /// </summary>
    public interface INativeWindow
    {
        event Action CloseRequested;
        event Action<Int2> Resized;
        event Action<Int2> Moved;
        event Action BeginClientRectChange;
        event Action<IntRect> EndClientRectChange;

        string Title { get; set; }
        bool Minimized { get; }
        bool Maximized { get; }
        bool IsMovingOrResizing { get; }
        IntRect ClientRect { get; }
    }
}