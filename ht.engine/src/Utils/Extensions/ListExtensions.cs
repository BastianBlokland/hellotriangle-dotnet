using System;
using System.Collections.Generic;

namespace HT.Engine.Utils
{
    public static class ListExtensions
    {
        public static bool IsEmpty<T>(this IList<T> list) => list.Count <= 0;
    }
}