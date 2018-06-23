using System;
using System.IO;

using HT.Engine.Math;
using HT.Engine.Rendering.Model;
using HT.Engine.Utils;

namespace HT.Engine.Parsing
{
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

            public Float3 CalculateFaceNormal(ResizeArray<Float3> positions)
            {
                //This method is used as a fallback when the file does not contain vertex normals
                //Obj uses counter-clockwise vertex definition, so we can use that to assume the
                //face normal
                if (Elements.Length != 3)
                    throw new Exception(
                        $"[{nameof(WavefrontObjParser)}] Only works on triangles");
                //Positions of the vertices that make up this face
                var pos1 = positions.Data[Elements[0].PositionIndex];
                var pos2 = positions.Data[Elements[1].PositionIndex];
                var pos3 = positions.Data[Elements[2].PositionIndex];
                //Take cross product of two edges in this triangle
                var normal = Float3.Cross(pos2 - pos1, pos3 - pos1);
                return Float3.FastNormalize(normal);
            }

            public Face(FaceElement[] elements) => Elements = elements;
        }

        private readonly float scale;
        private readonly ResizeArray<Float3> positions = new ResizeArray<Float3>();
        private readonly ResizeArray<Float3> normals = new ResizeArray<Float3>();
        private readonly ResizeArray<Float2> texcoords = new ResizeArray<Float2>();
        private readonly ResizeArray<Face> faces = new ResizeArray<Face>();
        private readonly ResizeArray<FaceElement> elementCache = new ResizeArray<FaceElement>();

        public WavefrontObjParser(Stream inputStream, float scale = 1f) : base(inputStream) 
            => this.scale = scale;

        protected override void ConsumeToken()
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
                    //TODO: Support triangulation when there is more then 3
                    if (elementCache.Count != 3)
                        throw new Exception(
                            $"[{nameof(WavefrontObjParser)}] This parser only supports triangles");
                    faces.Add(new Face(elementCache.ToArray()));
                }
                break;
            }
            //Ignore everything we don't know about
            ConsumeRestOfLine();

            //End the token with a newline
            if (!Current.IsEndOfFile)
                ConsumeNewline();
        }

        protected override Mesh Construct()
        {
            MeshBuilder meshBuilder = new MeshBuilder();

            for (int i = 0; i < faces.Count; i++)
            for (int j = 0; j < faces.Data[i].Elements.Length; j++)
            {
                var element = faces.Data[i].Elements[j];
                Float3 pos = positions.Data[element.PositionIndex];
                Float3 normal;
                Float2 uv;
                //Get the vertex normal, if none provided calculate the face-normal
                //Obj makes no promises that normals are normalized so we need to normalize them
                if (element.NormalIndex != null)
                    normal = Float3.FastNormalize(normals.Data[element.NormalIndex.Value]);
                else
                    normal = faces.Data[i].CalculateFaceNormal(positions);
                //Get the vertex uv, if none provided default to 0,0
                if (element.TexcoordIndex != null)
                    uv = texcoords.Data[element.TexcoordIndex.Value];
                else
                    uv = Float2.Zero;

                meshBuilder.PushVertex(new Vertex(pos, normal, uv));
            }
            return meshBuilder.ToMesh();
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