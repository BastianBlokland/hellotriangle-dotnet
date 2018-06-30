using System;
using HT.Engine.Utils;

namespace HT.Engine.Math
{
    public static class FloatSetUtils
    {
        public static int GetComponentCount<T>()
            where T : struct, IFloatSet
        {
            //Its debabtably if we should be using the switching on the type here or a if-else chain
            //the advantage of the switch is that the compiler should be able to use jump-tables to
            //avoid checking all cases but the downside is that it has to allocate the value on the 
            //stack. But because the FloatX structs are small i think the jump-table approach is going
            //to be faster
            T value = default(T);
            switch (value)
            {
            case Float1 _: return 1;
            case Float2 _: return 2;
            case Float3 _: return 3;
            case Float4 _: return 4;
            }
            throw new Exception($"[{nameof(FloatSetUtils)}] Unknown type: {typeof(T)}");
        }

        public static T Create<T>(in Span<float> data)
            where T : struct, IFloatSet
        {
            //This implementation uses some dirty generic specialization but its a very fast and
            //generic way to create a floatX out of a arbitrary set of floats. And also it stays
            //completely typed and doesn't do any boxing
            T value = default(T);
            switch (value)
            {
            case Float1 _:
                #if DEBUG
                if (data.Length < 1)
                    throw new Exception($"[{nameof(FloatSetUtils)}] No enough elements in given data");
                #endif
                UnsafeUtils.ForceAssign(ref value, new Float1(data[0]));
                break;
            case Float2 _:
                #if DEBUG
                if (data.Length < 2)
                    throw new Exception($"[{nameof(FloatSetUtils)}] No enough elements in given data");
                #endif
                UnsafeUtils.ForceAssign(ref value, new Float2(data[0], data[1]));
                break;
            case Float3 _:
                #if DEBUG
                if (data.Length < 3)
                    throw new Exception($"[{nameof(FloatSetUtils)}] No enough elements in given data");
                #endif
                UnsafeUtils.ForceAssign(ref value, new Float3(data[0], data[1], data[2]));
                break;
            case Float4 _:
                #if DEBUG
                if (data.Length < 4)
                    throw new Exception($"[{nameof(FloatSetUtils)}] No enough elements in given data");
                #endif
                UnsafeUtils.ForceAssign(ref value, new Float4(data[0], data[1], data[2], data[3]));
                break;
            }
            return value;
        }
    }
}