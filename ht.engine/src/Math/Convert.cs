using System.Runtime.CompilerServices;

namespace HT.Engine.Math
{
    public static class Convert
    {
        public static int UIntAsInt(ref uint value) => Unsafe.As<uint, int>(ref value);

        public static float UIntAsFloat(ref uint value) => Unsafe.As<uint, float>(ref value);

        public static uint IntAsUInt(ref int value) => Unsafe.As<int, uint>(ref value);

        public static float IntAsFloat(ref int value) => Unsafe.As<int, float>(ref value);
        
        public static uint FloatAsUInt(ref float value) => Unsafe.As<float, uint>(ref value);

        public static int FloatAsInt(ref float value) => Unsafe.As<float, int>(ref value);
    }
}