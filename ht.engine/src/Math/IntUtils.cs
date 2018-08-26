using System;

using HT.Engine.Math;

namespace HT.Engine.Math
{
    public static class IntUtils
    {
        public static int Min(int a, int b) => a < b ? a : b;
        
        public static int Max(int a, int b) => a > b ? a : b;

        public static int PerfectCubeRoot(int val)
        {
            int cubeRoot = (int)FloatUtils.CubeRoot(val);
            if (cubeRoot * cubeRoot * cubeRoot != val)
                throw new Exception($"[{nameof(IntUtils)}] '{val}' has no perfect cube-root");
            return cubeRoot;
        }
    }
}