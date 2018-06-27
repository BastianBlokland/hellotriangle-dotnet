using System;
using System.IO;
using System.Text;

namespace HT.Engine.Utils
{
    /// <summary>
    /// Text reader wrapper that reads a specified amount of characters ahead so you can Peek()
    /// further into the file. This is usefull for example file format where you need to make 
    /// decisions based on multiple characters into the future
    /// </summary>
    public sealed class BufferedTextReader : IDisposable
    {
        private readonly TextReader reader;
        private readonly int[] buffer;
        private int currentIndex;

        public BufferedTextReader(Stream stream, Encoding encoding, int readBufferSize = 2)
        {
            reader = new StreamReader(stream, encoding);
            buffer = new int[readBufferSize];
            for (int i = 0; i < readBufferSize; i++)
                buffer[i] = reader.Read();
        }

        public int Peek(int charactersAhead = 0)
        {
            if (charactersAhead < 0 || charactersAhead > buffer.Length)
                throw new ArgumentOutOfRangeException(
                    nameof(charactersAhead), "Out of specified read-buffer size");
            return buffer[(currentIndex + charactersAhead) % buffer.Length];
        }

        public int Read()
        {
            //Take the result we've read before for this entry
            int result = buffer[currentIndex];
            //Read a new character into the buffer and roll the buffer forward
            buffer[currentIndex] = reader.Read();
            currentIndex = (currentIndex + 1) % buffer.Length;
            return result;
        }

        public void Dispose() => reader.Dispose();
    }
}