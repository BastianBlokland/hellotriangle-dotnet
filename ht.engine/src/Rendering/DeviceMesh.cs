using System;
using System.Diagnostics;

using HT.Engine.Resources;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering
{
    //GPU representation of a mesh.
    //NOTE: Does not hold on to the cpu representation of the mesh so it can be garbage collected
    internal sealed class DeviceMesh : IDisposable
    {
        //Internal properties
        internal int VertexCount => vertexCount;
        internal int IndexCount => indexCount;

        //Data
        private readonly PrimitiveTopology topology;
        private readonly bool allowRestart;
        private readonly int vertexCount;
        private readonly int indexCount;
        private readonly Memory.DeviceBuffer vertexBuffer;
        private readonly Memory.DeviceBuffer indexBuffer;
        private bool disposed;

        internal DeviceMesh(Mesh mesh, RenderScene scene)
        {
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));

            topology = mesh.Topology;
            allowRestart = mesh.AllowRestart;
            vertexCount = mesh.VertexCount;
            indexCount = mesh.IndexCount;
            vertexBuffer = mesh.UploadVertices(scene);
            indexBuffer = mesh.UploadIndices(scene);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            vertexBuffer.Dispose();
            indexBuffer.Dispose();
            disposed = true;
        }

        internal void RecordBind(CommandBuffer commandbuffer, int binding)
        {
            ThrowIfDisposed();

            //Bind our the vertex and index buffer with our uploaded data
            commandbuffer.CmdBindVertexBuffer(vertexBuffer.VulkanBuffer, firstBinding: binding, offset: 0);
            commandbuffer.CmdBindIndexBuffer(indexBuffer.VulkanBuffer, offset: 0, indexType: IndexType.UInt16);
        }

        internal PipelineInputAssemblyStateCreateInfo GetInputAssemblyStateInfo()
            => new PipelineInputAssemblyStateCreateInfo(
                topology: topology,
                primitiveRestartEnable: allowRestart);

        internal FrontFace GetFrontFace() => FrontFace.Clockwise;

        [Conditional("DEBUG")]
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(DeviceMesh)}] Allready disposed");
        }
    }
}