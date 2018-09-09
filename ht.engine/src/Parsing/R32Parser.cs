using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using HT.Engine.Math;
using HT.Engine.Resources;
using HT.Engine.Utils;

namespace HT.Engine.Parsing
{
    //Parser for the Raw32 format, which is just a collection of 32bit floats without any meta-info
    //This format is for example used by WorldMachine.
    public sealed class R32Parser : ITextureParser, IParser<Float1Texture>, IParser
    {
        private readonly bool leaveStreamOpen;
        private readonly Stream inputStream;

        public R32Parser(Stream inputStream, bool leaveStreamOpen = false)
        {
            if (inputStream == null)
                throw new ArgumentNullException(nameof(inputStream));
            this.inputStream = inputStream;
            this.leaveStreamOpen = leaveStreamOpen;
        }

        public Float1Texture Parse()
        {
            if (inputStream.Length > int.MaxValue)
                throw new IOException(
                    $"[{nameof(R32Parser)}] Streams with more then {int.MaxValue} bytes of data are unsupported");

            int byteCount = (int)inputStream.Length;
            if (byteCount % sizeof(float) != 0)
                throw new Exception($"[[{nameof(R32Parser)}]] Data corrupt");
            
            //Allocate a array for the pixels;
            int pixelCount = byteCount / sizeof(float);
            float[] pixels = new float[pixelCount];

            //Read from the stream directly into the pixels array
            inputStream.ReadToEnd<float>(pixels);

            //Because the format contains no header we have no way of determining the dimensions if
            //we don't assume a power-of-two
            int? size = IntUtils.TryPerfectSquareRoot(pixelCount);
            if (size == null)
                throw new Exception($"[[{nameof(R32Parser)}]] Only power-of-two texture are supported");

            return new Float1Texture(pixels, (size.Value, size.Value));
        }

        public void Dispose()
        {
            if (!leaveStreamOpen)
                inputStream.Dispose();
        }

        ITexture ITextureParser.Parse() => Parse();
        object IParser.Parse() => Parse();
    }
}