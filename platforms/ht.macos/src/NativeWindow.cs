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
        private static extern IntPtr CreateWindow(	IntPtr appPointer, 
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
        public event Action BeginResizing;
        public event Action<Float2> Resized;
        public event Action EndResizing;

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
        public bool IsResizing { get; private set; }
        public Float2 Size { get; private set; }
        public Float2 MinSize { get; set; } = new Float2(0f, 0f);
        public Float2 MaxSize { get; set; } = new Float2(float.MaxValue, float.MaxValue);

        private readonly IntPtr nativeWindowPointer;
        private string title;
        private bool disposed;

        public NativeWindow(IntPtr nativeAppPointer, Float2 size)
        {
            Size = size;
            nativeWindowPointer = CreateWindow
            (
                nativeAppPointer,
                size,
                OnBeginResize,
                OnResize,
                OnEndResize,
                OnCloseRequested
            );
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
            IsResizing = true;
            BeginResizing?.Invoke();
        }

        private Float2 OnResize(Float2 size)
        {
            Size = new Float2
            (
                size.X.Clamp(MinSize.X, MaxSize.X),
                size.Y.Clamp(MinSize.Y, MaxSize.Y)
            );
            Resized?.Invoke(Size);
            return size;
        }

        private void OnEndResize()
        {
            IsResizing = false;
            EndResizing?.Invoke();
        }

        private void OnCloseRequested() => CloseRequested?.Invoke();

        private void ThrowIfDisposed()
        {
            if(disposed)
                throw new Exception($"[{nameof(NativeWindow)}] Allready disposed!");
        }
    }
}