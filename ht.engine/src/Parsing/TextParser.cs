using System;
using System.Collections.Generic;
using System.IO;

using HT.Engine.Utils;

namespace HT.Engine.Parsing
{
    /// <summary>
    /// Base class for a single pass text parser
    /// </summary>
    public abstract class TextParser<T> : IParser<T>, IDisposable
    {
        protected struct Entry
        {
            //Helper properties
            public bool IsEndOfFile => value < 0;
            public bool IsEndOfLine => IsCharacter('\r') || IsCharacter('\n');
            public bool IsWhitespace => !IsEndOfFile && !IsEndOfLine && Char.IsWhiteSpace((char)value);
            public bool IsDigit => !IsEndOfFile && Char.IsDigit((char)value);

            //Data
            private readonly int value;

            public Entry(int value) => this.value = value;

            public bool IsCharacter(char character) => !IsEndOfFile && (char)value == character;

            public override string ToString() => value.ToString();
        }

        //Helper properties
        protected Entry Current => new Entry(inputReader.Peek());

        //Data
        private readonly TextReader inputReader;
        private readonly ResizeArray<char> charCache = new ResizeArray<char>();
        private T result;
        private bool parsed;

        public TextParser(Stream inputStream)
            => inputReader = new StreamReader(inputStream);

        public T Parse()
        {
            if (!parsed)
            {
                while (!Current.IsEndOfFile)
                    ConsumeToken();
                result = Construct();
                parsed = true;
            }
            return result;
        }

        public void Dispose() => inputReader.Dispose();

        protected abstract void ConsumeToken();
        protected abstract T Construct();

        protected float ConsumeFloat()
        {
            charCache.Clear();
            //Optionally consume a negative sign
            if (Current.IsCharacter('-'))
                charCache.Add(Consume());
            //Consume all the before the decimal point
            while (Current.IsDigit)
                charCache.Add(Consume());
            //Optionally consume the decimal point and the digits after it
            if (Current.IsCharacter('.'))
            {
                charCache.Add(Consume()); //Consume the decimal point
                //Consume the digits after it
                while (Current.IsDigit)
                    charCache.Add(Consume());
            }
            //Sanity check that we consumed anything
            if (charCache.Count == 0)
                throw new Exception(
                    $"[{nameof(TextParser<T>)}] Expected float but got '{Current}'");
            //Parse it to a float
            return float.Parse(new string(charCache.Data, startIndex: 0, length: charCache.Count));
        }

        protected int ConsumeInt()
        {
            charCache.Clear();
            //Optionally parse a negative sign
            if (Current.IsCharacter('-'))
                charCache.Add(Consume());
            //Parse all the digits
            while (Current.IsDigit)
                charCache.Add(Consume());
            //Sanity check that we consumed anything
            if (charCache.Count == 0)
                throw new Exception(
                    $"[{nameof(TextParser<T>)}] Expected int but got '{Current}'");
            //Parse it to an integer
            return int.Parse(new string(charCache.Data, startIndex: 0, length: charCache.Count));
        }

        public string ConsumeWord() => ConsumeUntil(current => current.IsWhitespace || current.IsEndOfLine);

        protected string ConsumeRestOfLine() => ConsumeUntil(current => current.IsEndOfLine);

        protected string ConsumeUntil(Predicate<Entry> endPredicate)
        {
            charCache.Clear();
            while (!Current.IsEndOfFile && !endPredicate(Current))
                charCache.Add(Consume());
            return new string(charCache.Data, startIndex: 0, length: charCache.Count);
        }

        protected void ConsumeWhitespace()
        {
            while (Current.IsWhitespace)
                Consume();
        }

        protected void ConsumeNewline()
        {
            TryConsume('\r'); //Optionally consume the windows carriage-return
            ExpectConsume('\n');
        }

        protected void ExpectConsumeWhitespace()
        {
            if (!Current.IsWhitespace)
                throw new Exception(
                    $"[{nameof(TextParser<T>)}] Expected whitespace but got '{Current}'");
            ConsumeWhitespace();
        }

        protected void ExpectConsume(char expectedChar)
        {
            char consumedChar = Consume();
            if (consumedChar != expectedChar)
                throw new Exception(
                    $"[{nameof(TextParser<T>)}] Expected '{expectedChar}' but got '{consumedChar}'");
        }

        protected void ExpectConsume(string expectedString)
        {
            for (int i = 0; i < expectedString.Length; i++)
            {
                char consumedChar = Consume();
                if (consumedChar != expectedString[i])
                    throw new Exception(
                        $"[{nameof(TextParser<T>)}] Did not find expected string '{expectedString}'");
            }
        }

        protected bool TryConsume(char expectedChar)
        {
            if (!Current.IsCharacter(expectedChar))
                return false;
            Consume();
            return true;
        }

        protected bool TryConsume(string expectedString)
        {
            for (int i = 0; i < expectedString.Length; i++)
            {
                if (!Current.IsCharacter(expectedString[i]))
                    return false;
                Consume();
            }
            return true;
        }

        protected char Consume()
        {
            int value = inputReader.Read();
            if (value < 0)
                throw new Exception($"[{nameof(TextParser<T>)}] End of file");
            return (char)value;
        }
    }
}