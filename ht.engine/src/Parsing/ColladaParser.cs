using System;
using System.IO;
using System.Text;

using HT.Engine.Math;
using HT.Engine.Rendering.Model;
using HT.Engine.Utils;

using static System.Math;

namespace HT.Engine.Parsing
{
    /// <summary>
    /// Collada 1.4 parser
    /// Specifications: https://www.khronos.org/files/collada_spec_1_4.pdf
    /// 
    /// This uses a multipass approach, first it parses the xml structure of the file and takes note
    /// of the positions of the data in the xml file. Then it looks in the xml structure for the
    /// data we are interested in the and then we read that data from the stream.
    /// </summary>
    public sealed class ColladaParser : IParser<Mesh>
    {
        private readonly struct Input
        {
            public readonly string Semantic;
            public readonly int Offset;
            public readonly string Source;

            public Input(string semantic, int offset, string source)
            {
                Semantic = semantic;
                Offset = offset;
                Source = source;
            }
        }

        private readonly float scale;
        private readonly TextParser par;
        private readonly XmlElement colladaElement;

        //Triangle data
        private readonly ResizeArray<Input> inputs = new ResizeArray<Input>();
        private readonly ResizeArray<int> indices = new ResizeArray<int>();
        private int triangleCount;
        private int inputStride;

        //Vertex data
        private readonly ResizeArray<Float3> positions = new ResizeArray<Float3>();
        private readonly ResizeArray<Float4> colors = new ResizeArray<Float4>();
        private readonly ResizeArray<Float3> normals = new ResizeArray<Float3>();
        private readonly ResizeArray<Float2> texcoords1 = new ResizeArray<Float2>();
        private readonly ResizeArray<Float2> texcoords2 = new ResizeArray<Float2>();

        public ColladaParser(Stream inputStream, float scale, bool leaveStreamOpen = false)
        {
            if (inputStream == null)
                throw new ArgumentNullException(nameof(inputStream));
            //We need support for seekable streams because we use a multi-pass approach
            if (!inputStream.CanSeek)
                throw new Exception($"[{nameof(ColladaParser)}] Only works on seekable streams");
            this.scale = scale;
            
            //Parse xml structure
            XmlDocument document;
            using (XmlParser parser = new XmlParser(inputStream, leaveStreamOpen: true))
                document = parser.Parse();

            //Verify root elements
            if (document.RootElementCount != 2 || 
                !document.Elements[0].HasName("xml") ||
                !document.Elements[1].HasName("COLLADA"))
            {
                throw new Exception(
                    $"[{nameof(ColladaParser)}] Invalid document root, expected 'xml' and 'COLLADA'");
            }
            colladaElement = document.Elements[1];

            //Verify version
            string colladaVersion = colladaElement.Tag.GetAttributeValue("version");
            if (string.IsNullOrEmpty(colladaVersion) || !colladaVersion.StartsWith("1.4"))
                throw new Exception(
                    $"[{nameof(ColladaParser)}] Unsupported collada version '{colladaVersion}'");

            //Seek the stream back to the beginning and create a parser for getting data out of it
            inputStream.Seek(offset: 0, origin: SeekOrigin.Begin);
            par = new TextParser(inputStream, Encoding.UTF8, leaveStreamOpen);
        }

        public Mesh Parse()
        {
            XmlElement geometriesElement = colladaElement.GetChild("library_geometries");
            if (geometriesElement == null)
                throw new Exception($"[{nameof(ColladaParser)}] No 'library_geometries' element found");

            //At the moment we only parse the first mesh out of the file, we can expand this later
            //to load a set of meshes out of the file
            XmlElement geometryElement = geometriesElement.GetChild(index: 0);
            if (geometryElement == null)
                throw new Exception($"[{nameof(ColladaParser)}] No geometry found");
            return ParseGeometry(geometryElement);
        }

        public void Dispose() => par.Dispose();

        private Mesh ParseGeometry(XmlElement geometryElement)
        {
            XmlElement meshElement = geometryElement.GetChild("mesh");
            if (meshElement == null)
                throw new Exception($"[{nameof(ColladaParser)}] Mesh element missing");

            //Parse the triangles
            ParseTriangles(meshElement);

            //Parse input data
            for (int i = 0; i < inputs.Count; i++)
            {
                Input input = inputs.Data[i];
                XmlElement dataElement = meshElement.GetChildWithAttribute(
                    name: "source",
                    attributeName: "id",
                    attributeValue: input.Source);
                switch (input.Semantic)
                {
                case "POSITION": ParseFloatSetArray(dataElement, positions); break;
                case "COLOR": ParseFloatSetArray(dataElement, colors); break;
                case "NORMAL": ParseFloatSetArray(dataElement, normals); break;
                case "VERTEX_TEXCOORD": ParseFloatSetArray(dataElement, texcoords1); break;
                case "TEXCOORD": ParseFloatSetArray(dataElement, texcoords2); break;
                }
            }
            
            //Build a mesh from the parsed data
            MeshBuilder meshBuilder = new MeshBuilder();
            for (int i = 0; i < triangleCount; i++)
            {
                //Surface normal is used when a vertex doesn't specify explict normals
                Float3 surfaceNormal = Triangle.GetNormal(
                    GetPosition(i, vertexIndex: 2),
                    GetPosition(i, vertexIndex: 1),
                    GetPosition(i, vertexIndex: 0));

                //In reverse as collada uses counter-clockwise triangles and we use clockwise
                for (int j = 3 - 1; j >= 0 ; j--)
                {
                    Float3 position = GetPosition(i, vertexIndex: j);
                    
                    int colorIndex = GetIndex(i, vertexIndex: j, semantic: "COLOR");
                    Float4 color = colorIndex < 0 ? Float4.One : colors.Data[colorIndex];

                    int normalIndex = GetIndex(i, vertexIndex: j, semantic: "NORMAL");
                    Float3 normal = normalIndex < 0 ? surfaceNormal : Float3.FastNormalize(normals.Data[normalIndex]);

                    int texcoord1Index = GetIndex(i, vertexIndex: j, semantic: "VERTEX_TEXCOORD");
                    Float2 texcoord1 = texcoord1Index < 0 ? Float2.Zero : texcoords1.Data[texcoord1Index];

                    int texcoord2Index = GetIndex(i, vertexIndex: j, semantic: "TEXCOORD");
                    Float2 texcoord2 = texcoord2Index < 0 ? Float2.Zero : texcoords2.Data[texcoord2Index];

                    //If we have a texcoord2 but no texcoord1 then we flip them
                    if (texcoord1Index < 0 && texcoord2Index >= 0)
                    {
                        texcoord1 = texcoord2;
                        texcoord2 = Float2.Zero;
                    }

                    meshBuilder.PushVertex(new Vertex(
                        position: position,
                        color: color,
                        normal: normal,
                        uv1: texcoord1,
                        uv2: texcoord2));
                }
            }
            return meshBuilder.ToMesh();

            Float3 GetPosition(int triangleIndex, int vertexIndex)
            {
                int index = GetIndex(triangleIndex, vertexIndex, semantic: "POSITION");
                if (index < 0)
                    throw new Exception(
                        $"[{nameof(ColladaParser)}] No position data found for: triangle: {triangleIndex}, vertex: {vertexIndex}");
                return positions.Data[index] * scale;
            }

            int GetIndex(int triangleIndex, int vertexIndex, string semantic)
            {
                int offset = GetOffset(semantic);
                if (offset < 0)
                    return -1;
                int triangleStartOffset = triangleIndex * inputStride * 3;
                int vertexStartOffset = vertexIndex * inputStride; 
                return indices.Data[triangleStartOffset + vertexStartOffset + offset];
            }

            int GetOffset(string semantic)
            {
                for (int i = 0; i < inputs.Count; i++)
                    if (inputs.Data[i].Semantic == semantic)
                        return inputs.Data[i].Offset;
                return -1;
            }
        }

        private void ParseTriangles(XmlElement meshElement)
        {
            XmlElement trianglesElement = meshElement.GetChild("triangles");
            XmlElement polygonsElement = trianglesElement ?? meshElement.GetChild("polygons");
            if (polygonsElement == null || !polygonsElement.HasChildren)
                throw new Exception($"[{nameof(ColladaParser)}] Triangles / Polygons element missing");

            //Parse all the triangles data
            triangleCount = int.Parse(polygonsElement.Tag.GetAttributeValue("count"));
            for (int i = 0; i < polygonsElement.Children.Count; i++)
                ParseTriangleElement(polygonsElement.Children[i]);
            if (indices.Count != triangleCount * 3 * inputStride)
                throw new Exception($"[{nameof(ColladaParser)}] Incorrect indices count found");

            //The triangle input can contain a VERTEX element with more data, we want to resolve
            //that here so we end of with just a flat list of attributes
            for (int i = 0; i < inputs.Count; i++)
            {
                Input input = inputs.Data[i];
                if (input.Semantic == "VERTEX")
                {
                    XmlElement vertexElement = meshElement.GetChildWithAttribute(
                        name: "vertices",
                        attributeName: "id",
                        attributeValue: input.Source);
                    for (int j = 0; j < vertexElement.Children.Count; j++)
                    {
                        XmlElement vertexInput = vertexElement.Children[j];
                        if (vertexInput.HasName("input"))
                        {
                            string semantic = vertexInput.Tag.GetAttributeValue("semantic");
                            //If there allready was a semantic with the same name then we prefix it with 'VERTEX'
                            if (HasSemantic(semantic))
                                semantic = $"VERTEX_{semantic}";

                            string sourceReference = GetSourceReference(vertexInput);
                            inputs.Add(new Input(semantic, input.Offset, sourceReference));
                        }
                    }
                }
            }
            
            bool HasSemantic(string semantic)
            {
                for (int i = 0; i < inputs.Count; i++)
                    if (inputs.Data[i].Semantic == semantic)
                        return true;
                return false;
            }

            void ParseTriangleElement(XmlElement triangleElement)
            {
                //The input elements contain info about what is in the indices
                if (triangleElement.HasName("input"))
                {
                    string semantic = triangleElement.Tag.GetAttributeValue("semantic");
                    int offset = int.Parse(triangleElement.Tag.GetAttributeValue("offset"));
                    string sourceReference = GetSourceReference(triangleElement);
                    inputStride = Max(inputStride, offset + 1);
                    inputs.Add(new Input(semantic, offset, sourceReference));
                }

                // The 'p' (primitive) element contains the actual indices
                if (triangleElement.HasName("p"))
                {
                    XmlDataEntry? dataEntry = triangleElement.FirstData;
                    if (dataEntry == null)
                        throw new Exception($"[{nameof(ColladaParser)}] Data is missing");
                    
                    //Read the indices
                    par.Seek(dataEntry.Value.StartBytePosition);
                    while (par.CurrentBytePosition < dataEntry.Value.EndBytePosition)
                    {
                        par.ConsumeWhitespace(includeNewline: true);
                        indices.Add(par.ConsumeInt());
                    }
                }
            }
        }

        private string GetSourceReference(XmlElement element)
        {
            string source = element.Tag.GetAttributeValue("source");
            if (source == null || !source.StartsWith('#'))
                throw new Exception($"[{nameof(ColladaParser)}] Incorrect source format");
            source = source.Substring(startIndex: 1);
            return source;
        }

        private void ParseFloatSetArray<T>(XmlElement element, ResizeArray<T> output)
            where T : struct, IFloatSet
        {
            output.Clear();

            //Get meta data
            XmlElement accessorElement = element.GetChild("technique_common")?.GetChild("accessor");
            if (accessorElement == null)
                throw new Exception($"[{nameof(ColladaParser)}] 'accessor' element");
            int count = int.Parse(accessorElement.Tag.GetAttributeValue("count"));
            int stride = int.Parse(accessorElement.Tag.GetAttributeValue("stride"));

            //Get raw data
            XmlElement arrayElement = element.GetChild("float_array");
            if (arrayElement == null)
                throw new Exception($"[{nameof(ColladaParser)}] 'float_array' missing");
            
            XmlDataEntry? dataEntry = arrayElement.FirstData;
            if (dataEntry == null)
                throw new Exception($"[{nameof(ColladaParser)}] Data is missing");

            int componentCount = FloatSetUtils.GetComponentCount<T>();
            if (componentCount != stride)
                throw new Exception(
                    $"[{nameof(ColladaParser)}] Incorrect float component count, expected: {componentCount}, got: {stride}");

            par.Seek(dataEntry.Value.StartBytePosition);
            for (int i = 0; i < count; i++)
            {
                output.Add(par.ConsumeFloatSet<T>());
                par.ConsumeWhitespace(includeNewline: true);
            }
            if (par.CurrentBytePosition > dataEntry.Value.EndBytePosition)
                throw new Exception($"[{nameof(ColladaParser)}] Data was bigger then expected");
        }
    }
}