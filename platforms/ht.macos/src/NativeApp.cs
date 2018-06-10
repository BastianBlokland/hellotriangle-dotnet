using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using HT.Engine.Math;
using HT.Engine.Rendering;
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

        public IEnumerable<INativeWindow> Windows => windows;

        private readonly IntPtr nativeAppHandle;
        private readonly List<NativeWindow> windows = new List<NativeWindow>();
        private bool disposed;
        
        public NativeApp() => nativeAppHandle = SetupApp();

        public INativeWindow CreateWindow(Int2 size, Int2 minSize, string title)
        {
            ThrowIfDisposed();
            NativeWindow newWindow = new NativeWindow(nativeAppHandle, size, minSize, title);
            windows.Add(newWindow);
            return newWindow;
        }

        public void DestroyWindow(INativeWindow window)
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
            ProcessEvents(nativeAppHandle);
        }

        public void Dispose()
        {
            if(!disposed)
            {
                DisposeApp(nativeAppHandle);
                disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if(disposed)
                throw new Exception($"[{nameof(NativeApp)}] Allready disposed");
        }
    }
}