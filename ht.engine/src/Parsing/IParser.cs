using System;

namespace HT.Engine.Parsing
{
    public interface IParser<T> : IDisposable
    {
        T Parse();
    }
}