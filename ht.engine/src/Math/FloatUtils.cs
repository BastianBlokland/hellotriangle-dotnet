using HT.Engine.Math;

using static System.Math;

namespace HT.Engine.Math
{
    public static class FloatUtils
    {
        public const float PI = 3.14159265359f;
        public const float DEG_TO_RAD = PI / 180f;
        public const float RAD_TO_DEG = 180f / PI;

        public static float DegreesToRadians(float degrees) => degrees * DEG_TO_RAD;

        public static float RadiansToDegrees(float radians) => radians * RAD_TO_DEG;

        public static bool Approx(float a, float b, float maxDifference = .0001f)
            => Abs(b - a) < maxDifference;

        public static float Clamp(this float val, float min, float max)
            => val < min ? min : (val > max ? max : val);

        public static float Clamp01(float value) => Clamp(value, 0f, 1f);

        public static float Lerp(float a, float b, float t) => a + (b - a) * t;

        public static float UnLerp(float value, float a, float b)
            => (a == b) ? 0f : (value - a) / (b - a);

        //Implementation based on: https://en.wikipedia.org/wiki/Fast_inverse_square_root
        public static float FastInverseSquareRoot(float number, int precision = 2)
        {
            //Note: Higher number of precisions run more iterations of Newton's approx method,
            //1 allready gives a decent result and 2 makes it pretty good.

            int intVal = number.AsInt();
            //More info about this completely magic number can be found in the wiki page mentioned
            intVal = 0x5f3759df - (intVal >> 1); //Initial guess for Newton's approx method

            float floatVal = intVal.AsFloat();
            for (int i = 0; i < precision; i++)
            {
                //Iteration of Newton's approx method
                floatVal *= 1.5f - (number * .5f * floatVal * floatVal);
            }
            return floatVal;
        }
    }
}