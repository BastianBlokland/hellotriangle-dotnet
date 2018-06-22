using HT.Engine.Math.Extensions;

namespace HT.Engine.Math
{
    public static class FloatUtils
    {
        //Note: constant is: PI / 180;
        public static float DegreesToRadians(float degrees) => degrees * 0.0174532924f;
        //Note: constant is: 180 / PI
        public static float RadiansToDegrees(float radians) => radians * 57.29578f;

        public static float Clamp(this float val, float min, float max)
            => val < min ? min : (val > max ? max : val);

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