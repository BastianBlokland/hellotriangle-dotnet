using System;
using System.IO;

using HT.Engine.Parsing;
using HT.Engine.Platform;

namespace HT.Engine.Resources
{
    public static class ResourceUtils
    {
        public static IParser CreateParser(INativeApp app, string path)
        {
            string extension = Path.GetExtension(path);
            switch (extension)
            {
            case ".cube": case ".CUBE":
            {
                string leftPath = Path.ChangeExtension(path, ".left.tga");
                string rightPath = Path.ChangeExtension(path, ".right.tga");
                string upPath = Path.ChangeExtension(path, ".up.tga");
                string downPath = Path.ChangeExtension(path, ".down.tga");
                string frontPath = Path.ChangeExtension(path, ".front.tga");
                string backPath = Path.ChangeExtension(path, ".back.tga");
                return new CubeTextureParser(
                    CreateParser(app, leftPath) as ITextureParser, CreateParser(app, rightPath) as ITextureParser,
                    CreateParser(app, upPath) as ITextureParser, CreateParser(app, downPath) as ITextureParser,
                    CreateParser(app, frontPath) as ITextureParser, CreateParser(app, backPath) as ITextureParser);
            }
            case ".dae": case ".DAE":
                return new ColladaParser(app.ReadFile(path), leaveStreamOpen: false);
            
            case ".obj": case ".OBJ":
                return new WavefrontObjParser(app.ReadFile(path), leaveStreamOpen: false);

            case ".tga": case ".TGA":
                return new TruevisionTgaParser(app.ReadFile(path), leaveStreamOpen: false);

            case ".r32": case ".R32":
                return new R32Parser(app.ReadFile(path), leaveStreamOpen: false);

            case ".spv": case ".SPV":
                return new SpirVParser(app.ReadFile(path), leaveStreamOpen: false);
            }

            //No supported parser found
            throw new Exception(
                $"[{nameof(ResourceUtils)}] No parser known for extension: '{extension}'");
        }
    }
}