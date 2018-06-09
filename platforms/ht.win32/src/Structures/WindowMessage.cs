using System;
using System.Runtime.InteropServices;

using HT.Win32.Flags;
using HT.Engine.Math;

namespace HT.Win32.Structures
{
    /// <summary>
    /// 'MSG' structure as used in user32.dll window calls
    /// Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms644958(v=vs.85).aspx
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct WindowMessage
    {
        public readonly IntPtr WindowHandle;
        public readonly uint Message;
        public readonly IntPtr LParam;
        public readonly IntPtr WParam;
        public readonly uint Time;
        public readonly Int2 Point;

        public WindowMessage(   IntPtr windowHandle,
                                uint message,
                                IntPtr lParam,
                                IntPtr wParam,
                                uint time,
                                Int2 point)
        {
            WindowHandle = windowHandle;
            Message = message;
            LParam = lParam;
            WParam = wParam;
            Time = time;
            Point = point;
        }
    }
}