using System;
using System.Collections.Generic;

using HT.Engine.Math;
using HT.Engine.Resources;
using HT.Engine.Utils;
using VulkanCore;

namespace HT.Engine.Rendering
{
    internal sealed class Renderer : IDisposable
    {
        private class Item : IDisposable
        {
            //Properties
            public int Order => order;
            public IInternalRenderObject RenderObject => renderObject;
            
            //Data
            private readonly int order;
            private readonly bool depthClamp;
            private readonly bool depthBias;
            private readonly IInternalRenderObject renderObject;
            private readonly ShaderModule vertModule;
            private readonly ShaderModule fragModule;

            private ShaderInputManager.Block? inputBlock;
            private PipelineLayout pipelineLayout;
            private Pipeline pipeline;

            public Item(
                int order,
                bool depthClamp,
                bool depthBias,
                IInternalRenderObject renderObject,
                ShaderProgram vertProg,
                ShaderProgram fragProg,
                Device logicalDevice)
            {
                this.order = order;
                this.renderObject = renderObject;
                this.depthClamp = depthClamp;
                this.depthBias = depthBias;

                vertModule = vertProg.CreateModule(logicalDevice);
                fragModule = fragProg.CreateModule(logicalDevice);
            }

            public void CreateResources(
                Device logicalDevice,
                ShaderInputManager shaderInputManager,
                RenderPass renderPass,
                ReadOnlySpan<DeviceTexture> targets,
                ReadOnlySpan<IShaderInput> globalInputs)
            {
                //Update the shader inputs
                //TODO: Get rid of all the array allocation here
                var totalInputs = globalInputs.ToArray().Concat(renderObject.Inputs.ToArray());
                if (inputBlock == null)
                    inputBlock = shaderInputManager.Allocate(totalInputs);
                else
                    inputBlock.Value.Update(totalInputs);
                
                //Create a pipeline layout if we don't have one allready
                if (pipelineLayout == null)
                    pipelineLayout = logicalDevice.CreatePipelineLayout(new PipelineLayoutCreateInfo(
                        setLayouts: new [] { inputBlock.Value.Layout }));

                //Create a pipeline if we don't have one allready
                if (pipeline == null)
                    pipeline = PipelineUtils.CreatePipeline(
                        logicalDevice,
                        renderPass,
                        pipelineLayout,
                        vertModule,
                        fragModule,
                        depthClamp,
                        depthBias,
                        targets,
                        renderObject);
            }

            public void Record(CommandBuffer commandbuffer)
            {
                if (inputBlock == null || pipeline == null)
                    throw new Exception($"[{nameof(Renderer)}] Resources have not been created yet");

                //Bind the inputs
                commandbuffer.CmdBindDescriptorSet(
                    PipelineBindPoint.Graphics, pipelineLayout, inputBlock.Value.Set);

                //Bind the pipeline
                commandbuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);
                
                //Record the object
                renderObject.Record(commandbuffer);
            }

            public void Dispose()
            {
                pipeline?.Dispose();
                pipelineLayout?.Dispose();
                inputBlock?.Free();
                vertModule.Dispose();
                fragModule.Dispose();
            }
        }

        private class Output : IDisposable
        {
            //Properties
            public Int2 Size => size;
            public ReadOnlySpan<DeviceTexture> Targets => targets;
            public Framebuffer Framebuffer => framebuffer;

            //Data
            private readonly Int2 size;
            private readonly DeviceTexture[] targets;
            private readonly Framebuffer framebuffer;

            public Output(RenderPass renderPass, ReadOnlySpan<DeviceTexture> targets)
            {
                if (targets == null || targets.Length == 0)
                    throw new ArgumentException(
                        $"[{nameof(Renderer)}] At least one target has to be provided", nameof(targets));

                this.size = targets[0].Size;
                this.targets = targets.ToArray();
                this.framebuffer = renderPass.CreateFramebuffer(new FramebufferCreateInfo(
                    attachments: targets.MorphArray(target => target.View),
                    width: size.X, height: size.Y));
            }

            public void Dispose() => framebuffer.Dispose();
        }

        //Data
        private readonly Device logicalDevice;
        private readonly ShaderInputManager shaderInputManager;
        private readonly Logger logger;
        private readonly List<Item> items = new List<Item>();

        private Output[] outputs = new Output[1];
        private IShaderInput[] globalInputs = new IShaderInput[0];
        private RenderPass renderPass;
        private bool disposed;
        
        internal Renderer(
            Device logicalDevice,
            ShaderInputManager shaderInputManager,
            Logger logger = null)
        {
            if (logicalDevice == null)
                throw new ArgumentNullException(nameof(logicalDevice));
            if (shaderInputManager == null)
                throw new ArgumentNullException(nameof(shaderInputManager));

            this.logicalDevice = logicalDevice;
            this.shaderInputManager = shaderInputManager;
            this.logger = logger;
        }

        public void AddObject(
            IInternalRenderObject renderObject,
            ShaderProgram vertProg,
            ShaderProgram fragProg,
            int order = 0,
            bool depthClamp = false,
            bool depthBias = false)
        {
            ThrowIfDisposed();

            items.Add(new Item(
                order,
                depthClamp,
                depthBias,
                renderObject,
                vertProg,
                fragProg,
                logicalDevice));

            //Sort the objects based on order
            items.Sort(CompareOrder);
        }

        public void RemoveObject(IInternalRenderObject renderObject)
        {
            ThrowIfDisposed();

            for (int i = items.Count - 1; i >= 0 ; i--)
                if (items[i].RenderObject == renderObject)
                    items.RemoveAt(i);
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            items.DisposeAll();
            outputs.DisposeAll();
            renderPass?.Dispose();
            disposed = true;
        }

        internal void SetOutputCount(int count)
        {
            ThrowIfDisposed();
            if (outputs.Length != count)
            {
                //Dispose of the old outputs
                for (int i = 0; i < outputs.Length; i++)
                    if (outputs[i] != null)
                        outputs[i].Dispose();
                
                outputs = new Output[count];
            }
        }

        internal void BindTargets(ReadOnlySpan<DeviceTexture> targets, int outputIndex = 0)
        {
            if (targets == null)
                throw new ArgumentNullException(nameof(targets));
            if (targets.Length == 0)
                throw new ArgumentException("Provide at least 1 render-target", nameof(targets));
            if (outputIndex < 0 || outputIndex >= outputs.Length)
                throw new ArgumentOutOfRangeException(nameof(outputIndex));
            ThrowIfDisposed();

            //NOTE: Currently we create the renderpass once and allow rebinding of targets, this
            //only works if the targets are in the same order and have the same formats and layouts.
            //they can however have different sizes, in the future to be more robust this should check
            //if we can reuse the renderpass or if we have to create a new one
            if (renderPass == null)
                renderPass = CreateRenderPass(logicalDevice, targets);

            //Dispose the old output
            outputs[outputIndex]?.Dispose();

            //Create a new output
            outputs[outputIndex] = new Output(renderPass, targets);
        }

        internal void BindGlobalInputs(ReadOnlySpan<IShaderInput> inputs)
        {
            ThrowIfDisposed();

            globalInputs = inputs.ToArray();
        }

        internal void CreateResources()
        {
            ThrowIfDisposed();

            if (outputs.Length == 0 || outputs[0] == null)
                throw new Exception($"[{nameof(Renderer)}] No output has been setup");

            //Create resources for all the items
            for (int i = 0; i < items.Count; i++)
                items[i].CreateResources(
                    logicalDevice,
                    shaderInputManager,
                    renderPass,
                    outputs[0].Targets,
                    globalInputs);
        }

        internal void Record(CommandBuffer commandbuffer, int outputIndex = 0)
        {
            if (commandbuffer == null)
                throw new ArgumentNullException(nameof(commandbuffer));
            if (outputIndex < 0 || outputIndex >= outputs.Length)
                throw new ArgumentOutOfRangeException(nameof(outputIndex));
            ThrowIfDisposed();

            Int2 size = outputs[outputIndex].Size;
            Framebuffer framebuffer = outputs[outputIndex].Framebuffer;
            ReadOnlySpan<DeviceTexture> targets = outputs[outputIndex].Targets;

            //Dynamically set the viewport so that we can avoid recreating renderpasses when resizing
            SetViewport(commandbuffer, size);

            //Begin the renderpass
            commandbuffer.CmdBeginRenderPass(new RenderPassBeginInfo(
                renderPass: renderPass,
                framebuffer: framebuffer,
                renderArea: new Rect2D(x: 0, y: 0, width: size.X, height: size.Y),
                clearValues: targets.MorphArray(texture => texture.DepthTexture
                    ? new ClearValue(new ClearDepthStencilValue(depth: 1f, stencil: 0))
                    : new ClearValue(new ClearColorValue(ColorF4.Zero)))));
            {
                //Record all individual items
                for (int i = 0; i < items.Count; i++)
                    items[i].Record(commandbuffer);
            }
            commandbuffer.CmdEndRenderPass();
        }

        private static void SetViewport(CommandBuffer commandbuffer, Int2 size)
        {
            //Set viewport and scissor-rect dynamically to avoid the pipelines depending on
            //swapchain size (and thus having to be recreated on resize)
            commandbuffer.CmdSetViewport(
                new Viewport(
                    x: 0f, y: 0f, width: size.X, height: size.Y,
                    minDepth: 0f, maxDepth: 1f));
            commandbuffer.CmdSetScissor(
                new Rect2D(x: 0, y: 0, width: size.X, height: size.Y));
        }

        private static RenderPass CreateRenderPass(
            Device logicalDevice,
            ReadOnlySpan<DeviceTexture> targets)
        {
            ResizeArray<AttachmentReference> colorReferences = new ResizeArray<AttachmentReference>();
            AttachmentReference? depthReference = null;

            var attachmentDescriptions = new AttachmentDescription[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                //Create the description
                attachmentDescriptions[i] = new AttachmentDescription(
                    flags: AttachmentDescriptions.MayAlias,
                    format: targets[i].Format,
                    samples: SampleCounts.Count1,
                    loadOp: AttachmentLoadOp.Clear,
                    storeOp: AttachmentStoreOp.Store,
                    stencilLoadOp: AttachmentLoadOp.DontCare,
                    stencilStoreOp: AttachmentStoreOp.DontCare,
                    initialLayout: ImageLayout.Undefined,
                    finalLayout: targets[i].DesiredLayout);
                //Add the reference (either color or depth)
                if (targets[i].DepthTexture)
                {
                    if (depthReference != null)
                        throw new Exception(
                            $"[{nameof(Renderer)}] Only 1 depth target can be used at a time");
                    else
                        depthReference = new AttachmentReference(i, ImageLayout.DepthStencilAttachmentOptimal);
                }
                else
                    colorReferences.Add(new AttachmentReference(i, ImageLayout.ColorAttachmentOptimal));
            }
            //Dependency at the beginning to transition the attachments to the proper layout
            var beginTransitionDependency = new SubpassDependency(
                srcSubpass: Constant.SubpassExternal, 
                dstSubpass: 0, //Our subpass
                srcStageMask: PipelineStages.BottomOfPipe,
                srcAccessMask: Accesses.MemoryRead,
                dstStageMask: PipelineStages.ColorAttachmentOutput,
                dstAccessMask: Accesses.ColorAttachmentWrite,
                dependencyFlags: Dependencies.ByRegion
            );
            //Dependency at the end to transition the attachments to the final layout
            var endTransitionDependency = new SubpassDependency(
                srcSubpass: 0, //Our subpass
                dstSubpass: Constant.SubpassExternal, 
                srcStageMask: PipelineStages.ColorAttachmentOutput,
                srcAccessMask: Accesses.ColorAttachmentWrite,
                dstStageMask: PipelineStages.BottomOfPipe,
                dstAccessMask: Accesses.MemoryRead,
                dependencyFlags: Dependencies.ByRegion
            );
            return logicalDevice.CreateRenderPass(new RenderPassCreateInfo(
                subpasses: new [] 
                {
                    new SubpassDescription
                    (
                        flags: SubpassDescriptionFlags.None,
                        colorAttachments: colorReferences.ToArray(),
                        depthStencilAttachment: depthReference
                    )
                },
                attachments: attachmentDescriptions,
                dependencies: new [] { beginTransitionDependency, endTransitionDependency } 
            ));
        }

        private static int CompareOrder(Item a, Item b) => a.Order.CompareTo(b.Order);

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new Exception($"[{nameof(Renderer)}] Allready disposed");
        }
    }
}