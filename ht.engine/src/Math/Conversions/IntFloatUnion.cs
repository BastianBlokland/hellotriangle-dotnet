using System.Runtime.InteropServices;

namespace HT.Engine.Math.Conversions
{
    public static class IntFloatUnion
    {
        [StructLayout(LayoutKind.Explicit, Size = 4)]
        private struct IntFloat 
        {
            [FieldOffset(0)] public float FloatValue;
            [FieldOffset(0)] public int IntValue;
        }

        public static float IntAsFloat(int value)
        {
            var conversion = new IntFloat();
            conversion.IntValue = value;
            return conversion.FloatValue;
        }

        public static int FloatAsInt(float value)
        {
            var conversion = new IntFloat();
            conversion.FloatValue = value;
            return conversion.IntValue;
        }
    }
}