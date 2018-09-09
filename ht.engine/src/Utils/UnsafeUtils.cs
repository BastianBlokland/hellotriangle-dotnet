using System;
using System.Runtime.CompilerServices;

namespace HT.Engine.Utils
{
    public static class UnsafeUtils
    {
        /// <summary>
        /// Assign a generic value type to another generic value type.
        /// Note: This only works if both types are the same size
        /// </summary>
        public static void Assign<T1, T2>(ref T1 variable, T2 data)
            where T1 : struct
            where T2 : struct
        {
            #if DEBUG
            if (Unsafe.SizeOf<T1>() != Unsafe.SizeOf<T2>())
                throw new Exception(
                    $"[{nameof(UnsafeUtils)}] Given types have different sizes, cannot reinterpret");
            #endif
            variable = Unsafe.As<T2, T1>(ref data);
        }
    }
}