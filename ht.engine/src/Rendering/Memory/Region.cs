using System;
using System.Runtime.InteropServices;

namespace HT.Engine.Rendering.Memory
{
    [StructLayout(LayoutKind.Sequential, Size = SIZE)]
    internal struct Region : IEquatable<Region>
    {
        public const int SIZE = sizeof(long) * 3;

        public readonly long Offset;
        public readonly long Size;
        public readonly long RequestedSize;

        public Region(long offset, long size, long requestedSize)
        {
            Offset = offset;
            Size = size;
            RequestedSize = requestedSize;
        }

        public static bool operator ==(Region a, Region b) => a.Equals(b);

        public static bool operator !=(Region a, Region b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is Region && Equals((Region)obj);

        public bool Equals(Region other) => 
            other.Offset == Offset &&
            other.Size == Size &&
            other.RequestedSize == RequestedSize;

        public override int GetHashCode() => 
            Offset.GetHashCode() ^ 
            Size.GetHashCode() ^
            RequestedSize.GetHashCode();

        public override string ToString() 
            => $"(Offset: {Offset}, Size: {Size}, RequestedSize: {RequestedSize})";
    }
}