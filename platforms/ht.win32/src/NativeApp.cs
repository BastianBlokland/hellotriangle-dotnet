using System;
using System.Collections.Generic;

using HT.Engine.Math;

namespace HT.Win32
{
    /// <summary>
    /// Handle to a win32 application
    /// </summary>
    internal sealed class NativeApp : HT.Engine.Platform.INativeApp
    {
        private readonly List<NativeWindow> windows = new List<NativeWindow>();
        private bool disposed;
        
        public HT.Engine.Platform.INativeWindow CreateWindow(Int2 size, string title)
        {
            ThrowIfDisposed();
            NativeWindow newWindow = new NativeWindow(size, title);
            windows.Add(newWindow);
            return newWindow;
        }

        public void DestroyWindow(HT.Engine.Platform.INativeWindow window)
        {
            if(window == null)
                throw new ArgumentNullException($"{nameof(window)}");
            NativeWindow nativeWindow = window as NativeWindow;
            if(nativeWindow == null)
                throw new ArgumentException($"[{nameof(NativeApp)}] Provided window is incorrect type", $"{nameof(window)}");
            if(!windows.Remove(nativeWindow))
                throw new ArgumentException($"[{nameof(NativeApp)}] Provided window is not registered to this app", $"{nameof(window)}");
            nativeWindow.Dispose();
        }

        public void Update()
        {
            ThrowIfDisposed();
            for (int i = 0; i < windows.Count; i++)
                windows[i].Update();
        }

        public void Dispose()
        {
            if(!disposed)
            {
                for (int i = 0; i < windows.Count; i++)
                    windows[i].Dispose();
                disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if(disposed)
                throw new Exception($"[{nameof(NativeApp)}] Allready disposed!");
        }
    }
}