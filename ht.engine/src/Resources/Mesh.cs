using System;

using HT.Engine.Utils;
using HT.Engine.Rendering.Memory;
using HT.Engine.Rendering;
using VulkanCore;

namespace HT.Engine.Resources
{
    public sealed class Mesh
    {
        public const UInt16 RESTART_INDEX = 0xFFFF;

        public enum TopologyType
        {
            TriangleList,
            TriangleStrip
        }

        //Public properties
        public int VertexCount => vertices.Length;
        public int IndexCount => indices.Length;

        //Internal properties
        internal PrimitiveTopology Topology
        {
            get
            {
                switch (type)
                {
                    case TopologyType.TriangleStrip:
                        return PrimitiveTopology.TriangleStrip;
                    case TopologyType.TriangleList:
                    default:
                        return PrimitiveTopology.TriangleList;
                }
            }
        }
        internal bool AllowRestart
        {
            get
            {
                switch (type)
                {
                    case TopologyType.TriangleStrip: return true;
                    case TopologyType.TriangleList:
                    default: return false;
                }
            }
            
        }

        //Data
        private readonly Memory<Vertex> vertices;
        private readonly Memory<UInt16> indices;
        private readonly TopologyType type;

        public Mesh(
            Memory<Vertex> vertices,
            Memory<UInt16> indices,
            TopologyType type = TopologyType.TriangleList)
        {
            if (vertices.Length == 0)
                throw new ArgumentException($"[{nameof(Mesh)}] No vertices provided", nameof(vertices));
            if (indices.Length == 0)
                throw new ArgumentException($"[{nameof(Mesh)}] No indices provided", nameof(indices));

            this.vertices = vertices;
            this.indices = indices;
            this.type = type;
        }

        public void Scale(float scale)
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices.Span[i] = new Vertex(
                    position: vertices.Span[i].Position * scale,
                    color: vertices.Span[i].Color,
                    normal: vertices.Span[i].Normal,
                    uv1: vertices.Span[i].Uv1,
                    uv2: vertices.Span[i].Uv2);
        }

        internal DeviceBuffer UploadVertices(RenderScene scene)
        {
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));
           return DeviceBuffer.UploadData<Vertex>(vertices, scene, BufferUsages.VertexBuffer);
        }

        internal DeviceBuffer UploadIndices(RenderScene scene)
        {
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));
            return DeviceBuffer.UploadData<UInt16>(indices, scene, BufferUsages.IndexBuffer);
        }
    }
}