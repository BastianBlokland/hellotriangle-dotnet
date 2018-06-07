using System;
using System.Runtime.InteropServices;
using HT.Win32.Flags;

namespace HT.Win32.Structures
{
    /// <summary>
    /// 'WNDCLASSEX' structure as used in user32.dll window calls
    /// Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms633577(v=vs.85).aspx
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct WindowClassEx
    {
        public readonly uint Size;
        public readonly Flags.WindowClassStyles Style;
        [MarshalAs(UnmanagedType.FunctionPtr)] 
        public readonly Delegates.WindowProcedure WindowProcedure;
        public readonly int ClassExtraBytes;
        public readonly int WindowExtraBytes;
        public readonly IntPtr Instance;
        public readonly IntPtr Icon;
        public readonly IntPtr Cursor;
        public readonly IntPtr BackgroundBrush;
        public readonly string MenuName;
        public readonly string ClassName;
        public readonly IntPtr IconSmall;

        public WindowClassEx(   WindowClassStyles style,
                                Delegates.WindowProcedure windowProcedure,
                                int classExtraBytes,
                                int windowExtraBytes,
                                IntPtr instance,
                                IntPtr icon,
                                IntPtr cursor,
                                IntPtr backgroundBrush,
                                string menuName,
                                string className,
                                IntPtr iconSmall)
        {
            Size = (uint)Marshal.SizeOf(typeof(WindowClassEx));
            Style = style;
            WindowProcedure = windowProcedure;
            ClassExtraBytes = classExtraBytes;
            WindowExtraBytes = windowExtraBytes;
            Instance = instance;
            Icon = icon;
            Cursor = cursor;
            BackgroundBrush = backgroundBrush;
            MenuName = menuName;
            ClassName = className;
            IconSmall = iconSmall;
        }
    }
}