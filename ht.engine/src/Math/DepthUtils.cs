using System;

namespace HT.Engine.Math
{
    /// <summary>
    /// Utilities for converting between a 0-1 non-linear depth value as used by the gpu and a normal
    /// linear depth distance.
    /// </summary>
    public static class DepthUtils
    {
        public static float DepthToLinear(float nonLinearDepth, float nearDist, float farDist)
            => 2f * nearDist * farDist / (farDist + nearDist - nonLinearDepth * (farDist - nearDist));

        public static float LinearToDepth(float linearDepth, float nearDist, float farDist)
            => (farDist + nearDist - 2f * nearDist * farDist / linearDepth) / (farDist - nearDist);
    }
}