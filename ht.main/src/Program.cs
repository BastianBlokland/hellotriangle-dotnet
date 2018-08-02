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
                RenderScene scene = new RenderScene(window, clearColor: null, logger);
                window.AttachScene(scene);

                //Add skybox to the scene
                AddAttributelessObject(nativeApp, taskRunner, scene,
                    vertexCount: 3, //Uses fullscreen triangle 'trick'
                    texturePaths: new [] { "textures/skybox.cube" },
                    vertShaderPath: "shaders/bin/skybox.vert.spv",
                    fragShaderPath: "shaders/bin/skybox.frag.spv");

                //Add the fighter to the scene
                var fighter = AddInstancedObject(nativeApp, taskRunner, scene,
                    modelPath: "models/fighter.dae",
                    texturePaths: new [] { "textures/fighter_color.tga", "textures/fighter_exhaust.tga" },
                    vertShaderPath: "shaders/bin/fighter.vert.spv",
                    fragShaderPath: "shaders/bin/fighter.frag.spv");
                InstanceData[] fighterInstances = new InstanceData[64 * 64];

                var frameTracker = new FrameTracker();
                while (!window.IsCloseRequested)
                {
                    //Update the fighter instances
                    for (int x = 0; x < 64; x++)
                    for (int y = 0; y < 64; y++)
                        fighterInstances[y * 64 + x] = new InstanceData(
                            modelMatrix: Float4x4.CreateTranslation(((x - 32f) * 3f, -.5f, (y - 32f) * 3f)),
                            age: (float)frameTracker.ElapsedTime);
                    fighter.UpdateInstances(fighterInstances);

                    //Rotate the camera
                    scene.Camera.Transformation = Float4x4.CreateOrbit(
                        center: Float3.Zero,
                        offset: (0f, 0f, -5f),
                        axis: Float3.Up,
                        angle: (float)frameTracker.ElapsedTime * .5f);

                    //Call the os update loop to get os events about our windows
                    //Like input, resize, or close
                    nativeApp.Update();

                    //Draw the window (if its minimized there is no real point atm so we just
                    //sleep the cpu a bit)
                    if (!window.IsMinimized)
                        window.Draw(frameTracker);
                    else
                        Thread.Sleep(100);

                    //Track frame-number, deltatime, etc..
                    frameTracker.TrackFrame();
                }
            }
        }

        private static InstancedObject AddInstancedObject(
            INativeApp app,
            TaskRunner taskRunner,
            RenderScene scene,
            string modelPath,
            string[] texturePaths,
            string vertShaderPath,
            string fragShaderPath)
        {
            var paths = ArrayUtils.Build<string>(modelPath, texturePaths, vertShaderPath, fragShaderPath);

            //Star loading all the resources
            var loader = new Loader(app, paths);
            loader.StartLoading(taskRunner);

            //Wait for the resources to be loaded
            while (!loader.IsFinished)
                taskRunner.Help();

            //Gather the loaded textures
            var textures = new ITexture[texturePaths.Length];
            for (int i = 0; i < textures.Length; i++)
                textures[i] = loader.GetResult<ITexture>(texturePaths[i]);

            InstancedObject renderObject = new InstancedObject(
                scene, 
                loader.GetResult<Mesh>(modelPath),
                textures,
                loader.GetResult<ShaderProgram>(vertShaderPath),
                loader.GetResult<ShaderProgram>(fragShaderPath));
            scene.AddObject(renderObject);
            return renderObject;
        }

        private static AttributelessObject AddAttributelessObject(
            INativeApp app,
            TaskRunner taskRunner,
            RenderScene scene,
            int vertexCount,
            string[] texturePaths,
            string vertShaderPath,
            string fragShaderPath)
        {
            var paths = ArrayUtils.Build<string>(texturePaths, vertShaderPath, fragShaderPath);

            //Star loading all the resources
            var loader = new Loader(app, paths);
            loader.StartLoading(taskRunner);

            //Wait for the resources to be loaded
            while (!loader.IsFinished)
                taskRunner.Help();

            //Gather the loaded textures
            var textures = new ITexture[texturePaths.Length];
            for (int i = 0; i < textures.Length; i++)
                textures[i] = loader.GetResult<ITexture>(texturePaths[i]);

            AttributelessObject renderObject = new AttributelessObject(
                scene, vertexCount, textures,
                loader.GetResult<ShaderProgram>(vertShaderPath),
                loader.GetResult<ShaderProgram>(fragShaderPath));
            scene.AddObject(renderObject);
            return renderObject;
        }
    }
}