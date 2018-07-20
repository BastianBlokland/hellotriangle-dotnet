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
                scene.Camera.Transformation = 
                    Float4x4.CreateLookAt(new Float3(0f, .5f, -2f), Float3.Zero, Float3.Up);
                window.AttachScene(scene);

                AddObject(nativeApp, taskRunner, scene,
                    modelPath: "models/spaceship.dae",
                    texturePath: "textures/spaceship_color.tga",
                    vertShaderPath: "shaders/bin/test.vert.spv",
                    fragShaderPath: "shaders/bin/test.frag.spv");

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

        private static void AddObject(
            INativeApp app,
            TaskRunner taskRunner,
            RenderScene scene,
            string modelPath,
            string texturePath,
            string vertShaderPath,
            string fragShaderPath)
        {
            var loader = new Loader(app, modelPath, texturePath, vertShaderPath, fragShaderPath);
            loader.StartLoading(taskRunner);

            while (!loader.IsFinished)
                taskRunner.Help();

            //NOTE: At the moment 'RenderObject' creation is a fully single threaded thing because it
            //uses resources that are shared, like the staging buffer and also allot of the api is
            //still single threaded (like the MemoryPool and DescriptorManager), so for now it has to
            //be like this but in the future i would like to update the api so we can have object
            //creation happening from multiple threads so we can load multiple objects at the same 
            //time and also creating objects while the main loop is still going on
            RenderObject renderObject = new RenderObject(
                scene, 
                loader.GetResult<Mesh>(modelPath),
                loader.GetResult<ByteTexture>(texturePath),
                loader.GetResult<ShaderProgram>(vertShaderPath),
                loader.GetResult<ShaderProgram>(fragShaderPath));
            scene.AddObject(renderObject);
        }
    }
}