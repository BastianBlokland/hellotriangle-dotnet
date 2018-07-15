using System;

using HT.Engine.Utils;
using HT.Engine.Rendering.Memory;
using HT.Engine.Rendering;
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
                    srcBuffer: stagingBuffer.Buffer,
                    dstBuffer: vertexBuffer.Buffer,
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
                    srcBuffer: stagingBuffer.Buffer,
                    dstBuffer: indexBuffer.Buffer,
                    new BufferCopy(size: stagingSize, srcOffset: 0, dstOffset: 0));
            });
            return indexBuffer;
        }
    }
}