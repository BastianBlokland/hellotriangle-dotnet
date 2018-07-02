using System;
using System.IO;

using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Resources
{
    public sealed class ShaderProgram
    {
        private readonly byte[] shaderByteCode;

        public ShaderProgram(byte[] shaderByteCode)
        {
            if (shaderByteCode == null)
                throw new ArgumentNullException(nameof(shaderByteCode));
            this.shaderByteCode = shaderByteCode;
        }

        internal ShaderModule CreateModule(Device device) =>
            device.CreateShaderModule(new ShaderModuleCreateInfo(shaderByteCode));
    }
}