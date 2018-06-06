using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.Win32
{
    /// <summary>
    /// Wrapper arround a native Win32 window. Uses user32.dll bindings to for the native interop. 
    /// </summary>
    internal sealed class NativeWindow : HT.Engine.Platform.INativeWindow
    {
        #region Native bindings
        //Value as used by the user32 functions to indicate that a default should be used
        private const int USE_DEFAULT = unchecked((int)0x80000000); 

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        //NOTE: The 'A' at the end means we're calling the ansi version
        //Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms633587(v=vs.85).aspx
        private static extern ushort RegisterClassExA(ref Structures.WindowClassEx windowClass);

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        //NOTE: The 'A' at the end means we're calling the ansi version
        //Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms632680(v=vs.85).aspx
        private static extern IntPtr CreateWindowExA(   Flags.ExtendedWindowStyles extendedStyle,
                                                        IntPtr windowClassAtom,
                                                        string windowName,
                                                        Flags.WindowStyles style,
                                                        int x,
                                                        int y,
                                                        int width,
                                                        int height,
                                                        IntPtr parent,
                                                        IntPtr menu,
                                                        IntPtr instance,
                                                        IntPtr param);

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        //NOTE: The 'A' at the end means we're calling the ansi version
        //Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms633572(v=vs.85).aspx
        private static extern IntPtr DefWindowProcA(IntPtr windowHandle, uint message, IntPtr wParam, IntPtr lParam);
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
                    //Set title
                    title = value;
                }
            }
        }
        public bool IsResizing { get; private set; }
        public Float2 Size { get; private set; }
        public Float2 MinSize { get; set; } = new Float2(0f, 0f);
        public Float2 MaxSize { get; set; } = new Float2(float.MaxValue, float.MaxValue);

        private string title;
        private bool disposed;

        public NativeWindow(Float2 size)
        {
            Size = size;

            Structures.WindowClassEx windowClass = new Structures.WindowClassEx
            (
                style: Flags.WindowClassStyles.HRedraw | Flags.WindowClassStyles.VRedraw,
                windowProcedure: WindowProcedure,
                classExtraBytes: 0,
                windowExtraBytes: 0,
                instance: IntPtr.Zero,
                icon: IntPtr.Zero,
                cursor: IntPtr.Zero,
                backgroundBrush: IntPtr.Zero,
                menuName: null,
                className: nameof(NativeWindow),
                iconSmall: IntPtr.Zero
            );
            IntPtr windowClassAtom = new IntPtr(RegisterClassExA(ref windowClass));
            if(windowClassAtom == IntPtr.Zero)
                throw new Exception($"[{nameof(NativeWindow)}] Failed to create a window-class");
            
            IntPtr windowHandle = CreateWindowExA
            (
                extendedStyle: Flags.ExtendedWindowStyles.Default,
                windowClassAtom: windowClassAtom,
                windowName: null,
                style: Flags.WindowStyles.OverlappedWindow,
                x: USE_DEFAULT,
                y: USE_DEFAULT,
                width: (int)size.X, //TODO: Need to adjust this size for the topbar
                height: (int)size.Y, //TODO: Need to adjust this size for the topbar
                parent: IntPtr.Zero,
                menu: IntPtr.Zero,
                instance: IntPtr.Zero,
                param: IntPtr.Zero
            );
            if(windowHandle == IntPtr.Zero)
                throw new Exception($"[{nameof(NativeWindow)}] Failed to create a window-handle");

            
        }
        
        public void Dispose()
        {
            if(!disposed)
            {
                //Dispose window
                disposed = true;
            }
        }

        private IntPtr WindowProcedure(IntPtr windowHandle, uint message, IntPtr wParam, IntPtr lParam)
        {
            //Fallback to the default window-procedure if we don't handle this message
            return DefWindowProcA(windowHandle, message, wParam, lParam);
        }

        private void ThrowIfDisposed()
        {
            if(disposed)
                throw new Exception($"[{nameof(NativeWindow)}] Allready disposed!");
        }
    }
}