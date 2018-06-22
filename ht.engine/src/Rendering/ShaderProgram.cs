using System;
using System.IO;

using HT.Engine.Platform;
using HT.Engine.Utils;
using HT.Engine.Utils.Extensions;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class ShaderProgram
    {
        private readonly byte[] binaryCode;

        public ShaderProgram(INativeApp nativeApp, string sourceFilename)
        {
            if (nativeApp == null)
                throw new ArgumentNullException(nameof(nativeApp));
            string binFileName = Path.Combine("shaders", "bin", $"{sourceFilename}.spv");
            using(var file = nativeApp.ReadFile(binFileName))
            {
                binaryCode = file.ReadToEnd();
            }
        }

        internal ShaderModule CreateModule(Device device) =>
            device.CreateShaderModule(new ShaderModuleCreateInfo(binaryCode));
    }
}