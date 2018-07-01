using System;
using System.IO;
using System.Text;
using HT.Engine.Math;
using HT.Engine.Rendering.Model;
using HT.Engine.Utils;

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

        //Vertex data
        private readonly ResizeArray<Float3> positions = new ResizeArray<Float3>();
        private readonly ResizeArray<Float3> normals = new ResizeArray<Float3>();
        private readonly ResizeArray<Float2> texcoords = new ResizeArray<Float2>();
        private string positionSemantic, normalSemantic, texcoordSemantic;

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
            XmlElement trianglesElement = meshElement.GetChild("triangles");
            if (trianglesElement == null || !trianglesElement.HasChildren)
                throw new Exception($"[{nameof(ColladaParser)}] Triangles element missing / incorrect");
            ParseTriangles(trianglesElement);

            //Parse vertex data
            XmlElement verticesElement = meshElement.GetChild("vertices");
            if (verticesElement == null || !verticesElement.HasChildren)
                throw new Exception($"[{nameof(ColladaParser)}] Vertices element missing / incorrect");
            for (int i = 0; i < verticesElement.Children.Count; i++)
            {
                XmlElement childElement = verticesElement.Children[i];
                if (childElement.HasName("input"))
                {
                    //Find the source data
                    string source = childElement.Tag.GetAttributeValue("source");
                    if (!source.StartsWith('#'))
                        throw new Exception($"[{nameof(ColladaParser)}] Incorrect source format");
                    source = source.Substring(startIndex: 1);
                    XmlElement dataElement = meshElement.GetChildWithAttribute("source", "id", source);
                    if (dataElement == null)
                        throw new Exception($"[{nameof(ColladaParser)}] Source not found");

                    string semantic = childElement.Tag.GetAttributeValue("semantic");
                    switch (semantic)
                    {
                    case "POSITION":
                        ParseFloatSetArray(dataElement, positions); 
                        positionSemantic = "VERTEX";
                        break;
                    case "NORMAL":
                        ParseFloatSetArray(dataElement, normals); 
                        normalSemantic = "VERTEX";
                        break;
                    case "TEXCOORD":
                        ParseFloatSetArray(dataElement, texcoords);
                        texcoordSemantic = "VERTEX";
                        break;
                    }
                }
            }
            
            //Build a mesh from the parsed data
            MeshBuilder meshBuilder = new MeshBuilder();
            for (int i = 0; i < triangleCount; i++)
            {
                //In reverse as collada uses counter-clockwise triangles and we use clockwise
                for (int j = 3 - 1; j >= 0 ; j--)
                {
                    int positionIndex = GetIndex(i, vertexIndex: j, semantic: positionSemantic);
                    Float3 position = positionIndex >= 0 ? positions.Data[positionIndex] * scale : Float3.Zero;
                    int normalIndex = GetIndex(i, vertexIndex: j, semantic: normalSemantic);
                    Float3 normal = normalIndex >= 0 ? normals.Data[normalIndex] : Float3.Zero;
                    int texcoordIndex = GetIndex(i, vertexIndex: j, semantic: texcoordSemantic);
                    Float2 texcoord = texcoordIndex >= 0 ? texcoords.Data[texcoordIndex] : Float2.Zero;
                    meshBuilder.PushVertex(new Vertex(position, normal, texcoord));
                }
            }
            return meshBuilder.ToMesh();

            int GetIndex(int triangleIndex, int vertexIndex, string semantic)
            {
                int offset = GetOffset(semantic);
                if (offset < 0)
                    return -1;
                int triangleStartOffset = triangleIndex * inputs.Count * 3;
                int vertexStartOffset = vertexIndex * inputs.Count; 
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

        private void ParseTriangles(XmlElement trianglesElement)
        {
            triangleCount = int.Parse(trianglesElement.Tag.GetAttributeValue("count"));
            for (int i = 0; i < trianglesElement.Children.Count; i++)
            {
                XmlElement childElement = trianglesElement.Children[i];
                if (childElement.HasName("input"))
                {
                    string semantic = childElement.Tag.GetAttributeValue("semantic");
                    int offset = int.Parse(childElement.Tag.GetAttributeValue("offset"));
                    string source = childElement.Tag.GetAttributeValue("source");
                    inputs.Add(new Input(semantic, offset, source));
                }
                if (childElement.HasName("p"))
                {
                    XmlDataEntry? dataEntry = childElement.FirstData;
                    if (dataEntry == null)
                        throw new Exception($"[{nameof(ColladaParser)}] Data is missing");
                    
                    //Read the indices
                    par.Seek(dataEntry.Value.StartBytePosition);
                    while (par.CurrentBytePosition < dataEntry.Value.EndBytePosition)
                    {
                        par.ConsumeWhitespace(includeNewline: true);
                        indices.Add(par.ConsumeInt());
                    }
                    if (indices.Count != triangleCount * 3 * inputs.Count)
                        throw new Exception($"[{nameof(ColladaParser)}] Incorrect indices count found");
                }
            }
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