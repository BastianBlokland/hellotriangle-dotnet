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
            {
                var loader = new Loader(nativeApp,
                    "models/spaceship.dae",
                    "textures/spaceship_color.tga",
                    "shaders/bin/test.vert.spv",
                    "shaders/bin/test.frag.spv");
                loader.StartLoading(taskRunner);

                //Let the main thread help out until we've loaded our assets
                while (!loader.IsFinished)
                    taskRunner.Help();

                using (var window = host.CreateWindow(
                    windowSize: (x: 800, y: 600),
                    deviceRequirements: deviceRequirements,
                    scene: new RenderScene(
                        clearColor: ColorUtils.Yellow,
                        renderobjects: new [] { new RenderObject(
                            loader.GetResult<Mesh>("models/spaceship.dae"),
                            loader.GetResult<ByteTexture>("textures/spaceship_color.tga"),
                            loader.GetResult<ShaderProgram>("shaders/bin/test.vert.spv"),
                            loader.GetResult<ShaderProgram>("shaders/bin/test.frag.spv")) })))
                {
                    bool running = true;
                    window.CloseRequested += () => running = false;

                    while (running)
                    {
                        nativeApp.Update();
                        
                        window.Update();
                        bool hasDrawn = window.Draw();
                        if (!hasDrawn)
                            Thread.Sleep(100);
                    }
                }
            }
        }
    }
}