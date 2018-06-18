using System;
using System.Runtime.InteropServices;

namespace HT.Win32.Delegates
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    //Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms633573(v=vs.85).aspx
    internal delegate IntPtr WindowProcedure(
        IntPtr windowHandle, uint message, IntPtr wParam, IntPtr lParam);
}