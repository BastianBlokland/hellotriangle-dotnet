using System;
using System.IO;
using System.Text;

using HT.Engine.Math;

namespace HT.Engine.Parsing
{
    /// <summary>
    /// Implementation of a text-reader
    /// - Reads in blocks of 1024 bytes to reduce io pressure
    /// - Supports peeking ahead a set number of characters (can be usefull for some parsers)
    /// - Supports getting the 'current' byte position in the stream. 
    ///     NOTE: this is a virtual position as the actual stream position is further because of 
    ///     our buffered reading. So allways use this and not Stream.Position
    /// - Supports seeking forward to given byte position
    ///     NOTE: On streams that do not support seeking this is implemented by just reading until
    ///     we reach the desired point
    /// 
    /// Why was this not implemented using a StreamReader? Even tho stream-reader has buffered
    /// reading also it does not expose its internal read offset so we cannot determine the current
    /// byte-position which in turn does not allow us to seek back to a set point. You can get this
    /// data out by using reflection but that has high performance cost and garbage creation. Also
    /// StreamReader only allows peeking at the next character while this supports peeking a
    /// configurable set of characters into the future
    /// </summary>
    public class BufferedTextReader : IDisposable
    {
        private const int BYTE_BUFFER_SIZE = 1024;

        //Why is this in bytes and not in characters? Well position in bytes allows us to seek to
        //that position fast. This means you can save this byte offset and later call 'Seek' to 
        //get back to that point. Can be very usefull for certain parsers
        public long CurrentBytePosition 
            => (stream.Position - byteBufferSize) - 
                encoding.GetByteCount( //Substract the characters that where still left of the previous buffer
                    chars: charBuffer,
                    index: 0,
                    count: (charBufferStartOffset - currentCharIndex).ClampPositive()) +
                encoding.GetByteCount( //Add the characters that we've allready read from this buffer
                    chars: charBuffer, 
                    index: charBufferStartOffset, 
                    count: (currentCharIndex - charBufferStartOffset).ClampPositive());
        public bool CanSeekBackward => stream.CanSeek;

        //Data
        private readonly Stream stream;
        private readonly Encoding encoding;
        private readonly int maxPeekAhead;
        private readonly bool leaveStreamOpen;
        private readonly byte[] byteBuffer;
        private readonly char[] charBuffer;
        private int byteBufferSize;
        private int charBufferSize;
        private int charBufferStartOffset;
        private int currentCharIndex;

        public BufferedTextReader(
            Stream stream,
            Encoding encoding,
            int maxPeekAhead = 10,
            bool leaveStreamOpen = false)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (!stream.CanRead)
                throw new ArgumentException(
                    $"[{nameof(BufferedTextReader)}] Only works on a readable stream", nameof(stream));
            if (maxPeekAhead < 0)
                throw new ArgumentOutOfRangeException(nameof(maxPeekAhead));

            this.stream = stream;
            this.encoding = encoding;
            this.maxPeekAhead = maxPeekAhead;
            this.leaveStreamOpen = leaveStreamOpen;
            byteBuffer = new byte[BYTE_BUFFER_SIZE];
            //Why maxPeakAhead + 1? Because we allways read in BYTE_BUFFER_SIZE block BUT
            //we want to read a new block before completely 'exhausting' the previous block because
            //with Peek() you need to be able to look further. So when there is less then
            //1 + maxPeekAhead chars left in the buffer we start reading a new block. + 1 because 
            //maxPeekAhead of 0 still allows you to peek at the current
            charBuffer = new char[BYTE_BUFFER_SIZE + maxPeekAhead + 1];
            FillBuffer(charIndex: 0);     
        }

        public int Peek(int charactersAhead = 0)
        {
            if (charactersAhead < 0 || charactersAhead > maxPeekAhead)
                throw new ArgumentOutOfRangeException(nameof(charactersAhead),
                    $"[{nameof(BufferedTextReader)}] Out of specified peek-ahead size");
            int index = currentCharIndex + charactersAhead;
            if (index >= charBufferSize) //End of file
                return -1;
            return charBuffer[index];
        }

        public int Read()
        {
            //End of file
            if (currentCharIndex >= charBufferSize)
                return -1;
            int result = charBuffer[currentCharIndex++];

            //If there is not enough peek-ahead left then we read another block
            int charsLeft = charBufferSize - currentCharIndex;
            //Need atleast 1 and then more for peekahead.
            //Note we also check if the last buffer read was the not end of the file
            if (charsLeft <= (1 + maxPeekAhead) && byteBufferSize == BYTE_BUFFER_SIZE) 
            {
                //Copy the characters that are left to the beginning of the array
                Array.Copy(
                    sourceArray: charBuffer,
                    sourceIndex: currentCharIndex,
                    destinationArray: charBuffer,
                    destinationIndex: 0,
                    length: charsLeft);
                //After the character that are left we read a new block
                FillBuffer(charIndex: charsLeft);
                //reset our pointer to the beginning of the array
                currentCharIndex = 0;
            }

            return result;
        }

        public void SeekToBeginning() => Seek(bytePosition: 0);

        public void Seek(long bytePosition)
        {
            if (CurrentBytePosition == bytePosition) //Allready at correct entry
                return;
            
            if (stream.CanSeek) //If the stream supports seeking we can just skip to desired position
            {
                //Seek the stream to the specified point
                stream.Seek(bytePosition, SeekOrigin.Begin);
                FillBuffer(charIndex: 0);
                currentCharIndex = 0;
            }
            else
            {
                if (bytePosition < CurrentBytePosition)
                    throw new ArgumentOutOfRangeException(nameof(bytePosition), 
                        $"[{nameof(BufferedTextReader)}] Non-seekable streams can only go forward");
                while (CurrentBytePosition < bytePosition)
                {
                    if (Read() < 0)
                        throw new Exception(
                            $"[{nameof(BufferedTextReader)}] Unexpected end of file");
                }
                if (CurrentBytePosition != bytePosition)
                    throw new Exception(
                        $"[{nameof(BufferedTextReader)}] Could not seek to specified byte position, encoding mismatch?");
            }
        }

        public void Dispose()
        {
            if (!leaveStreamOpen)
                stream.Dispose();
        }

        private void FillBuffer(int charIndex)
        {
            //Read the raw bytes
            byteBufferSize = stream.Read(byteBuffer, offset: 0, count: byteBuffer.Length);
            //Decode the actual characters from those bytes
            //Note with none simple encodings (like unicode) there will be less chars then bytes
            charBufferSize = encoding.GetChars(
                bytes: byteBuffer,
                byteIndex: 0,
                byteCount: byteBufferSize,
                chars: charBuffer,
                charIndex: charIndex) + charIndex;
            //Keep track of where in the charBuffer our newly read chars begin, this is required for
            //the property that calculates our current offset in the stream, as the chars before
            //this where not read in this iteration so should not be part of the offset calculation
            charBufferStartOffset = charIndex;
        }
    }
}