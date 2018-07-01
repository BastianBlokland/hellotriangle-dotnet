using System;
using System.IO;
using System.Runtime.InteropServices;

using HT.Engine.Math;
using HT.Engine.Rendering;

namespace HT.Engine.Parsing
{
    //Supports 24 (rgb) and 32 (rgba) bit tga and can be optionally rle compressed
    //Followed the spec from wikipedia: https://en.wikipedia.org/wiki/Truevision_TGA
    //About tga colors: http://www.ryanjuckett.com/programming/parsing-colors-in-a-tga-file/
    //About rle compression: https://en.wikipedia.org/wiki/Run-length_encoding
    public sealed class TruevisionTgaParser : IParser<Texture>
    {
        private enum ColorMapType : byte
        {
            NoColorMap = 0,
            HasColorMap = 1
        }

        private enum ImageType : byte
        {
            UncompressedTrueColorImage = 2,
            RunLengthEncodedTrueColorImage = 10
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 18)]
        private readonly struct Header
        {
            public readonly byte IdLength;
            public readonly ColorMapType ColorMapType;
            public readonly ImageType ImageType;
            public readonly UInt16 ColorMapOrigin;
            public readonly UInt16 ColorMapLength;
            public readonly byte ColorMapEntrySize;
            public readonly UInt16 XOrigin;
            public readonly UInt16 YOrigin;
            public readonly UInt16 ImageWidth;
            public readonly UInt16 ImageHeight;
            public readonly byte BitsPerPixel;
            public readonly byte ImageDescriptor;
        }

        private readonly BinaryParser par;
        private Header header;
        private Float4[] pixels;

        public TruevisionTgaParser(Stream inputStream, bool leaveStreamOpen = false)
        {
            if (inputStream == null)
                throw new ArgumentNullException(nameof(inputStream));
            par = new BinaryParser(inputStream, leaveStreamOpen);
        }

        public Texture Parse()
        {
            //Read the header
            header = par.Consume<Header>();
            //Sanity check some of the data in the header
            if (header.ColorMapType != ColorMapType.NoColorMap && header.ColorMapType != ColorMapType.HasColorMap)
                throw par.CreateError($"Unsupported colormap type: {header.ColorMapType}");
            if (header.BitsPerPixel != 24 && header.BitsPerPixel != 32)
                throw par.CreateError($"Only 24 (rgb) and 32 (rgba) bits per pixel are supported");
            //Check if this image is using the run-length-encoding compression
            bool rleCompressed = CheckCompression(header.ImageType);
            
            //Create array for the pixels
            pixels = new Float4[header.ImageWidth * header.ImageHeight];

            //Ignore the id field
            par.ConsumeIgnore(header.IdLength);
            //Ignore the colormap if there was any (we just want to read the raw colors)
            if (header.ColorMapType == ColorMapType.HasColorMap)
                par.ConsumeIgnore(header.ColorMapLength);

            //Read the color data
            for (int i = 0; i < pixels.Length; i++)
            {
                if (rleCompressed)
                {
                    byte header = par.Consume();
                    bool isRunLengthPacket = header.HasBitSet(7);
                    byte count = (byte)(header & ~(1 << 7)); //Interpret the first 7 bits as a count
                    
                    //If this is a runlength packet it means we repeat the pixel value
                    //as many times as set by count
                    if (isRunLengthPacket)
                    {
                        Float4 pixel = ConsumePixel();
                        for (int j = 0; j < count + 1; j++)
                            pixels[i + j] = pixel;
                    }
                    //If its not a runlength packet then its a 'raw' packet meaning just normal
                    //pixels for as many pixels as set by count
                    else
                    {
                        for (int j = 0; j < count + 1; j++)
                            pixels[i + j] = ConsumePixel();
                    }
                    i += count;
                }
                else
                    pixels[i] = ConsumePixel();
            }
            return new Texture(pixels, header.ImageWidth, header.ImageHeight);
        }

        public void Dispose() => par.Dispose();

        private Float4 ConsumePixel()
        {
            switch (header.BitsPerPixel)
            {
                case 24: //Stored as BGR and 1 byte per component (because little-endian)
                {
                    //Stored as RGB but we need to read as BGR because its in little-endian
                    Span<byte> data = stackalloc byte[3];
                    par.Consume(data);
                    return Float4.CreateFrom32Bit(data[2], data[1], data[0], 255);
                }
                case 32: //Stored as BGRA and 1 byte per component (because little-endian)
                {
                    Span<byte> data = stackalloc byte[4];
                    par.Consume(data);
                    return Float4.CreateFrom32Bit(data[2], data[1], data[0], data[3]);
                }
                default:
                    throw par.CreateError("Unsupported bitsPerPixel");
            }
        }

        private bool CheckCompression(ImageType type)
        {
            switch (type)
            {
                case ImageType.UncompressedTrueColorImage: return false;
                case ImageType.RunLengthEncodedTrueColorImage: return true;
                default:
                    throw par.CreateError($"Unsupported image-type: {header.ImageType}");
            }
        }
    }
}