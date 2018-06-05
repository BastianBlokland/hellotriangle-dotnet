using System;

using HT.Engine.Math;

namespace HT.Engine.Platform
{
    /// <summary>
    /// Handle to a native platform application
    /// </summary>
    public interface INativeApp : IDisposable
    {
        INativeWindow CreateWindow(Float2 size);

        /// <summary>
        /// Send a update pulse to a native-app so it can handle os events 
        /// </summary>
        void Update();
    }
}