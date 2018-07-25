using System;
using System.Runtime.CompilerServices;

namespace HT.Engine.Utils
{
    public static class SpanExtensions
    {
        public static int GetSize<T>(this Span<T> data) => Unsafe.SizeOf<T>() * data.Length;
    }
}