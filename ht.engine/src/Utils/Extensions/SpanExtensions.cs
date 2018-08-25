using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HT.Engine.Utils
{
    public static class SpanExtensions
    {
        public static int GetSize<T>(this ReadOnlySpan<T> data) => Unsafe.SizeOf<T>() * data.Length;

        public static unsafe Span<T> Concat<T>(this ReadOnlySpan<T> a, ReadOnlySpan<T> b)
        {
            Span<T> result = SpanUtils.Create<T>(a.Length + b.Length);
            a.CopyTo(result);
            b.CopyTo(result.Slice(a.Length, b.Length));
            return result;
        }
    }
}