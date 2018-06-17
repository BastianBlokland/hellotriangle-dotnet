using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using HT.Engine.Math;
using HT.Engine.Rendering;
using HT.Engine.Utils;
using HT.Engine.Platform;

namespace HT.MacOS
{
    /// <summary>
    /// Native bindings around a NSApplication, this is root of the application and handles processing all the input events
    /// and updating all the windows that are attached to this app.
    /// NOTE: The lifetime of the app has the be longer then the lifetime of the windows it contains. Only one app can exist at a time
    /// </summary>
    internal sealed class NativeApp : INativeApp
    {
        #region Native bindings
        [DllImport("libmacwindow")] 
        private static extern IntPtr SetupApp();

        [DllImport("libmacwindow")] 
        private static extern void ProcessEvents(IntPtr nativeAppHandle);

        [DllImport("libmacwindow")] 
        private static extern void DisposeApp(IntPtr nativeAppHandle);
        #endregion

        public SurfaceType SurfaceType => SurfaceType.MvkMacOS;

        private readonly Logger logger;
        private readonly IntPtr nativeAppHandle;
        private readonly List<NativeWindow> windows = new List<NativeWindow>();
        private bool disposed;
        
        public NativeApp(Logger logger = null)
        {
            this.logger = logger;
            nativeAppHandle = SetupApp();
        }

        public INativeWindow CreateWindow(Int2 size, Int2 minSize, string title)
        {
            ThrowIfDisposed();
            NativeWindow newWindow = new NativeWindow(nativeAppHandle, size, minSize, title);
            windows.Add(newWindow);
            newWindow.Disposed += () => OnWindowDisposed(newWindow);

            logger?.Log(nameof(NativeApp), $"Native window created (size: {size}, minSize: {minSize}, title: '{title}')");
            return newWindow;
        }

        public FileStream ReadFile(string path)
        {
            string absolutePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            if(!File.Exists(absolutePath))
                throw new IOException($"[{nameof(NativeApp)}] No file found at path: {absolutePath}");
            return File.OpenRead(absolutePath);
        }

        public void Update()
        {
            ThrowIfDisposed();
            ProcessEvents(nativeAppHandle);

            windows.UpdateAll();
        }

        public void Dispose()
        {
            if(!disposed)
            {
                windows.DisposeAll();
                DisposeApp(nativeAppHandle);
                disposed = true;
            }
        }

        private void OnWindowDisposed(NativeWindow window)
        {
            if(!windows.Remove(window))
                throw new ArgumentException($"[{nameof(NativeApp)}] Provided window is not registered to this app", $"{nameof(window)}");
            logger?.Log(nameof(NativeApp), $"Native window destroyed");
        }

        private void ThrowIfDisposed()
        {
            if(disposed)
                throw new Exception($"[{nameof(NativeApp)}] Allready disposed");
        }
    }
}