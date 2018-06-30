using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using HT.Engine.Utils;

namespace HT.Engine.Parsing
{
    /// <summary>
    /// Utility for parsing a text stream
    /// </summary>
    public sealed class TextParser : IDisposable
    {
        public struct Entry
        {
            //Properties
            public bool IsEndOfFile => value < 0;
            public bool IsEndOfLine => IsCharacter('\r') || IsCharacter('\n');
            public bool IsSingleQuote => !IsEndOfFile && IsCharacter('\'');
            public bool IsDoubleQuote => !IsEndOfFile && IsCharacter('\"');
            public bool IsWhitespace => !IsEndOfFile && !IsEndOfLine && Char.IsWhiteSpace((char)value);
            public bool IsDigit => !IsEndOfFile && Char.IsDigit((char)value);

            //Data
            private readonly int value;

            public Entry(int value) => this.value = value;

            public bool IsCharacter(char character) => !IsEndOfFile && (char)value == character;

            public override string ToString()
            {
                if (IsEndOfFile) return "EOF";
                if (IsEndOfLine) return "EOL";
                if (IsWhitespace) return "SPACE";
                return ((char)value).ToString();
            }
        }

        //Properties
        public long CurrentBytePosition => inputReader.CurrentBytePosition;
        public Entry Current => new Entry(inputReader.Peek(charactersAhead: 0));
        public Entry Next => new Entry(inputReader.Peek(charactersAhead: 1));

        //Data
        private readonly BufferedTextReader inputReader;
        private readonly ResizeArray<char> charCache = new ResizeArray<char>();

        public TextParser(Stream inputStream, Encoding encoding, bool leaveStreamOpen)
            => inputReader = new BufferedTextReader(
                inputStream,
                encoding,
                maxPeekAhead: 2,
                leaveStreamOpen: leaveStreamOpen);

        public void SeekToBeginning() => inputReader.SeekToBeginning();
        public void Seek(long bytePosition) => inputReader.Seek(bytePosition);

        public float ConsumeFloat()
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
                throw CreateError($"Expected float but got '{Current}'");
            //Parse it to a float
            return float.Parse(new string(charCache.Data, startIndex: 0, length: charCache.Count));
        }

        public int ConsumeInt()
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
                throw CreateError($"Expected int but got '{Current}'");
            //Parse it to an integer
            return int.Parse(new string(charCache.Data, startIndex: 0, length: charCache.Count));
        }

        public string ConsumeQuotedString()
        {
            //Determine if this is a single or double quoted string
            bool isSingleQuote = Current.IsSingleQuote;
            bool isDoubleQuote = Current.IsDoubleQuote;
            if (!isSingleQuote && !isDoubleQuote)
                throw CreateError($"Expected single or double quote but got '{Current}'");
            //Consume the starting quote
            Consume();
            //Consume all until we find a matching end quote
            string content = ConsumeUntil(() =>
                isSingleQuote ? Current.IsSingleQuote : Current.IsDoubleQuote);
            //Consume the end quote
            Consume();
            return content;
        }

        public string ConsumeWord() => ConsumeUntil(() => Current.IsWhitespace || Current.IsEndOfLine);

        public string ConsumeRestOfLine() => ConsumeUntil(() => Current.IsEndOfLine);

        public string ConsumeUntil(Func<bool> endPredicate)
        {
            charCache.Clear();
            while (!Current.IsEndOfFile && !endPredicate())
                charCache.Add(Consume());
            return new string(charCache.Data, startIndex: 0, length: charCache.Count);
        }

        public void ConsumeIgnoreUntil(Func<bool> endPredicate)
        {
            while (!Current.IsEndOfFile && !endPredicate())
                Consume();
        }

        public void ConsumeWhitespace(bool includeNewline = false)
        {
            while (Current.IsWhitespace || (includeNewline && Current.IsEndOfLine))
                Consume();
        }

        public void ConsumeNewline()
        {
            TryConsume('\r'); //Optionally consume the windows carriage-return
            ExpectConsume('\n');
        }

        public void ExpectConsumeWhitespace()
        {
            if (!Current.IsWhitespace)
                throw CreateError($"Expected whitespace but got '{Current}'");
            ConsumeWhitespace();
        }

        public void ExpectConsume(char expectedChar, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                char consumedChar = Consume();
                if (consumedChar != expectedChar)
                    throw CreateError($"Expected '{expectedChar}' but got '{consumedChar}'");
            }
        }

        public void ExpectConsume(string expectedString)
        {
            for (int i = 0; i < expectedString.Length; i++)
            {
                char consumedChar = Consume();
                if (consumedChar != expectedString[i])
                    throw CreateError($"Did not find expected string '{expectedString}'");
            }
        }

        public bool TryConsume(char expectedChar)
        {
            if (!Current.IsCharacter(expectedChar))
                return false;
            Consume();
            return true;
        }

        public bool TryConsume(string expectedString)
        {
            for (int i = 0; i < expectedString.Length; i++)
            {
                if (!Current.IsCharacter(expectedString[i]))
                    return false;
                Consume();
            }
            return true;
        }

        public char Consume()
        {
            int value = inputReader.Read();
            if (value < 0)
                throw CreateError("Unexpected end of file");
            return (char)value;
        }

        public Exception CreateError(string errorMessage)
        {
            #if DEBUG
            //In debug seek back through the stream to gather what line number we where at,
            //why not just keep track of the lines while reading? Because someone can skip ahead
            //using the seek feature and thus the linenumber would not match 
            if (inputReader.CanSeekBackward)
            {
                long lineNumber = 1; //Start at 1 to match how most editors show linenumbers
                long charOnLine = 1; //Start at 1 to match how most editors show characters on line
                long curBytePos = inputReader.CurrentBytePosition;
                inputReader.SeekToBeginning();
                while (inputReader.CurrentBytePosition < curBytePos) //Read back to current 
                {
                    int val = inputReader.Read();
                    if (val == '\n')
                    {
                        lineNumber++;
                        charOnLine = 0;
                    }
                    charOnLine++;
                }
                return new Exception(
                    $"[{nameof(TextParser)}] {errorMessage} (Current: '{Current}', Line: {lineNumber}, CharacterOnLine: {charOnLine})");
            }
            #endif
            return new Exception($"[{nameof(TextParser)}] {errorMessage}");
        }

        public void Dispose() => inputReader.Dispose();
    }
}