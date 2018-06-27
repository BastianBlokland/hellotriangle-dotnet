using System;
using System.IO;
using System.Text;

using HT.Engine.Math;
using HT.Engine.Rendering.Model;
using HT.Engine.Utils;

namespace HT.Engine.Parsing
{
    //Supports vertex position, normal and texcoords and supports simple convex faces,
    //non triangle faces will be converted to triangles using a simple triangle fan starting from
    //vertex 0 in the face
    //Followed the spec from wikipedia: https://en.wikipedia.org/wiki/Wavefront_.obj_file
    public sealed class WavefrontObjParser : TextParser<Mesh>
    {
        private struct FaceElement
        {
            public readonly int PositionIndex;
            public readonly int? TexcoordIndex;
            public readonly int? NormalIndex;

            public FaceElement(int positionIndex, int? texcoordIndex, int? normalIndex)
            {
                PositionIndex = positionIndex;
                TexcoordIndex = texcoordIndex;
                NormalIndex = normalIndex;
            }
        }

        private struct Face
        {
            public readonly FaceElement[] Elements;

            public Face(FaceElement[] elements) => Elements = elements;
        }

        private readonly float scale;
        private readonly ResizeArray<Float3> positions = new ResizeArray<Float3>();
        private readonly ResizeArray<Float3> normals = new ResizeArray<Float3>();
        private readonly ResizeArray<Float2> texcoords = new ResizeArray<Float2>();
        private readonly ResizeArray<Face> faces = new ResizeArray<Face>();
        private readonly ResizeArray<FaceElement> elementCache = new ResizeArray<FaceElement>();

        public WavefrontObjParser(Stream inputStream, float scale = 1f) 
            : base(inputStream, Encoding.ASCII) 
            => this.scale = scale;

        protected override bool ConsumeToken()
        {
            ConsumeWhitespace(); //Ignore whitespace before the id
            string id = ConsumeWord();
            ConsumeWhitespace(); //Ignore whitespace after the id
            switch (id)
            {
                case "v": positions.Add(ConsumeFloat3() * scale); break;
                case "vn": normals.Add(ConsumeFloat3()); break;
                case "vt": texcoords.Add(ConsumeFloat2()); break;
                case "f":
                {
                    elementCache.Clear();
                    while (!Current.IsEndOfLine)
                    {
                        elementCache.Add(ConsumeFaceElement());
                        ConsumeWhitespace();
                    }
                    if (elementCache.Count < 3)
                        throw CreateError("At least 3 vertices are required to create a face");
                    faces.Add(new Face(elementCache.ToArray()));
                }
                break;
            }
            //Ignore everything we don't know about
            ConsumeRestOfLine();

            //End the token with a newline
            if (!Current.IsEndOfFile)
                ConsumeNewline();

            //true means keep parsing (we want to read tokens till the end of the file)
            return true; 
        }

        protected override Mesh Construct()
        {
            MeshBuilder meshBuilder = new MeshBuilder();
            for (int i = 0; i < faces.Count; i++)
            {
                var face = faces.Data[i];
                if (face.Elements.Length < 3)
                    throw CreateError("Need at least 3 vertices to form a triangle");

                //Create a simple triangle fan for each face.
                //Note: this only supports ordered simple convex polygons
                FaceElement a = face.Elements[0]; //Pivot point for the fan
                for (int j = 2; j < face.Elements.Length; j++)
                {
                    FaceElement b = face.Elements[j - 1];
                    FaceElement c = face.Elements[j];

                    //Surface normal is used when a vertex doesn't specify explict normals
                    Float3 surfaceNormal = Triangle.GetNormal(
                        a: GetPosition(a),
                        b: GetPosition(b),
                        c: GetPosition(c));

                    //Add the triangle to the meshbuilder
                    //Note: Adding the triangle in reverse because obj has counter-clockwise
                    //triangle order by default and we want a clockwise order
                    meshBuilder.PushVertex(GetVertex(c, surfaceNormal));
                    meshBuilder.PushVertex(GetVertex(b, surfaceNormal));
                    meshBuilder.PushVertex(GetVertex(a, surfaceNormal));
                }
            }
            return meshBuilder.ToMesh();

            //Helper functions
            Float3 GetPosition(FaceElement element) => positions.Data[element.PositionIndex];

            Vertex GetVertex(FaceElement element, Float3 surfaceNormal)
            {
                Float3 pos = GetPosition(element);
                Float3 normal = surfaceNormal;
                Float2 uv = Float2.Zero;
                //Obj makes no promises that normals are normalized so we need to normalize them
                if (element.NormalIndex != null)
                    normal = Float3.FastNormalize(normals.Data[element.NormalIndex.Value]);
                if (element.TexcoordIndex != null)
                    uv = texcoords.Data[element.TexcoordIndex.Value];
                return new Vertex(pos, normal, uv);
            }
        }

        private FaceElement ConsumeFaceElement()
        {
            //Note: The minus 1, that is done on the indices is because obj uses 1 as the starting index
            int positionIndex;
            int? texcoordIndex = null;
            int? normalIndex = null;
            TryConsume('v'); //Optionally start with v
            positionIndex = ConsumeInt() - 1;
            if (TryConsume('/'))
            {
                TryConsume("vt"); //Optionally start with vt
                if (Current.IsDigit) //Check here because its allowed to omit the texcoord
                    texcoordIndex = ConsumeInt() - 1;
            }
            if (TryConsume('/'))
            {
                TryConsume("vn"); //Optionally start with vn
                normalIndex = ConsumeInt() - 1;
            }
            return new FaceElement(positionIndex, texcoordIndex, normalIndex);
        }

        private Float3 ConsumeFloat3()
        {
            float x = ConsumeFloat();
            ExpectConsumeWhitespace();
            float y = ConsumeFloat();
            ExpectConsumeWhitespace();
            float z = ConsumeFloat();
            return new Float3(x, y, z);
        }

        private Float2 ConsumeFloat2()
        {
            float x = ConsumeFloat();
            ExpectConsumeWhitespace();
            float y = ConsumeFloat();
            return new Float2(x, y);
        }
    }
}