using System;
using System.Collections.Generic;

using HT.Engine.Math;
using HT.Engine.Utils;

namespace HT.Engine.Resources
{
    public static class MeshUtils
    {
        public static Mesh CreatePlane(int segments, float size)
        {
            //Create the vertices
            Vertex[] vertices = new Vertex[segments * segments];
            Float3 offset = (x: (segments - 1f) / 2f, y: 0f, z: (segments - 1f) / 2f);
            for (int x = 0; x < segments; x++)
            for (int z = 0; z < segments; z++)
            {
                float xProg = (float)x / (segments - 1);
                float zProg = (float)z / (segments - 1);
                vertices[x + z * segments] = new Vertex(
                    position: (xProg * size, 0f, zProg * size) - offset,
                    color: Float4.One,
                    normal: Float3.Up,
                    uv1: new Float2(xProg, zProg),
                    uv2: new Float2(xProg, zProg));
            } 

            //Create the indices
            ResizeArray<UInt16> indexList = new ResizeArray<UInt16>();
            for (int z = 1; z < segments; z++) //Start at 1 because we handle 2 'rows' in one iteration
            {
                //Strip row
                for (int x = 0; x < segments; x++)
                { 
                    indexList.Add((UInt16)(x + segments * z));
                    indexList.Add((UInt16)(x + segments * (z - 1)));
                }
                //Restart strip
                indexList.Add(Mesh.RESTART_INDEX);
            }

            return new Mesh(vertices, indexList.ToArray(), Mesh.TopologyType.TriangleStrip);
        }
    }
}