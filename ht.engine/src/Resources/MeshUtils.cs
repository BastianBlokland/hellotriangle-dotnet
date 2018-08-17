using System;
using System.Collections.Generic;

using HT.Engine.Math;
using HT.Engine.Utils;

namespace HT.Engine.Resources
{
    public static class MeshUtils
    {
        public static Mesh CreateBox(FloatBox box, Float4 color)
        {
            //Get the 8 corners of this box
            Span<Float3> points = stackalloc Float3[8];
            box.GetPoints(points);

            //Get all the vertices that this box is made of 
            //(unique vertices per face because of unique normals)
            Vertex[] vertices = new Vertex[6 * 4];

            //Top vertices
            vertices[0] = new Vertex(points[0], color, Float3.Up, uv1: (0f, 0f));
            vertices[1] = new Vertex(points[1], color, Float3.Up, uv1: (1f, 0f));
            vertices[2] = new Vertex(points[2], color, Float3.Up, uv1: (0f, 1f));
            vertices[3] = new Vertex(points[3], color, Float3.Up, uv1: (1f, 1f));

            //Bottom vertices
            vertices[4] = new Vertex(points[4], color, Float3.Down, uv1: (0f, 0f));
            vertices[5] = new Vertex(points[5], color, Float3.Down, uv1: (1f, 0f));
            vertices[6] = new Vertex(points[6], color, Float3.Down, uv1: (0f, 1f));
            vertices[7] = new Vertex(points[7], color, Float3.Down, uv1: (1f, 1f));

            //Right vertices
            vertices[8] = new Vertex(points[3], color, Float3.Right, uv1: (0f, 0f));
            vertices[9] = new Vertex(points[2], color, Float3.Right, uv1: (1f, 0f));
            vertices[10] = new Vertex(points[6], color, Float3.Right, uv1: (0f, 1f));
            vertices[11] = new Vertex(points[5], color, Float3.Right, uv1: (1f, 1f));

            //Left vertices
            vertices[12] = new Vertex(points[7], color, Float3.Left, uv1: (0f, 0f));
            vertices[13] = new Vertex(points[1], color, Float3.Left, uv1: (1f, 0f));
            vertices[14] = new Vertex(points[0], color, Float3.Left, uv1: (0f, 1f));
            vertices[15] = new Vertex(points[4], color, Float3.Left, uv1: (1f, 1f));
            
            //Front vertices
            vertices[16] = new Vertex(points[2], color, Float3.Forward, uv1: (0f, 0f));
            vertices[17] = new Vertex(points[1], color, Float3.Forward, uv1: (1f, 0f));
            vertices[18] = new Vertex(points[7], color, Float3.Forward, uv1: (0f, 1f));
            vertices[19] = new Vertex(points[6], color, Float3.Forward, uv1: (1f, 1f));

            //Back vertices
            vertices[20] = new Vertex(points[4], color, Float3.Backward, uv1: (0f, 0f));
            vertices[21] = new Vertex(points[0], color, Float3.Backward, uv1: (1f, 0f));
            vertices[22] = new Vertex(points[3], color, Float3.Backward, uv1: (0f, 1f));
            vertices[23] = new Vertex(points[5], color, Float3.Backward, uv1: (1f, 1f));

            //Calculate indices using a strip approach
            UInt16[] indices = new UInt16[]
            {
                0,  1,  3,  2, Mesh.RESTART_INDEX, //Top
                4,  5,  7,  6, Mesh.RESTART_INDEX, //Bottom
                8,  9,  11, 10, Mesh.RESTART_INDEX, //Right
                12, 13, 15, 14, Mesh.RESTART_INDEX, //Left
                16, 17, 19, 18, Mesh.RESTART_INDEX, //Front
                20, 21, 23, 22, Mesh.RESTART_INDEX //Back
            };
            return new Mesh(vertices, indices, Mesh.TopologyType.TriangleStrip);
        }

        public static Mesh CreatePlane(int segments, float size)
        {
            //Create the vertices
            Vertex[] vertices = new Vertex[segments * segments];
            Float3 offset = (x: size * .5f, y: 0f, z: size * .5f);
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
            ResizeArray<UInt16> indices = new ResizeArray<UInt16>();
            for (int z = 1; z < segments; z++) //Start at 1 because we handle 2 'rows' in one iteration
            {
                //Strip row
                for (int x = 0; x < segments; x++)
                { 
                    indices.Add((UInt16)(x + segments * z));
                    indices.Add((UInt16)(x + segments * (z - 1)));
                }
                //Restart strip
                indices.Add(Mesh.RESTART_INDEX);
            }

            return new Mesh(vertices, indices.ToArray(), Mesh.TopologyType.TriangleStrip);
        }
    }
}