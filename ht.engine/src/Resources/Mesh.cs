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
        private readonly Vertex[] vertices;
        private readonly UInt16[] indices;
        private readonly TopologyType type;

        public Mesh(Vertex[] vertices, UInt16[] indices, TopologyType type = TopologyType.TriangleList)
        {
            if (vertices == null)
                throw new ArgumentNullException(nameof(vertices));
            if (indices == null)
                throw new ArgumentNullException(nameof(indices));
            this.vertices = vertices;
            this.indices = indices;
            this.type = type;
        }

        public void Scale(float scale)
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = new Vertex(
                    position: vertices[i].Position * scale,
                    color: vertices[i].Color,
                    normal: vertices[i].Normal,
                    uv1: vertices[i].Uv1,
                    uv2: vertices[i].Uv2);
        }

        internal DeviceBuffer UploadVertices(
            Device logicalDevice,
            Pool memoryPool,
            HostBuffer stagingBuffer,
            TransientExecutor executor)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (memoryPool == null)
                throw new ArgumentNullException(nameof(memoryPool));
            if (stagingBuffer == null)
                throw new ArgumentNullException(nameof(stagingBuffer));
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));

            DeviceBuffer vertexBuffer = new DeviceBuffer(
                logicalDevice: logicalDevice,
                memoryPool: memoryPool,
                size: vertices.GetSize(),
                usages: BufferUsages.VertexBuffer);

            //Write the data to the staging buffer
            int stagingSize = stagingBuffer.Write(vertices);

            //Copy the staging buffer to a device buffer
            executor.ExecuteBlocking(commandBuffer =>
            {
                commandBuffer.CmdCopyBuffer(
                    srcBuffer: stagingBuffer.VulkanBuffer,
                    dstBuffer: vertexBuffer.VulkanBuffer,
                    new BufferCopy(size: stagingSize, srcOffset: 0, dstOffset: 0));
            });
            return vertexBuffer;
        }

        internal DeviceBuffer UploadIndices(
            Device logicalDevice,
            Pool memoryPool,
            HostBuffer stagingBuffer,
            TransientExecutor executor)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (memoryPool == null)
                throw new ArgumentNullException(nameof(memoryPool));
            if (stagingBuffer == null)
                throw new ArgumentNullException(nameof(stagingBuffer));
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));

            DeviceBuffer indexBuffer = new DeviceBuffer(
                logicalDevice: logicalDevice,
                memoryPool: memoryPool,
                size: indices.GetSize(),
                usages: BufferUsages.IndexBuffer);
            
             //Write the data to the staging buffer
            int stagingSize = stagingBuffer.Write(indices);

            //Copy the staging buffer to a device buffer
            executor.ExecuteBlocking(commandBuffer =>
            {
                commandBuffer.CmdCopyBuffer(
                    srcBuffer: stagingBuffer.VulkanBuffer,
                    dstBuffer: indexBuffer.VulkanBuffer,
                    new BufferCopy(size: stagingSize, srcOffset: 0, dstOffset: 0));
            });
            return indexBuffer;
        }
    }
}