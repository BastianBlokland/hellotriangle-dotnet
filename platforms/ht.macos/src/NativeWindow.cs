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
        private delegate void ResizedDelegate(Int2 size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void BeginResizeDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void EndResizeDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void MovedDelegate(Int2 size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void MinimizedDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void DeminimizedDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void MaximizedDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void DemaximizedDelegate();
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void CloseRequestedDelegate();

        [DllImport("libmacwindow", CharSet = CharSet.Ansi)] 
        private static extern IntPtr CreateWindow(	IntPtr appHandle,
                                                    Int2 size, Int2 minSize,
                                                    string title,
                                                    [MarshalAs(UnmanagedType.FunctionPtr)]ResizedDelegate resizedCallback,
                                                    [MarshalAs(UnmanagedType.FunctionPtr)]BeginResizeDelegate beginResizeCallback,
                                                    [MarshalAs(UnmanagedType.FunctionPtr)]EndResizeDelegate endResizeCallback,
                                                    [MarshalAs(UnmanagedType.FunctionPtr)]MovedDelegate movedCallback,
                                                    [MarshalAs(UnmanagedType.FunctionPtr)]MinimizedDelegate minimizedCallback,
                                                    [MarshalAs(UnmanagedType.FunctionPtr)]DeminimizedDelegate deminimizedDelegate,
                                                    [MarshalAs(UnmanagedType.FunctionPtr)]MaximizedDelegate maximizedCallback,
                                                    [MarshalAs(UnmanagedType.FunctionPtr)]DemaximizedDelegate demaximizedCallback,
                                                    [MarshalAs(UnmanagedType.FunctionPtr)]CloseRequestedDelegate closeCallback);

        [DllImport("libmacwindow", CharSet = CharSet.Ansi)] 
        private static extern void SetTitle(IntPtr windowHandle, string title);

        [DllImport("libmacwindow")] 
        private static extern void DisposeWindow(IntPtr windowHandle);
        #endregion

        public event Action CloseRequested;
        public event Action Resized;
        public event Action Moved;

        public string Title
        {
            get => title;
            set
            {
                if(title != value)
                {
                    ThrowIfDisposed();
                    SetTitle(nativeWindowHandle, value);
                    title = value;
                }
            }
        }
        public bool Minimized { get; private set; }
        public bool Maximized { get; private set; }
        public bool IsMovingOrResizing { get; private set; }
        public IntRect ClientRect { get; private set; }
        public Int2 MinClientSize { get; private set; }

        private IntPtr nativeWindowHandle;
        private string title;
        private bool disposed;

        public NativeWindow(IntPtr nativeAppHandle, Int2 size, Int2 minSize, string title)
        {
            MinClientSize = minSize;
            this.title = title;

            nativeWindowHandle = CreateWindow
            (
                nativeAppHandle,
                size,
                minSize,
                title,
                OnResized,
                OnBeginResize,
                OnEndResize,
                OnMoved,
                OnMinimized, 
                OnDeminimized,
                OnMaximized,
                OnDemaximized,
                OnCloseRequested
            );
        }

        public void Dispose()
        {
            if(!disposed)
            {
                DisposeWindow(nativeWindowHandle);
                disposed = true;
            }
        }

        private void OnResized(Int2 size)
        {
            ClientRect = new IntRect(ClientRect.Min, ClientRect.Min + size);
            Resized?.Invoke();
        }

        private void OnBeginResize() => IsMovingOrResizing = true;

        private void OnEndResize() => IsMovingOrResizing = false;

        private void OnMoved(Int2 pos)
        {
            ClientRect = new IntRect(pos, pos + ClientRect.Size);
            Moved?.Invoke();
        }

        private void OnMinimized() => Minimized = true;
        
        private void OnDeminimized() => Minimized = false;

        private void OnMaximized() => Maximized = true;

        private void OnDemaximized() => Maximized = false;

        private void OnCloseRequested() => CloseRequested?.Invoke();

        private void ThrowIfDisposed()
        {
            if(disposed)
                throw new Exception($"[{nameof(NativeWindow)}] Allready disposed!");
        }
    }
}