using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using HT.Engine.Math;
using HT.Engine.Rendering;
using HT.Engine.Utils;
using HT.Engine.Platform;

namespace HT.MacOS
{
    /// <summary>
    /// Wrapper around the NSWindow object, can be used to set properties of the window and get 
    /// callbacks when the user interacts with the window.
    /// </summary>
    internal sealed class NativeWindow : INativeWindow, IUpdatable
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
        private static extern IntPtr CreateWindow(
            IntPtr appHandle,
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

        [DllImport("libmacwindow")] 
        private static extern IntPtr CreateMetalView(IntPtr windowPointer);

        [DllImport("libmacwindow", CharSet = CharSet.Ansi)] 
        private static extern void SetTitle(IntPtr windowHandle, string title);

        [DllImport("libmacwindow")] 
        private static extern void DisposeWindow(IntPtr windowHandle);
        #endregion

        public event Action Disposed;
        public event Action CloseRequested;
        public event Action Resized;
        public event Action Moved;

        public string Title
        {
            get => title;
            set
            {
                if (title != value)
                {
                    ThrowIfDisposed();
                    SetTitle(nativeWindowHandle, value);
                    title = value;
                }
            }
        }
        public bool IsMinimized { get; private set; }
        public bool IsMaximized { get; private set; }
        public bool IsMovingOrResizing { get; private set; }
        public IntRect ClientRect { get; private set; }

        public IntPtr OSInstanceHandle => instanceHandle;
        public IntPtr OSViewHandle => nativeMetalViewHandle;

        //Handle to the delgates we've handed to the native side, its important we hold a handle
        //to them otherwise they will get garbage collected on the native side
        private readonly ResizedDelegate onResized;
        private readonly BeginResizeDelegate onBeginResize;
        private readonly EndResizeDelegate onEndResize;
        private readonly MovedDelegate onMoved;
        private readonly MinimizedDelegate onMinimized;
        private readonly DeminimizedDelegate onDeminimized;
        private readonly MaximizedDelegate onMaximized;
        private readonly DemaximizedDelegate onDemaximized;
        private readonly CloseRequestedDelegate onCloseRequested;

        private IntPtr instanceHandle;
        private IntPtr nativeWindowHandle;
        private IntPtr nativeMetalViewHandle;
        private string title;
        private bool disposed;

        private bool initialSizeSet;
        private bool initialPosSet;

        private bool invokeCloseRequestedEvent;
        private bool invokeResizedEvent;
        private bool invokeMovedEvent;

        public NativeWindow(IntPtr nativeAppHandle, Int2 size, Int2 minSize, string title)
        {
            this.title = title;

            //Create delegates pointing to our callbacks
            //NOTE: Its important to keep a reference to this delegates in the class because
            //otherwise they will be garbage collected but the native side still has pointer to them
            onResized = new ResizedDelegate(OnResized);
            onBeginResize = new BeginResizeDelegate(OnBeginResize);
            onEndResize = new EndResizeDelegate(OnEndResize);
            onMoved = new MovedDelegate(OnMoved);
            onMinimized = new MinimizedDelegate(OnMinimized);
            onDeminimized = new DeminimizedDelegate(OnDeminimized);
            onMaximized = new MaximizedDelegate(OnMaximized);
            onDemaximized = new DemaximizedDelegate(OnDemaximized);
            onCloseRequested = new CloseRequestedDelegate(OnCloseRequested);

            //Gets a handle to our process
            instanceHandle = System.Diagnostics.Process.GetCurrentProcess().Handle;
            //Create the os window
            nativeWindowHandle = CreateWindow
            (
                nativeAppHandle,
                size,
                minSize,
                title,
                onResized,
                onBeginResize,
                onEndResize,
                onMoved,
                onMinimized, 
                onDeminimized,
                onMaximized,
                onDemaximized,
                onCloseRequested
            );
            //create a metal-view so we can actually render into our window
            nativeMetalViewHandle = CreateMetalView(nativeWindowHandle);
        }

        public void Update()
        {
            ThrowIfDisposed();

            //Invoke events that happened in the native-callbacks. Why dont we just call these
            //directly? basically if we call then directly then the call origin would be in the os
            //event loop and if you then try to change the window from within that event-loop it
            //doesn't like that
            if (invokeCloseRequestedEvent)
            {
                CloseRequested?.Invoke();
                invokeCloseRequestedEvent = false;
            }
            if (invokeResizedEvent)
            {
                Resized?.Invoke();
                invokeResizedEvent = false;
            }
            if (invokeMovedEvent)
            {
                Moved?.Invoke();
                invokeMovedEvent = false;
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                DisposeWindow(nativeWindowHandle);
                disposed = true;
                Disposed?.Invoke();
            }
        }

        private void OnResized(Int2 size)
        {
            ClientRect = new IntRect(ClientRect.Min, ClientRect.Min + size);
            
            //Invoke the 'Resized' event only if this was not the initial size set,
            //this way we don't get resized events when the window just opens
            invokeResizedEvent = initialSizeSet;
            initialSizeSet = true;
        }

        private void OnBeginResize() => IsMovingOrResizing = true;

        private void OnEndResize() => IsMovingOrResizing = false;

        private void OnMoved(Int2 pos)
        {
            ClientRect = new IntRect(pos, pos + ClientRect.Size);
            
            //Invoke the 'Moved' event only if this was not the initial pos set,
            //this way we don't get moved events when the window just opens 
            invokeMovedEvent = initialPosSet;
            initialPosSet = true;
        }

        private void OnMinimized() => IsMinimized = true;
        
        private void OnDeminimized() => IsMinimized = false;

        private void OnMaximized() => IsMaximized = true;

        private void OnDemaximized() => IsMaximized = false;

        private void OnCloseRequested() => invokeCloseRequestedEvent = true;

        [Conditional("DEBUG")]
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(NativeWindow)}] Allready disposed");
        }
    }
}