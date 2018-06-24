using System;
using System.IO;
using System.Collections.Generic;

using HT.Engine.Math;
using HT.Engine.Rendering;
using HT.Engine.Utils;
using HT.Engine.Platform;

namespace HT.Win32
{
    /// <summary>
    /// Handle to a win32 application
    /// </summary>
    internal sealed class NativeApp : INativeApp
    {
        public SurfaceType SurfaceType => SurfaceType.HkrWin32;

        private readonly Logger logger;
        private readonly List<NativeWindow> windows = new List<NativeWindow>();
        private bool disposed;
        
        public NativeApp(Logger logger = null) => this.logger = logger;

        public INativeWindow CreateWindow(Int2 size, Int2 minSize, string title)
        {
            ThrowIfDisposed();
            NativeWindow newWindow = new NativeWindow(size, minSize, title);
            windows.Add(newWindow);
            newWindow.Disposed += () => OnWindowDisposed(newWindow);
            
            logger?.Log(nameof(NativeApp),
                $"Native window created (size: {size}, minSize: {minSize}, title: '{title}')");
            return newWindow;
        }

        public FileStream ReadFile(string path)
        {
            string absolutePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            if (!File.Exists(absolutePath))
                throw new IOException(
                    $"[{nameof(NativeApp)}] No file found at path: {absolutePath}");
            return File.OpenRead(absolutePath);
        }

        public void Update()
        {
            ThrowIfDisposed();
            windows.UpdateAll();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                windows.DisposeAll();
                disposed = true;
            }
        }

        private void OnWindowDisposed(NativeWindow window)
        {
            if (!windows.Remove(window))
                throw new ArgumentException(
                    $"[{nameof(NativeApp)}] Provided window is not registered to this app", $"{nameof(window)}");
            logger?.Log(nameof(NativeApp), $"Native window destroyed");
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(NativeApp)}] Allready disposed");
        }
    }
}