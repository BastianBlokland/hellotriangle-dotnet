using System;

using HT.Engine.Utils;
using HT.Engine.Rendering.Memory;
using VulkanCore;

namespace HT.Engine.Resources
{
    public sealed class Mesh
    {
        //Properties
        public int VertexCount => vertices.Length;
        public int IndexCount => indices.Length;

        //Data
        private readonly Vertex[] vertices;
        private readonly UInt16[] indices;

        public Mesh(Vertex[] vertices, UInt16[] indices)
        {
            if (vertices == null)
                throw new ArgumentNullException(nameof(vertices));
            if (indices == null)
                throw new ArgumentNullException(nameof(indices));
            this.vertices = vertices;
            this.indices = indices;
        }

        internal DeviceBuffer UploadVertices(
            Device logicalDevice, Pool memoryPool, StagingBuffer stagingBuffer)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (memoryPool == null)
                throw new ArgumentNullException(nameof(memoryPool));
            if (stagingBuffer == null)
                throw new ArgumentNullException(nameof(stagingBuffer));

            DeviceBuffer vertexBuffer = new DeviceBuffer(
                logicalDevice: logicalDevice,
                memoryPool: memoryPool,
                size: vertices.GetSize(),
                usages: BufferUsages.VertexBuffer);
            stagingBuffer.Upload(vertices, vertexBuffer);
            return vertexBuffer;
        }

        internal DeviceBuffer UploadIndices(
            Device logicalDevice, Pool memoryPool, StagingBuffer stagingBuffer)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (memoryPool == null)
                throw new ArgumentNullException(nameof(memoryPool));
            if (stagingBuffer == null)
                throw new ArgumentNullException(nameof(stagingBuffer));

            DeviceBuffer indexBuffer = new DeviceBuffer(
                logicalDevice: logicalDevice,
                memoryPool: memoryPool,
                size: indices.GetSize(),
                usages: BufferUsages.IndexBuffer);
            stagingBuffer.Upload(indices, indexBuffer);
            return indexBuffer;
        }
    }
}