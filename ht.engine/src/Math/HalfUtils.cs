using System;

using HT.Engine.Math;

namespace HT.Engine.Math
{
    public static class HalfUtils
    {
        public static void HalfToFloat(ReadOnlySpan<Half> input, Span<float> output)
        {
            if (input.Length != output.Length)
                throw new ArgumentException(
                    $"[{nameof(HalfUtils)}] Output size does not match input size", nameof(output));
            
            for (int i = 0; i < input.Length; i++)
                output[i] = input[i].ToFloat();
        }

        public static void FloatToHalf(ReadOnlySpan<float> input, Span<Half> output)
        {
            if (input.Length != output.Length)
                throw new ArgumentException(
                    $"[{nameof(HalfUtils)}] Output size does not match input size", nameof(output));
            
            for (int i = 0; i < input.Length; i++)
                output[i] = input[i].ToHalf();
        }
    }
}