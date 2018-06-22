using System;
using System.Collections.Generic;

namespace HT.Engine.Utils.Extensions
{
    public static class UpdatableExtensions
    {
        public static void UpdateAll<T>(this IList<T> list)
            where T : IUpdatable
        {
            for (int i = 0; i < list.Count; i++)
                list[i].Update();
        }
    }
}