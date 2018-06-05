using System;

using HT.Engine.Math;

namespace HT.Engine.Platform
{
    /// <summary>
    /// Handle to a native platform window
    /// </summary>
    public interface INativeWindow : IDisposable
    {
        event Action CloseRequested;
        event Action BeginResizing;
        event Action<Float2> Resized;
        event Action EndResizing;

        string Title { get; set; }
        bool IsResizing { get; }
        Float2 Size { get; }
        Float2 MinSize { get; set; }
        Float2 MaxSize { get; set; }
    }
}