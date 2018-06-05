using System.Runtime.InteropServices;

namespace HT.Engine.Math
{
    public static class FloatExtensions
    {
        public static float Clamp(this float val, float min, float max) => (val < min) ? min : (val > max) ? max : val;
    }
}