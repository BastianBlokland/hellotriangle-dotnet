namespace HT.Engine.Math
{
    public static class UIntExtensions
    {
        public static float AsFloat(this uint val) => Convert.UIntAsFloat(ref val);
    }
}