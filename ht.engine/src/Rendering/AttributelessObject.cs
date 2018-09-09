using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using HT.Engine.Math;
using HT.Engine.Resources;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class AttributelessObject : IInternalRenderObject
    {
        //Properties
        ReadOnlySpan<IShaderInput> IInternalRenderObject.Inputs => inputs;

        //Data
        private readonly IShaderInput[] inputs;
        private readonly int vertexCount;
        private bool disposed;

        public AttributelessObject(
            RenderScene scene,
            int vertexCount,
            ReadOnlySpan<TextureInfo> textureInfos)
        {
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));
            this.vertexCount = vertexCount;

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
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            inputs.DisposeAll();
            disposed = true;
        }

        FrontFace IInternalRenderObject.GetFrontFace() => FrontFace.Clockwise;

        PipelineInputAssemblyStateCreateInfo IInternalRenderObject.GetInputAssemblyStateInfo()
            => new PipelineInputAssemblyStateCreateInfo(PrimitiveTopology.TriangleList);

        PipelineVertexInputStateCreateInfo IInternalRenderObject.GetVertexInputState()
            => new PipelineVertexInputStateCreateInfo(); //No vertex inputs

        void IInternalRenderObject.Record(CommandBuffer commandbuffer)
        {
            //Draw
            commandbuffer.CmdDraw(vertexCount, instanceCount: 1, firstVertex: 0, firstInstance: 0);
        }

        [Conditional("DEBUG")]
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(InstancedObject)}] Allready disposed");
        }
    }
}