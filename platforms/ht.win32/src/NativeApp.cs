using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.Win32
{
    /// <summary>
    /// INSERT DESCRIPTION
    /// </summary>
    internal sealed class NativeApp : HT.Engine.Platform.INativeApp
    {
        #region Native bindings
       
        #endregion

        private bool disposed;
        
        public HT.Engine.Platform.INativeWindow CreateWindow(Float2 size)
        {
            ThrowIfDisposed();
            return new NativeWindow(size);
        }

        public void Update()
        {
            ThrowIfDisposed();
            //Process events here
        }

        public void Dispose()
        {
            if(!disposed)
            {
                //Dispose app here
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