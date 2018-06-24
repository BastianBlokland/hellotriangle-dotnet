using System;

using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering.Model
{
    public sealed class Mesh
    {
        //Helper properties
        internal bool Uploaded => uploaded;

        //Data
        private readonly Vertex[] vertices;
        private readonly UInt16[] indices;

        private bool uploaded;
        private Memory.DeviceBuffer vertexBuffer;
        private Memory.DeviceBuffer indexBuffer;

        internal Mesh(Vertex[] vertices, UInt16[] indices)
        {
            this.vertices = vertices;
            this.indices = indices;
        }

        internal void Upload(
            Device logicalDevice,
            Memory.PoolGroup memoryGroup,
            Memory.StagingBuffer stagingBuffer)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));

            //Upload our vertices
            vertexBuffer = new Memory.DeviceBuffer(
                logicalDevice: logicalDevice,
                memoryGroup: memoryGroup,
                size: vertices.GetSize(),
                usages: BufferUsages.VertexBuffer);
            stagingBuffer.Upload(vertices, vertexBuffer);

            //Upload our indices
            indexBuffer = new Memory.DeviceBuffer(
                logicalDevice: logicalDevice,
                memoryGroup: memoryGroup,
                size: indices.GetSize(),
                usages: BufferUsages.IndexBuffer);
            stagingBuffer.Upload(indices, indexBuffer);
            uploaded = true;
        }

        internal void ClearUpload()
        {
            if (uploaded)
            {
                vertexBuffer.Dispose();
                indexBuffer.Dispose();
            }
            uploaded = false;
        }

        internal void RecordBind(CommandBuffer commandbuffer)
        {
            ThrowIfNotUploaded();

            //Bind our the vertex and index buffer with our uploaded data
            commandbuffer.CmdBindVertexBuffer(vertexBuffer.Buffer, offset: 0);
            commandbuffer.CmdBindIndexBuffer(indexBuffer.Buffer, offset: 0, indexType: IndexType.UInt16);
        }

        internal void RecordDraw(CommandBuffer commandbuffer)
        {
            ThrowIfNotUploaded();

            //Draw all our indices
            commandbuffer.CmdDrawIndexed(
                indexCount: indices.Length,
                instanceCount: 1,
                firstIndex: 0,
                firstInstance: 0);
        }

        internal PipelineVertexInputStateCreateInfo GetVertexInputStateInfo()
            => new PipelineVertexInputStateCreateInfo(
                vertexBindingDescriptions: new [] { Model.Vertex.GetBindingDescription() }, 
                vertexAttributeDescriptions: Model.Vertex.GetAttributeDescriptions());

        internal PipelineInputAssemblyStateCreateInfo GetInputAssemblyStateInfo()
            => new PipelineInputAssemblyStateCreateInfo(
                topology: PrimitiveTopology.TriangleList,
                primitiveRestartEnable: false);

        internal FrontFace GetFrontFace() => FrontFace.Clockwise;

        private void ThrowIfNotUploaded()
        {
            if (!uploaded)
                throw new Exception($"[{nameof(Mesh)}] Data has not been upload yet");
        }
    }
}