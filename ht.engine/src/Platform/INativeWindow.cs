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
        bool IsMinimized { get; }
        bool IsMaximized { get; }
        bool IsMovingOrResizing { get; }
        IntRect ClientRect { get; }
        
        IntPtr OSInstanceHandle { get; }
        IntPtr OSViewHandle { get; }
    }
}