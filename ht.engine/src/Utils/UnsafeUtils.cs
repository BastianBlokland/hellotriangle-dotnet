using System;

namespace HT.Engine.Utils
{
    public static class UnsafeUtils
    {
        /// <summary>
        /// Assign a generic value type to another generic value type.
        /// Note: This only works if both types match. So why is this usefull? In some cases
        /// you know that a generic type is of a certain type (because you checked) but the
        /// compiler will still not let you convert to a non generic type.
        /// 
        /// Btw this does no boxing and its about a thousand times better then using object
        /// as a intermediate type to do this conversion
        /// 
        /// With this is you do simple generic specialization
        /// for example:
        /// 
        /// void Test<T>() where T : struct
        /// {
        ///     T data = default(T);
        ///     if (data is float)
        ///         ForceAssign(ref data, 133f);
        /// }
        /// 
        /// Even tho it is considered code-smell to do generic specialization like this but i think
        /// it has its uses.
        /// </summary>
        public static void ForceAssign<T1, T2>(ref T1 variable, in T2 data)
            where T1 : struct
            where T2 : struct
        {
            #if DEBUG
            if (typeof(T1) != typeof(T2))
                throw new Exception($"[{nameof(UnsafeUtils)}] Given types don't match!");
            #endif

            //These funny looking functions are actually c# keywords
            //there is not much documentation about them but __makeref returns a TypedReference
            //https://msdn.microsoft.com/en-us/library/system.typedreference(v=vs.110).aspx
            //and __refvalue can assign (or get data from) a TypedReference
            //some more info is here:
            //http://benbowen.blog/post/fun_with_makeref/

            var typedRef = __makeref(variable);
            __refvalue(typedRef, T2) = data;
        }
    }
}