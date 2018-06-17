using System;
using System.Collections.Generic;
using System.IO;

using HT.Engine.Math;
using HT.Engine.Utils;

namespace HT.Engine.Platform
{
    /// <summary>
    /// Handle to a native platform application
    /// </summary>
    public interface INativeApp : IDisposable, IUpdatable
    {
        Rendering.SurfaceType SurfaceType { get; }

        INativeWindow CreateWindow(Int2 size, Int2 minSize, string title);
        FileStream ReadFile(string fileName);
    }
}