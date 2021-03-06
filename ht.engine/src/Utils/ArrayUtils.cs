using System;

namespace HT.Engine.Utils
{
    public static class ArrayUtils
    {
        public static B[] MorphArray<A, B>(this A[] input, Func<A, B> selector)
            => MorphArray<A, B>(input.AsSpan(), selector);

        public static B[] MorphArray<A, B>(this ReadOnlySpan<A> input, Func<A, B> selector)
        {
            B[] result = new B[input.Length];
            for (int i = 0; i < input.Length; i++)
                result[i] = selector(input[i]);
            return result;
        }

        public static T[] Build<T>(params object[] items)
        {
            //Gather count
            int count = 0;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] is T)
                    count++;
                else
                if (items[i] is T[])
                    count += ((T[])items[i]).Length;
                else
                    throw new Exception(
                        $"[{nameof(ArrayUtils)}] Incorrect type in input array");
            }
            
            //Allocate array
            T[] output = new T[count];

            //Populate array
            int outputIndex = 0;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] is T)
                {
                    output[outputIndex] = (T)items[i];
                    outputIndex++;
                }
                else
                {
                    T[] inputArray = (T[])items[i];
                    for (int j = 0; j < inputArray.Length; j++)
                        output[outputIndex + j] = inputArray[j];
                    outputIndex += inputArray.Length;
                }
            }
            return output;
        }
    }
}