using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HT.Engine.Utils
{
    public static class ByteArrayExtensions
    {
        /// <summary>
        /// Interpret the given data as a structure
        /// NOTE: Make sure your struct has a sequential layout otherwise sparks may fly out
        /// </summary>
        public static unsafe T Parse<T>(this byte[] data)
            where T : struct
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            int size = Unsafe.SizeOf<T>();
            if (data.Length < size)
                throw new ArgumentException($"[{nameof(ByteArrayExtensions)}] Not enough bytes in the data", nameof(data));

            //Allocate (unmanaged) memory for the struct
            IntPtr structPointer = Marshal.AllocHGlobal(size);
            //Copy the data to our struct
            Marshal.Copy(data, startIndex: 0, destination: structPointer, length: size);
            //Bring the struct into the managed world
            T result = Marshal.PtrToStructure<T>(structPointer);
            //Free the unmanaged memory
            Marshal.FreeHGlobal(structPointer);

            return result;
        }
    }
}