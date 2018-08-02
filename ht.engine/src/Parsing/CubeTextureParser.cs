using System;
using System.IO;
using System.Runtime.InteropServices;

using HT.Engine.Math;
using HT.Engine.Resources;

namespace HT.Engine.Parsing
{
    public sealed class CubeTextureParser : IParser<CubeTexture>, IParser
    {
        private readonly ITextureParser leftParser;
        private readonly ITextureParser rightParser;
        private readonly ITextureParser upParser;
        private readonly ITextureParser downParser;
        private readonly ITextureParser frontParser;
        private readonly ITextureParser backParser;

        public CubeTextureParser(
            ITextureParser leftParser,
            ITextureParser rightParser,
            ITextureParser upParser,
            ITextureParser downParser,
            ITextureParser frontParser,
            ITextureParser backParser)
        {
            if (leftParser == null)
                throw new ArgumentNullException(nameof(leftParser));
            if (rightParser == null)
                throw new ArgumentNullException(nameof(rightParser));
            if (upParser == null)
                throw new ArgumentNullException(nameof(upParser));
            if (downParser == null)
                throw new ArgumentNullException(nameof(downParser));
            if (frontParser == null)
                throw new ArgumentNullException(nameof(frontParser));
            if (backParser == null)
                throw new ArgumentNullException(nameof(backParser));
            this.leftParser = leftParser;
            this.rightParser = rightParser;
            this.upParser = upParser;
            this.downParser = downParser;
            this.frontParser = frontParser;
            this.backParser = backParser;
        }

        public CubeTexture Parse()
        {
           ITexture leftTexture = leftParser.Parse();
           ITexture rightTexture = rightParser.Parse();
           ITexture upTexture = upParser.Parse();
           ITexture downTexture = downParser.Parse();
           ITexture frontTexture = frontParser.Parse();
           ITexture backTexture = backParser.Parse();
           return new CubeTexture(
               leftTexture,
               rightTexture,
               upTexture,
               downTexture,
               frontTexture,
               backTexture);
        }

        public void Dispose()
        {
            leftParser.Dispose();
            rightParser.Dispose();
            upParser.Dispose();
            downParser.Dispose();
            frontParser.Dispose();
            backParser.Dispose();
        }

        object IParser.Parse() => Parse();
    }
}