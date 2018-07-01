using System;

namespace HT.Engine.Math
{
    public interface IByteSet
    {
        int ComponentCount { get; }

        byte this[int i] { get; }
    }
}