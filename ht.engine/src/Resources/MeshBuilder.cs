using System;
using System.Collections.Generic;

using HT.Engine.Utils;

namespace HT.Engine.Resources
{
    public sealed class MeshBuilder
    {
        private readonly ResizeArray<Vertex> vertices = new ResizeArray<Vertex>();
        private readonly ResizeArray<UInt16> indices = new ResizeArray<UInt16>();

        public void PushVertex(Vertex vertex)
        {
            //Filter out similar vertices to reduce vertex count
            int index = FindSimilar(vertex);
            if (index < 0)
            {
                if (vertices.Count >= UInt16.MaxValue)
                    throw new Exception(
                        $"[{nameof(MeshBuilder)}] Only '{UInt16.MaxValue}' vertices are supported");
                index = vertices.Count;
                vertices.Add(vertex);
            }
            indices.Add((UInt16)index);
        }

        public Mesh ToMesh() => new Mesh(vertices.ToArray(), indices.ToArray());

        private int FindSimilar(Vertex vertex)
        {
            for (int i = 0; i < vertices.Count; i++)
                if (vertices.Data[i].Approx(vertex))
                    return i;
            return -1;
        }
    }
}