using System;

using HT.Engine.Math;

namespace HT.Engine.Math
{
    public static class IntUtils
    {
        public static int Min(int a, int b) => a < b ? a : b;
        
        public static int Max(int a, int b) => a > b ? a : b;

        public static int? TryPerfectSquareRoot(int val)
        {
            int squareRoot = (int)FloatUtils.SquareRoot(val);
            if (squareRoot * squareRoot != val)
                return null;
            return squareRoot;
        }

        public static int PerfectSquareRoot(int val)
        {
            int squareRoot = (int)FloatUtils.SquareRoot(val);
            if (squareRoot * squareRoot != val)
                throw new Exception($"[{nameof(IntUtils)}] '{val}' has no perfect square-root");
            return squareRoot;
        }

        public static int? TryPerfectCubeRoot(int val)
        {
            int cubeRoot = (int)FloatUtils.CubeRoot(val);
            if (cubeRoot * cubeRoot * cubeRoot != val)
                return null;
            return cubeRoot;
        }

        public static int PerfectCubeRoot(int val)
        {
            int cubeRoot = (int)FloatUtils.CubeRoot(val);
            if (cubeRoot * cubeRoot * cubeRoot != val)
                throw new Exception($"[{nameof(IntUtils)}] '{val}' has no perfect cube-root");
            return cubeRoot;
        }
    }
}