using System;
using System.Collections.Generic;

namespace HT.Engine.Utils
{
    public static class DisposableExtensions
    {
        public static void DisposeAll<T>(this IList<T> list)
            where T : IDisposable
        {
            for (int i = 0; i < list.Count; i++)
                list[i].Dispose();
        }
    }
}