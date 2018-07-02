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
            FileStream stream = app.ReadFile(path);

            switch (extension)
            {
            case ".dae": case ".DAE":
                return new ColladaParser(stream, leaveStreamOpen: false);
            
            case ".obj": case ".OBJ":
                return new WavefrontObjParser(stream, leaveStreamOpen: false);

            case ".tga": case ".TGA":
                return new TruevisionTgaParser(stream, leaveStreamOpen: false);

            case ".spv": case ".SPV":
                return new SpirVParser(stream, leaveStreamOpen: false);
            }

            //No supported parser found
            stream.Dispose();
            throw new Exception(
                $"[{nameof(ResourceUtils)}] No parser known for extension: '{extension}'");
        }
    }
}