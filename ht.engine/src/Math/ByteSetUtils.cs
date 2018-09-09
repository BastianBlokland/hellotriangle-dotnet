using System;
using HT.Engine.Utils;

namespace HT.Engine.Math
{
    //This class is the byte version of the FloatSetUtils, 
    //for some implementation notes check that class
    public static class ByteSetUtils
    {
        public static int GetComponentCount<T>()
            where T : struct, IByteSet
        {
            T value = default(T);
            switch (value)
            {
            case Byte1 _: return 1;
            case Byte2 _: return 2;
            case Byte3 _: return 3;
            case Byte4 _: return 4;
            }
            throw new Exception($"[{nameof(ByteSetUtils)}] Unknown type: {typeof(T)}");
        }

        public static T Create<T>(in ReadOnlySpan<byte> data)
            where T : struct, IByteSet
        {
            #if DEBUG
            if (data.Length < GetComponentCount<T>())
                throw new Exception($"[{nameof(ByteSetUtils)}] No enough elements in given data");
            #endif
            T value = default(T);
            switch (value)
            {
            case Byte1 _: UnsafeUtils.Assign(ref value, new Byte1(data[0])); break;
            case Byte2 _: UnsafeUtils.Assign(ref value, new Byte2(data[0], data[1])); break;
            case Byte3 _: UnsafeUtils.Assign(ref value, new Byte3(data[0], data[1], data[2])); break;
            case Byte4 _: UnsafeUtils.Assign(ref value, new Byte4(data[0], data[1], data[2], data[3])); break;
            }
            return value;
        }
    }
}