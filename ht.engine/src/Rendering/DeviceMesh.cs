using System;

using HT.Engine.Resources;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering
{
    //GPU representation of a mesh.
    //NOTE: Does not hold on to the cpu representation of the mesh so it can be garbage collected
    internal sealed class DeviceMesh : IDisposable
    {
        private readonly int vertexCount;
        private readonly int indexCount;
        private readonly Memory.DeviceBuffer vertexBuffer;
        private readonly Memory.DeviceBuffer indexBuffer;
        private bool disposed;

        internal DeviceMesh(
            Mesh mesh,
            Device logicalDevice,
            Memory.Pool memoryPool,
            Memory.StagingBuffer stagingBuffer)
        {
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (memoryPool == null)
                throw new ArgumentNullException(nameof(memoryPool));
            if (stagingBuffer == null)
                throw new ArgumentNullException(nameof(stagingBuffer));

            vertexCount = mesh.VertexCount;
            indexCount = mesh.IndexCount;
            vertexBuffer = mesh.UploadVertices(logicalDevice, memoryPool, stagingBuffer);
            indexBuffer = mesh.UploadIndices(logicalDevice, memoryPool, stagingBuffer);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            vertexBuffer.Dispose();
            indexBuffer.Dispose();
            disposed = true;
        }

        internal void RecordBind(CommandBuffer commandbuffer)
        {
            ThrowIfDisposed();

            //Bind our the vertex and index buffer with our uploaded data
            commandbuffer.CmdBindVertexBuffer(vertexBuffer.Buffer, offset: 0);
            commandbuffer.CmdBindIndexBuffer(indexBuffer.Buffer, offset: 0, indexType: IndexType.UInt16);
        }

        internal void RecordDraw(CommandBuffer commandbuffer)
        {
            ThrowIfDisposed();

            //Draw all our indices
            commandbuffer.CmdDrawIndexed(
                indexCount: indexCount,
                instanceCount: 1,
                firstIndex: 0,
                firstInstance: 0);
        }

        internal PipelineVertexInputStateCreateInfo GetVertexInputStateInfo()
            => new PipelineVertexInputStateCreateInfo(
                vertexBindingDescriptions: new [] { Resources.Vertex.GetBindingDescription() }, 
                vertexAttributeDescriptions: Resources.Vertex.GetAttributeDescriptions());

        internal PipelineInputAssemblyStateCreateInfo GetInputAssemblyStateInfo()
            => new PipelineInputAssemblyStateCreateInfo(
                topology: PrimitiveTopology.TriangleList,
                primitiveRestartEnable: false);

        internal FrontFace GetFrontFace() => FrontFace.Clockwise;

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(DeviceMesh)}] Allready disposed");
        }
    }
}