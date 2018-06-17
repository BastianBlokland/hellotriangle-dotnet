using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;
using HT.Engine.Rendering;
using HT.Engine.Utils;
using HT.Engine.Platform;
using HT.Win32.Delegates;
using HT.Win32.Structures;
using HT.Win32.Flags;

namespace HT.Win32
{
    /// <summary>
    /// Wrapper around a native Win32 window. Uses user32.dll bindings to for the native interop.
    /// </summary>
    internal sealed class NativeWindow : INativeWindow, IUpdatable
    {
        #region Native bindings
        [DllImport("user32.dll", EntryPoint = "RegisterClassExA", CharSet = CharSet.Ansi)]
        //Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms633587(v=vs.85).aspx
        private static extern ushort RegisterClassEx(ref WindowClassEx windowClass);

        [DllImport("user32.dll")]
        //Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms632667(v=vs.85).aspx
        private static extern bool AdjustWindowRect(ref IntRect rect, WindowStyles style, bool menu, ExtendedWindowStyles extendedStyle);

        [DllImport("user32.dll", EntryPoint = "CreateWindowExA", CharSet = CharSet.Ansi)]
        //Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms632680(v=vs.85).aspx
        private static extern IntPtr CreateWindowEx(    ExtendedWindowStyles extendedStyle,
                                                        IntPtr windowClassAtom,
                                                        string windowName,
                                                        WindowStyles style,
                                                        int x, int y, int width, int height,
                                                        IntPtr parent,
                                                        IntPtr menu,
                                                        IntPtr instance,
                                                        IntPtr param);

        [DllImport("user32.dll")]
        //Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms633548(v=vs.85).aspx
        private static extern bool ShowWindow(IntPtr windowHandle, int cmdShow);

        [DllImport("user32.dll")]
        //Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms633539(v=vs.85).aspx
        private static extern bool SetForegroundWindow(IntPtr windowHandle);

        [DllImport("user32.dll")]
        //Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms646312(v=vs.85).aspx
        private static extern IntPtr SetFocus(IntPtr windowHandle);

        [DllImport("user32.dll", EntryPoint = "DefWindowProcA", CharSet = CharSet.Ansi)]
        //Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms633572(v=vs.85).aspx
        private static extern IntPtr DefWindowProc(IntPtr windowHandle, uint message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "PeekMessageA", CharSet = CharSet.Ansi)]
        //Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms644943(v=vs.85).aspx
        private static extern bool PeekMessage(out WindowMessage message, IntPtr windowHandle, uint filterMin, uint filterMax, uint remove);

        [DllImport("user32.dll")]
        //Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms644955(v=vs.85).aspx
        private static extern bool TranslateMessage(ref WindowMessage message);

        [DllImport("user32.dll")]
        //Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms644934(v=vs.85).aspx
        private static extern IntPtr DispatchMessage(ref WindowMessage message);

        [DllImport("user32.dll", EntryPoint = "SetWindowTextA", CharSet = CharSet.Ansi)]
        //NOTE: The 'A' at the end means we're calling the ansi version
        //Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms633546(v=vs.85).aspx
        private static extern bool SetWindowText(IntPtr windowHandle, string text);

        [DllImport("user32.dll")]
        //Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms632682(v=vs.85).aspx
        private static extern bool DestroyWindow(IntPtr windowHandle);

        [DllImport("user32.dll")]
        //Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms644899(v=vs.85).aspx
        private static extern bool UnregisterClass(IntPtr windowClassAtom, IntPtr instance);
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
                if(title != value)
                {
                    ThrowIfDisposed();
                    SetWindowText(nativeWindowHandle, value);
                    title = value;
                }
            }
        }
        public bool Minimized { get; private set; }
        public bool Maximized { get; private set; }
        public bool IsMovingOrResizing { get; private set; }
        public IntRect ClientRect { get; private set; }

        public IntPtr OSInstanceHandle => instanceHandle;
        public IntPtr OSViewHandle => nativeWindowHandle;

        private IntPtr instanceHandle;
        private IntPtr nativeWindowClassAtom;
        private IntPtr nativeWindowHandle;
        private WindowStyles windowStyle;
        private ExtendedWindowStyles extendedWindowStyle;
        private string title;
        private Int2 minClientSize;
        private bool disposed;

        private bool invokeCloseRequestedEvent;
        private bool invokeResizedEvent;
        private bool invokeMovedEvent;

        public NativeWindow(Int2 size, Int2 minSize, string title)
        {
            this.title = title;
            this.minClientSize = minSize;

            //Value as used by the user32 functions to indicate that a default should be used
            const int USE_DEFAULT = unchecked((int)0x80000000);

            //Gets a handle to our process
            instanceHandle = System.Diagnostics.Process.GetCurrentProcess().Handle;

            //Create the 'WindowClass' that this window will be bound to
            WindowClassEx windowClass = new WindowClassEx
            (
                style: Flags.WindowClassStyles.HRedraw | Flags.WindowClassStyles.VRedraw,
                windowProcedure: WindowProcedure,
                classExtraBytes: 0,
                windowExtraBytes: 0,
                instance: instanceHandle,
                icon: IntPtr.Zero,
                cursor: IntPtr.Zero,
                backgroundBrush: IntPtr.Zero,
                menuName: null,
                className: nameof(NativeWindow) + Guid.NewGuid().ToString(), //Adding a guid here because the className needs to be unique
                iconSmall: IntPtr.Zero
            );
            nativeWindowClassAtom = new IntPtr(RegisterClassEx(ref windowClass));
            if(nativeWindowClassAtom == IntPtr.Zero)
                throw new Exception($"[{nameof(NativeWindow)}] Failed to create a window-class");
            
            //Setup window styles
            windowStyle = WindowStyles.OverlappedWindow;
            extendedWindowStyle = ExtendedWindowStyles.AppWindow | ExtendedWindowStyles.WindowEdge;

            //Need to adjust the rect here because we want to the requested size to be the client size so we need extra space for the top-bar
            IntRect windowRect = new IntRect(min: Int2.Zero, max: Int2.Max(size, minSize));
            AdjustWindowRect(ref windowRect, style: windowStyle, menu: false, extendedStyle: extendedWindowStyle);

            //Create the actual window
            nativeWindowHandle = CreateWindowEx
            (
                extendedStyle: extendedWindowStyle,
                windowClassAtom: nativeWindowClassAtom,
                windowName: title,
                style: windowStyle,
                x: USE_DEFAULT,
                y: USE_DEFAULT,
                width: windowRect.Width,
                height: windowRect.Height,
                parent: IntPtr.Zero,
                menu: IntPtr.Zero,
                instance: instanceHandle,
                param: IntPtr.Zero
            );
            if(nativeWindowHandle == IntPtr.Zero)
                throw new Exception($"[{nameof(NativeWindow)}] Failed to create a window-handle");

            const int SW_SHOW = 5;
            ShowWindow(nativeWindowHandle, SW_SHOW);

            SetForegroundWindow(nativeWindowHandle);
            SetFocus(nativeWindowHandle);
        }

        public void Update()
        {
            ThrowIfDisposed();

            //Event loop: Process a event if one is available from the thread queue
            WindowMessage message;
            const int PM_REMOVE = 1;
            if(PeekMessage(out message, nativeWindowHandle, filterMin: 0, filterMax: 0, remove: PM_REMOVE))
            {
                TranslateMessage(ref message);
                DispatchMessage(ref message);
            }

            //Invoke events that happened during this windowProcedure
            //Why dont we just call these from the windowProcedure? Well then the call origin of those callbacks
            //would actually be in the user32 dll and if then someone does something in the callbacks that changes 
            //the window state then that would happen from within the user32 callback and it doesn't like that.
            if(invokeCloseRequestedEvent)
            {
                CloseRequested?.Invoke();
                invokeCloseRequestedEvent = false;
            }
            if(invokeResizedEvent)
            {
                Resized?.Invoke();
                invokeResizedEvent = false;
            }
            if(invokeMovedEvent)
            {
                Moved?.Invoke();
                invokeMovedEvent = false;
            }
        }
        
        public void Dispose()
        {
            if(!disposed)
            {
                DestroyWindow(nativeWindowHandle);
                UnregisterClass(nativeWindowClassAtom, instanceHandle);
                disposed = true;
                Disposed?.Invoke();
            }
        }

        private IntPtr WindowProcedure(IntPtr windowHandle, uint message, IntPtr wParam, IntPtr lParam)
        {
            //Reference on these constants: https://msdn.microsoft.com/en-us/library/windows/desktop/ff468922(v=vs.85).aspx
            const uint WM_CLOSE = 0x0010;
            const uint WM_ENTERSIZEMOVE = 0x0231;
            const uint WM_EXITSIZEMOVE = 0x0232;
            const uint WM_SIZE = 0x0005;
            const uint WM_MOVE = 0x0003;
            const uint WM_GETMINMAXINFO = 0x0024;

            switch(message)
            {
                case WM_GETMINMAXINFO:
                    //This gets called when the window wants to know its min and max size
                    WindowMinMaxInfo info = Marshal.PtrToStructure<WindowMinMaxInfo>(lParam);

                    IntRect windowRect = new IntRect(min: Int2.Zero, max: minClientSize);
                    //Adjust here so the size we clamp to is actually the clientSize and not the outer size (that includes the topbar)
                    AdjustWindowRect(ref windowRect, style: windowStyle, menu: false, extendedStyle: extendedWindowStyle);

                    info.MinTrackSize = windowRect.Size;
                    Marshal.StructureToPtr(info, lParam, fDeleteOld: false); //No need for deleteOld because its plain data that doesn't hold references 
                    break;

                case WM_SIZE:
                    //Constants that can apply to the 'wParam'
                    //Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms632646(v=vs.85).aspx
                    const int SIZE_MAXIMIZED = 2;
                    const int SIZE_MINIMIZED = 1;

                    int sizeType = wParam.ToInt32();
                    Minimized = sizeType == SIZE_MINIMIZED;
                    Maximized = sizeType == SIZE_MAXIMIZED;
                   
                    short width, height;
                    lParam.ToInt64().Split(out width, out height);
                    
                    ClientRect = new IntRect(ClientRect.Min, ClientRect.Min + new Int2(width, height));
                    invokeResizedEvent = true;
                    break;

                case WM_MOVE:
                    short x, y;
                    lParam.ToInt64().Split(out x, out y);
                    
                    Int2 pos = new Int2(x, y);
                    ClientRect = new IntRect(pos, pos + ClientRect.Size);
                    invokeMovedEvent = true;
                    break;

                case WM_ENTERSIZEMOVE:
                    IsMovingOrResizing = true;
                    break;

                case WM_EXITSIZEMOVE:
                    IsMovingOrResizing = false;
                    break;

                case WM_CLOSE:
                    invokeCloseRequestedEvent = true;
                    return IntPtr.Zero; //'Consume' the message
            }

            //Fallback to the default window-procedure if we don't handle this message
            return DefWindowProc(windowHandle, message, wParam, lParam);
        }

        private void ThrowIfDisposed()
        {
            if(disposed)
                throw new Exception($"[{nameof(NativeWindow)}] Allready disposed");
        }
    }
}