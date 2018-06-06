using System;

namespace HT.Win32.Flags
{
    /// <summary>
    /// 'WindowClassStyles' as used in user32.dll window calls
    /// Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ff729176(v=vs.85).aspx
    /// </summary>
    [Flags] internal enum WindowClassStyles
    {
        Default = 0x0000,
        ByteAlignClient = 0x1000,
        ByteAlignWindow = 0x2000,
        ClassDC = 0x0040,
        DoubleClicks = 0x0008,
        DropShadow = 0x00020000,
        GlobalClass = 0x4000,
        HRedraw = 0x0002,
        NoClose = 0x0200,
        OwnDC = 0x0020,
        ParentDC = 0x0080,
        SaveBits = 0x0800,
        VRedraw = 0x0001
    }
}