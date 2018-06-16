namespace HT.Engine.Utils
{
    public static class DebugUtils
    {
        #if DEBUG
        public const bool IS_DEBUG = true;
        #else
        public const bool IS_DEBUG = false;
        #endif
    }
}