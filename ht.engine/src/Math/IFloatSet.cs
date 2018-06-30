using System;

namespace HT.Engine.Math
{
    public interface IFloatSet
    {
        int ComponentCount { get; }

        float this[int i] { get; }
    }
}