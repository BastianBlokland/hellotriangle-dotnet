using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.MacOS
{
    /// <summary>
    /// Native bindings around a NSApplication, this is root of the application and handles processing all the input events
    /// and updating all the windows that are attached to this app.
    /// NOTE: The lifetime of the app has the be longer then the lifetime of the windows it contains. Only one app can exist at a time
    /// </summary>
    internal sealed class NativeApp : HT.Engine.Platform.INativeApp
    {
        #region Native bindings
        [DllImport("libmacwindow")] 
        private static extern IntPtr SetupApp();

        [DllImport("libmacwindow")] 
        private static extern void ProcessEvents(IntPtr appPointer);

        [DllImport("libmacwindow")] 
        private static extern void DisposeApp(IntPtr appPointer);
        #endregion

        private readonly IntPtr nativeAppPointer;
        private bool disposed;
        
        public NativeApp() => nativeAppPointer = SetupApp();

        public HT.Engine.Platform.INativeWindow CreateWindow(Int2 size, string title)
        {
            ThrowIfDisposed();
            return new NativeWindow(nativeAppPointer, size);
        }

        public void DestroyWindow(HT.Engine.Platform.INativeWindow window)
        {
            //TODO: Implement
        }

        public void Update()
        {
            ThrowIfDisposed();
            ProcessEvents(nativeAppPointer);
        }

        public void Dispose()
        {
            if(!disposed)
            {
                DisposeApp(nativeAppPointer);
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