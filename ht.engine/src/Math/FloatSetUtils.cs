using System;

namespace HT.Engine.Math
{
    public static class FloatSetUtils
    {
        public static int GetComponentCount<T>()
            where T : IFloatSet
        {
            Type t = typeof(T);
            if (t == typeof(Float1)) return 1;
            if (t == typeof(Float2)) return 2;
            if (t == typeof(Float3)) return 3;
            if (t == typeof(Float4)) return 4;
            throw new Exception($"[{nameof(FloatSetUtils)}] Unknown type: {t.Name}");
        }
    }
}