using System;

using HT.Engine.Math;
using VulkanCore;

namespace HT.Engine.Rendering
{
    public sealed class RenderScene
    {
        private readonly Float4 clearColor;

        public RenderScene(Float4 clearColor) => this.clearColor = clearColor;

        internal void Record(CommandBuffer commandbuffer, Framebuffer framebuffer, Int2 swapchainSize)
        {
            commandbuffer.CmdBeginRenderPass(new RenderPassBeginInfo
            (
                framebuffer: framebuffer,
                renderArea: new Rect2D(x: 0, y: 0, width: swapchainSize.X, height: swapchainSize.Y),
                clearValues: new ClearValue(new ClearColorValue(new ColorF4(clearColor.R, clearColor.G, clearColor.B, clearColor.A)))
            ));
        }
    }
}