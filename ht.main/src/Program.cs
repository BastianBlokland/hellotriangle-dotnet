using System;
using System.IO;
using System.Threading;

using HT.Engine.Platform;
using HT.Engine.Rendering;
using HT.Engine.Math;
using HT.Engine.Utils;
using HT.Engine.Tasks;
using HT.Engine.Resources;

namespace HT.Main
{
    public static class Program
    {
        private static readonly HostDeviceRequirements deviceRequirements 
            = new HostDeviceRequirements(samplerAnisotropy: true);
        
        public static void Run(INativeApp nativeApp, Logger logger = null)
        {
            using (var taskRunner = new TaskRunner(logger))
            using (var host = new Host(nativeApp, appName: "Test", appVersion: 1, logger: logger))
            using (var window = host.CreateWindow(windowSize: (800, 600), deviceRequirements))
            {
                RenderScene scene = new RenderScene(window, clearColor: ColorUtils.Yellow, logger);
                window.AttachScene(scene);

                var loader = new Loader(nativeApp,
                    "models/spaceship.dae",
                    "textures/spaceship_color.tga",
                    "shaders/bin/test.vert.spv",
                    "shaders/bin/test.frag.spv");
                loader.StartLoading(taskRunner);

                while (!loader.IsFinished)
                    taskRunner.Help();

                RenderObject renderObject = new RenderObject(
                    scene,
                    loader.GetResult<Mesh>("models/spaceship.dae"),
                    loader.GetResult<ByteTexture>("textures/spaceship_color.tga"),
                    loader.GetResult<ShaderProgram>("shaders/bin/test.vert.spv"),
                    loader.GetResult<ShaderProgram>("shaders/bin/test.frag.spv"));
                scene.AddObject(renderObject);

                while (!window.IsCloseRequested)
                {
                    //Call the os update loop to get os events about our windows
                    //Like input, resize, or close
                    nativeApp.Update();

                    //Draw the window (if its minimized there is no real point atm so we just
                    //sleep the cpu a bit)
                    if (!window.IsMinimized)
                        window.Draw();
                    else
                        Thread.Sleep(100);
                }
            }
        }
    }
}