using System;
using System.Runtime.CompilerServices;
using HT.Engine.Math;
using HT.Engine.Resources;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class InstancedObject : IInternalRenderObject
    {
        //Properties
        ReadOnlySpan<IShaderInput> IInternalRenderObject.Inputs => inputs;

        //Data
        private readonly IShaderInput[] inputs;
        private readonly DeviceMesh deviceMesh;
        private readonly Memory.HostBuffer instanceDataBuffer;
        private readonly Memory.HostBuffer indirectArgumentsBuffer;
        private bool disposed;

        public InstancedObject(
            RenderScene scene,
            Mesh mesh,
            TextureInfo[] textureInfos,
            int maxInstances = 100_000)
        {
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));

            //Prepare the inputs
            inputs = new IShaderInput[textureInfos.Length];
            for (int i = 0; i < inputs.Length; i++)
            {
                DeviceTexture texture = DeviceTexture.UploadTexture(
                    texture: textureInfos[i].Texture as IInternalTexture,
                    scene, generateMipMaps: textureInfos[i].UseMipMaps);
                inputs[i] = new DeviceSampler(
                    scene.LogicalDevice,
                    texture,
                    disposeTexture: true,
                    repeat: textureInfos[i].Repeat,
                    maxAnisotropy: 8f);
            }

            //Upload our mesh to the gpu
            deviceMesh = new DeviceMesh(
                mesh, 
                scene.LogicalDevice,
                scene.MemoryPool,
                scene.StagingBuffer,
                scene.Executor);

            //Allocate a buffers for the instance data and indirect args
            instanceDataBuffer = new Memory.HostBuffer(
                logicalDevice: scene.LogicalDevice, 
                memoryPool: scene.MemoryPool,
                usages: BufferUsages.VertexBuffer,
                size: InstanceData.SIZE * maxInstances);
            indirectArgumentsBuffer = new Memory.HostBuffer(
                logicalDevice: scene.LogicalDevice,
                memoryPool: scene.MemoryPool,
                usages: BufferUsages.IndirectBuffer,
                size: DrawIndexedIndirectCommand.SIZE);

            //Write defaults to the indirect args buffer
            indirectArgumentsBuffer.Write(new DrawIndexedIndirectCommand(
                indexCount: (uint)deviceMesh.IndexCount,
                instanceCount: 0, firstIndex: 0, vertexOffset: 0, firstInstance: 0));
        }

        public void UpdateInstances(ReadOnlySpan<InstanceData> instances)
        {
            instanceDataBuffer.Write(instances);
            indirectArgumentsBuffer.Write(new DrawIndexedIndirectCommand(
                indexCount: (uint)deviceMesh.IndexCount,
                instanceCount: (uint)instances.Length,
                firstIndex: 0,
                vertexOffset: 0,
                firstInstance: 0));
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            deviceMesh.Dispose();
            instanceDataBuffer.Dispose();
            indirectArgumentsBuffer.Dispose();
            inputs.DisposeAll();
            disposed = true;
        }

        FrontFace IInternalRenderObject.GetFrontFace() =>
            deviceMesh.GetFrontFace();

        PipelineInputAssemblyStateCreateInfo IInternalRenderObject.GetInputAssemblyStateInfo()
            => deviceMesh.GetInputAssemblyStateInfo();

        PipelineVertexInputStateCreateInfo IInternalRenderObject.GetVertexInputState()
        {
            //Gather the attribute descriptions
            var vertexAttributeDescriptions = new ResizeArray<VertexInputAttributeDescription>();
            Vertex.AddAttributeDescriptions(binding: 0, vertexAttributeDescriptions);
            InstanceData.AddAttributeDescriptions(binding: 1, vertexAttributeDescriptions);

            return new PipelineVertexInputStateCreateInfo(
                vertexBindingDescriptions: new [] 
                { 
                    new VertexInputBindingDescription(
                        binding: 0,
                        stride: Vertex.SIZE,
                        inputRate: VertexInputRate.Vertex),
                    new VertexInputBindingDescription(
                        binding: 1,
                        stride: InstanceData.SIZE,
                        inputRate: VertexInputRate.Instance)
                },
                vertexAttributeDescriptions: vertexAttributeDescriptions.ToArray());
        }

        void IInternalRenderObject.Record(CommandBuffer commandbuffer)
        {
            //Bind mesh data
            deviceMesh.RecordBind(commandbuffer, binding: 0);

            //Binding instance data
            commandbuffer.CmdBindVertexBuffer(
                instanceDataBuffer.VulkanBuffer,
                firstBinding: 1,
                offset: 0);

            //Draw
            commandbuffer.CmdDrawIndexedIndirect(
                buffer: indirectArgumentsBuffer.VulkanBuffer,
                offset: 0,
                drawCount: 1,
                stride: DrawIndexedIndirectCommand.SIZE);
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(InstancedObject)}] Allready disposed");
        }
    }
}