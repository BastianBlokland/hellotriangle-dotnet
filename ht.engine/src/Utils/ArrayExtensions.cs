using System;

namespace HT.Engine.Utils
{
    public static class ArrayExtensions
    {
        /// <summary>
        /// Concatenates two arrays into a new array
        /// NOTE: This allocates a new array
        /// </summary>
        public static T[] Concat<T>(this T[] a, T[] b)
        {
            int aLength = a.Length;
            //Resize a to also fit b (Note: this allocates a new array)
            Array.Resize<T>(ref a, a.Length + b.Length);
            //Copy the contents of b into this new bigger array
            Array.Copy
            (
                sourceArray: b, sourceIndex: 0,
                destinationArray: a, destinationIndex: aLength,
                length: b.Length
            );
            return a;
        }

        /// <summary>
        /// Concatenates a element after the array into a new array
        /// NOTE: This allocates a new array
        /// </summary>
        public static T[] Concat<T>(this T[] a, T b)
        {
            //Resize to fit one more (Note: this allocates a new array)
            Array.Resize(ref a, a.Length + 1);
            //Add the new element at the end of the new bigger array
            a[a.Length - 1] = b;
            return a;
        }
    }
}