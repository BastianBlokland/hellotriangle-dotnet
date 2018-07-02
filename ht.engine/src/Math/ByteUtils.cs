using HT.Engine.Math;

using static System.Math;

namespace HT.Engine.Math
{
    public static class ByteUtils
    {
        public const long KILOBYTE_TO_BYTE = 1024;
        public const long MEGABYTE_TO_BYTE = 1024 * 1024;
        public const long GIGABYTE_TO_BYTE = 1024 * 1024 * 1024;

        public static long KilobyteToByte(long kilobytes) => kilobytes * KILOBYTE_TO_BYTE;
        public static long ByteToKilobyte(long bytes) => bytes / KILOBYTE_TO_BYTE;

        public static long MegabyteToByte(long megabytes) => megabytes * MEGABYTE_TO_BYTE;
        public static long ByteToMegabyte(long bytes) => bytes / MEGABYTE_TO_BYTE;

        public static long GigabyteToByte(long gigabytes) => gigabytes * GIGABYTE_TO_BYTE;
        public static long ByteToGigabyte(long bytes) => bytes / GIGABYTE_TO_BYTE;
    }
}