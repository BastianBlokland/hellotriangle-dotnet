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
        //Helper properties
        public long CurrentPosition => stream.Position - buffer.Length;

        //Data
        private readonly Stream stream;
        private readonly TextReader reader;
        private readonly int[] buffer;
        private int currentIndex;

        public BufferedTextReader(Stream stream, Encoding encoding, int readBufferSize = 2)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException(
                    $"[{nameof(BufferedTextReader)}] Only works on a readable stream", nameof(stream));

            this.stream = stream;
            reader = new StreamReader(stream, encoding);
            buffer = new int[readBufferSize];
            for (int i = 0; i < readBufferSize; i++)
                buffer[i] = reader.Read();
        }

        public void SeekForward(long position)
        {
            if (CurrentPosition == position) //Allready at correct entry
                return;
            if (position < CurrentPosition)
                throw new ArgumentOutOfRangeException(nameof(position), 
                    $"[{nameof(BufferedTextReader)}] Only forward seeking is allowed");
            
            if (stream.CanSeek) //If the stream supports seeking we can use that to 'skip' ahead
            {
                //Seek the stream to the specified point
                stream.Seek(position, SeekOrigin.Begin);
                //Refill our buffer
                currentIndex = 0;
                for (int i = 0; i < buffer.Length; i++)
                    buffer[i] = reader.Read();
            }
            else //If not we just read through it one by one
            {
                long readCount = position - CurrentPosition;
                for (long i = 0; i < readCount; i++)
                    Read();
            }
        }

        public int Peek(int charactersAhead = 0)
        {
            if (charactersAhead < 0 || charactersAhead > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(charactersAhead),
                    $"[{nameof(BufferedTextReader)}] Out of specified read-buffer size");
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