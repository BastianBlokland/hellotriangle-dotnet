namespace HT.Engine.Utils
{
    public static class TimeUtils
    {
        public const float SECONDS_TO_MILLISECONDS = .001f;
        public const float MILLISECONDS_TO_SECONDS = 1000f;

        public static double SecondsToMilliseconds(double seconds)
            => seconds * SECONDS_TO_MILLISECONDS;

        public static double MillisecondsToSeconds(double milliseconds)
            => milliseconds * MILLISECONDS_TO_SECONDS;
    }
}