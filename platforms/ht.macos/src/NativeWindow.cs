using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.MacOS
{
    /// <summary>
    /// Wrapper around the NSWindow object, can be used to set properties of the window and get 
    /// callbacks when the user interacts with the window.
    /// </summary>
    internal sealed class NativeWindow : HT.Engine.Platform.INativeWindow
    {
        #region Native bindings
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void BeginResizeCallback();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate Float2 ResizeCallback(Float2 size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void EndResizeCallback();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void CloseRequestedDelegate();

        [DllImport("libmacwindow")] 
        private static extern IntPtr CreateWindow(  IntPtr appPointer, 
                                                    Float2 size, 
                                                    [MarshalAs(UnmanagedType.FunctionPtr)] BeginResizeCallback beginResizeCallback,
                                                    [MarshalAs(UnmanagedType.FunctionPtr)] ResizeCallback resizeCallback,
                                                    [MarshalAs(UnmanagedType.FunctionPtr)] BeginResizeCallback endResizeCallback,
                                                    [MarshalAs(UnmanagedType.FunctionPtr)] CloseRequestedDelegate closeCallback);

        [DllImport("libmacwindow", CharSet = CharSet.Ansi)] 
        private static extern void SetTitle(IntPtr windowPointer, string title);
        
        [DllImport("libmacwindow")] 
        private static extern void DisposeWindow(IntPtr windowPointer);
        #endregion

        public event Action CloseRequested;
        public event Action<Int2> Resized;
        public event Action<Int2> Moved;
        public event Action BeginClientRectChange;
        public event Action<IntRect> EndClientRectChange;

        public string Title
        {
            get => title;
            set
            {
                if(title != value)
                {
                    ThrowIfDisposed();
                    SetTitle(nativeWindowPointer, value);
                    title = value;
                }
            }
        }
        public bool Minimized { get; private set; }
        public bool Maximized { get; private set; }
        public bool IsMovingOrResizing { get; private set; }
        public IntRect ClientRect { get; private set; }

        private readonly IntPtr nativeWindowPointer;
        private string title;
        private bool disposed;

        public NativeWindow(IntPtr nativeAppPointer, Int2 size)
        {
            nativeWindowPointer = CreateWindow
            (
                nativeAppPointer,
                new Float2(size.X, size.Y),
                OnBeginResize,
                OnResize,
                OnEndResize,
                OnCloseRequested
            );
        }

        public void Update()
        {
            
        }
        
        public void Dispose()
        {
            if(!disposed)
            {
                DisposeWindow(nativeWindowPointer);
                disposed = true;
            }
        }

        private void OnBeginResize()
        {
            IsMovingOrResizing = true;
            BeginClientRectChange?.Invoke();
        }

        private Float2 OnResize(Float2 size)
        {
            ClientRect = new IntRect(0, 0, (int)size.X, (int)size.Y);
            Resized?.Invoke(ClientRect.Size);
            return size;
        }

        private void OnEndResize()
        {
            IsMovingOrResizing = false;
            EndClientRectChange?.Invoke(ClientRect);
        }

        private void OnCloseRequested() => CloseRequested?.Invoke();

        private void ThrowIfDisposed()
        {
            if(disposed)
                throw new Exception($"[{nameof(NativeWindow)}] Allready disposed!");
        }
    }
}