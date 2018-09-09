using System.Runtime.CompilerServices;

namespace HT.Engine.Math
{
    public static class Convert
    {
        public static int UIntAsInt(uint value) => Unsafe.As<uint, int>(ref value);

        public static float UIntAsFloat(uint value) => Unsafe.As<uint, float>(ref value);

        public static uint IntAsUInt(int value) => Unsafe.As<int, uint>(ref value);

        public static float IntAsFloat(int value) => Unsafe.As<int, float>(ref value);
        
        public static uint FloatAsUInt(float value) => Unsafe.As<float, uint>(ref value);

        public static int FloatAsInt(float value) => Unsafe.As<float, int>(ref value);
    }
}