using System;

using VulkanCore;

namespace HT.Engine.Rendering.Model
{
    public sealed class Mesh
    {
        internal bool Uploaded => uploaded;

        private readonly Vertex[] vertices;
        private readonly UInt16[] indices;

        private bool uploaded;
        private Memory.Pool memoryPool;
        private Memory.Region verticesRegion;
        private Memory.Region indicesRegion;

        internal Mesh(Vertex[] vertices, UInt16[] indices)
        {
            this.vertices = vertices;
            this.indices = indices;
        }

        internal void Upload(Memory.Pool memoryPool)
        {
            //Sanity check that the given pool supports vertices and indices
            if (!memoryPool.Usages.HasFlag(BufferUsages.VertexBuffer))
                throw new ArgumentException(
                    $"[{nameof(Mesh)}] Given pool cannot contain a vertex-buffer", nameof(memoryPool));
            if (!memoryPool.Usages.HasFlag(BufferUsages.IndexBuffer))
                throw new ArgumentException(
                    $"[{nameof(Mesh)}] Given pool cannot contain a index-buffer", nameof(memoryPool));
            //Only upload to the gpu once
            if (uploaded)
                throw new Exception($"[{nameof(Mesh)}] Allready uploaded");

            //Save the pool so we know to what buffer we've uploaded the data
            this.memoryPool = memoryPool;

            //Allocate the space in the pool
            verticesRegion = memoryPool.Allocate<Vertex>(vertices.Length);
            indicesRegion = memoryPool.Allocate<UInt16>(indices.Length);

            //Write the date to the pool.
            //This is a completely blocking operation, in the future we can support asynchronous
            //uploading to the gpu
            memoryPool.Write(vertices, verticesRegion);
            memoryPool.Write(indices, indicesRegion);

            uploaded = true;
        }

        internal void RecordBind(CommandBuffer commandbuffer)
        {
            ThrowIfNotUploaded();

            //Bind our the vertex and index buffer with our uploaded data
            commandbuffer.CmdBindVertexBuffer(memoryPool.Buffer, verticesRegion.Offset);
            commandbuffer.CmdBindIndexBuffer(memoryPool.Buffer, indicesRegion.Offset, IndexType.UInt16);
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