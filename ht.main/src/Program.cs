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

                //Add the fighter to the scene
                var fighter = AddObject(nativeApp, taskRunner, scene,
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
                            modelMatrix: Float4x4.CreateTranslation(((x - 32f) * 3f, 0f, (y - 32f) * 3f)),
                            age: (float)frameTracker.ElapsedTime);
                    fighter.UpdateInstances(fighterInstances);

                    //Rotate the camera
                    scene.Camera.Transformation = Float4x4.CreateOrbit(
                        center: Float3.Zero,
                        offset: (0f, .5f, -2f),
                        axis: Float3.Up,
                        angle: (float)frameTracker.ElapsedTime);

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

        private static RenderObject AddObject(
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
            var textures = new ByteTexture[texturePaths.Length];
            for (int i = 0; i < textures.Length; i++)
                textures[i] = loader.GetResult<ByteTexture>(texturePaths[i]);

            //NOTE: At the moment 'RenderObject' creation is a fully single threaded thing because it
            //uses resources that are shared, like the staging buffer and also allot of the api is
            //still single threaded (like the MemoryPool and DescriptorManager), so for now it has to
            //be like this but in the future i would like to update the api so we can have object
            //creation happening from multiple threads so we can load multiple objects at the same 
            //time and also creating objects while the main loop is still going on
            RenderObject renderObject = new RenderObject(
                scene, 
                loader.GetResult<Mesh>(modelPath),
                textures,
                loader.GetResult<ShaderProgram>(vertShaderPath),
                loader.GetResult<ShaderProgram>(fragShaderPath));
            scene.AddObject(renderObject);
            return renderObject;
        }
    }
}