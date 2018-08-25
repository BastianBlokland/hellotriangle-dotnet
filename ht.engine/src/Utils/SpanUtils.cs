using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HT.Engine.Utils
{
    public static class SpanUtils
    {
        public static unsafe Span<T> Create<T>(int count)
        {
            int size = Unsafe.SizeOf<T>() * count;
            byte* stackArray = stackalloc byte[size];
            return new Span<T>(stackArray, count);
        }

        public static unsafe Span<T> Create<T>(T a)
        {
            Span<T> result = Create<T>(1);
            result[0] = a;
            return result;
        }
        
        public static unsafe Span<T> Create<T>(T a, T b)
        {
            Span<T> result = Create<T>(2);
            result[0] = a;
            result[1] = b;
            return result;
        }

        public static unsafe Span<T> Create<T>(T a, T b, T c)
        {
            Span<T> result = Create<T>(3);
            result[0] = a;
            result[1] = b;
            result[2] = c;
            return result;
        }

        public static unsafe Span<T> Create<T>(T a, T b, T c, T d)
        {
            Span<T> result = Create<T>(4);
            result[0] = a;
            result[1] = b;
            result[2] = c;
            result[3] = d;
            return result;
        }

        public static unsafe Span<T> Create<T>(T a, T b, T c, T d, T e)
        {
            Span<T> result = Create<T>(5);
            result[0] = a;
            result[1] = b;
            result[2] = c;
            result[3] = d;
            result[4] = e;
            return result;
        }

        public static unsafe Span<T> Create<T>(T a, T b, T c, T d, T e, T f)
        {
            Span<T> result = Create<T>(6);
            result[0] = a;
            result[1] = b;
            result[2] = c;
            result[3] = d;
            result[4] = e;
            result[5] = f;
            return result;
        }

        public static unsafe Span<T> Create<T>(T a, T b, T c, T d, T e, T f, T g)
        {
            Span<T> result = Create<T>(7);
            result[0] = a;
            result[1] = b;
            result[2] = c;
            result[3] = d;
            result[4] = e;
            result[5] = f;
            result[6] = g;
            return result;
        }

        public static unsafe Span<T> Create<T>(T a, T b, T c, T d, T e, T f, T g, T h)
        {
            Span<T> result = Create<T>(8);
            result[0] = a;
            result[1] = b;
            result[2] = c;
            result[3] = d;
            result[4] = e;
            result[5] = f;
            result[6] = g;
            result[7] = h;
            return result;
        }
    }
}