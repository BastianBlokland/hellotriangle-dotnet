using System.IO;

using HT.Engine.Platform;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class ShaderProgram
    {
        private readonly byte[] binaryCode;

        public ShaderProgram(INativeApp nativeApp, string sourceFilename)
        {
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