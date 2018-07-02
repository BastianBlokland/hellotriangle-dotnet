using System;

namespace HT.Engine.Parsing
{
    public interface IParser : IDisposable
    {
        object Parse();
    }

    public interface IParser<T> : IDisposable
    {
        T Parse();
    }
}