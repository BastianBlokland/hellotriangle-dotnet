using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.Win32.Structures
{
    /// <summary>
    /// 'MINMAXINFO ' structure as used in user32.dll window calls
    /// Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms632605(v=vs.85).aspx
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct WindowMinMaxInfo
    {
        public readonly Int2 Reserved; //Unused
        public Int2 MaxSize;
        public Int2 MaxPosition;
        public Int2 MinTrackSize;
        public Int2 MaxTrackSize;

        public WindowMinMaxInfo(    Int2 maxSize,
                                    Int2 maxPosition,
                                    Int2 minTrackSize,
                                    Int2 maxTrackSize)
        {
            Reserved = new Int2();
            MaxSize = maxSize;
            MaxPosition = maxPosition;
            MinTrackSize = minTrackSize;
            MaxTrackSize = maxTrackSize;
        }
    }
}