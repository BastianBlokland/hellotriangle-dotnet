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
                RenderScene scene = new RenderScene(window, clearColor: null,
                    compositionVertProg: LoadProg("shaders/bin/post_fullscreen.vert.spv"),
                    compositionFragProg: LoadProg("shaders/bin/post_baselighting.frag.spv"), logger);
                window.AttachScene(scene);

                //Add terrain to the scene
                AddTerrain(nativeApp, taskRunner, scene, renderOrder: 900,
                    textureSources: new []
                    { 
                        ("textures/terrain.tga", useMipMaps: false, repeat: false),
                        ("textures/terrain_detail_1.tga", useMipMaps: false, repeat: true),
                        ("textures/terrain_detail_2.tga", useMipMaps: true, repeat: true)
                    },
                    vertShaderPath: "shaders/bin/terrain.vert.spv",
                    fragShaderPath: "shaders/bin/terrain.frag.spv");

                //Add vegetation
                AddVegetation(nativeApp, taskRunner, scene, renderOrder: 950,
                    modelSource: ("models/bush.obj", scale: .005f),
                    textureSources: new []
                    { 
                        ("textures/bush_color.tga", useMipMaps: true, repeat: false),
                        ("textures/terrain.tga", useMipMaps: false, repeat: false)
                    },
                    vertShaderPath: "shaders/bin/vegetation.vert.spv",
                    fragShaderPath: "shaders/bin/vegetation.frag.spv");

                //Add skybox to the scene
                AddAttributelessObject(nativeApp, taskRunner, scene, renderOrder: 1000,
                    vertexCount: 3, //Uses fullscreen triangle 'trick'
                    textureSources: new [] { ("textures/skybox.cube", useMipMaps: false, repeat: false) },
                    vertShaderPath: "shaders/bin/skybox.vert.spv",
                    fragShaderPath: "shaders/bin/skybox.frag.spv");

                //Add the fighter to the scene
                var fighter = AddInstancedObject(nativeApp, taskRunner, scene, renderOrder: 0,
                    modelSource: ("models/fighter.dae", scale: 2f),
                    textureSources: new [] { ("textures/fighter_color.tga", useMipMaps: true, repeat: false) },
                    vertShaderPath: "shaders/bin/fighter.vert.spv",
                    fragShaderPath: "shaders/bin/fighter.frag.spv");
                InstanceData[] fighterInstances = new InstanceData[16 * 16];
                for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                    fighterInstances[y * 16 + x] = new InstanceData(
                        Float4x4.CreateTranslation(((x - 8) * 5f, 7.5f, (y - 8) * 5f)), age: 1f);
                fighter.UpdateInstances(fighterInstances);

                var frameTracker = new FrameTracker();
                while (!window.IsCloseRequested)
                {
                    //Rotate the camera
                    scene.Camera.Transformation = Float4x4.CreateOrbit(
                        center: (0f, 3f, 0f),
                        offset: (0f, 1f, -5f),
                        axis: Float3.Up,
                        angle: (float)frameTracker.ElapsedTime * .25f);

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

                //Utilities
                ShaderProgram LoadProg(string path)
                    => Load(nativeApp, taskRunner, path).GetResult<ShaderProgram>(path);
            }
        }

        private static InstancedObject AddTerrain(
            INativeApp app,
            TaskRunner taskRunner,
            RenderScene scene,
            int renderOrder,
            (string path, bool useMipMaps, bool repeat)[] textureSources,
            string vertShaderPath,
            string fragShaderPath)
        {
            const int PATCH_SEGMENTS = 64;
            const int PATCH_SIZE = 32;
            const int PATCH_COUNT = 8;
            const float PATCH_OFFSET = (PATCH_COUNT - 1) / 2f;

            InstanceData[] instances = new InstanceData[PATCH_COUNT * PATCH_COUNT];
            for (int x = 0; x < PATCH_COUNT; x++)
            for (int y = 0; y < PATCH_COUNT; y++)
                instances[y * PATCH_COUNT + x] = new InstanceData(
                    Float4x4.CreateTranslation(new Float3(
                            x: (x - PATCH_OFFSET) * PATCH_SIZE,
                            y: 0f,
                            z: (y - PATCH_OFFSET) * PATCH_SIZE)));
            
            var terrain = AddInstancedObject(
                app,
                taskRunner,
                scene,
                MeshUtils.CreatePlane(segments: PATCH_SEGMENTS, size: PATCH_SIZE),
                renderOrder,
                textureSources,
                vertShaderPath,
                fragShaderPath);
            terrain.UpdateInstances(instances);
            return terrain;
        }

        private static void AddVegetation(
            INativeApp app,
            TaskRunner taskRunner,
            RenderScene scene,
            int renderOrder,
            (string path, float scale) modelSource,
            (string path, bool useMipMaps, bool repeat)[] textureSources,
            string vertShaderPath,
            string fragShaderPath)
        {
            const int BUSH_COUNT = 5_000;
            
            var random = new ShiftRandom(seed: 1337);
            var bush = AddInstancedObject(
                app, taskRunner, scene, renderOrder,
                modelSource, textureSources, vertShaderPath, fragShaderPath);

            InstanceData[] bushInstances = new InstanceData[BUSH_COUNT];
            for (int i = 0; i < BUSH_COUNT; i++)
            {
                Float3 pos = (random.GetBetween(-128f, 128f), 0f, random.GetBetween(-128f, 128f));
                float yRot = random.GetNextAngle();
                float scale = random.GetBetween(1f, 2.5f);
                bushInstances[i] = new InstanceData(
                    Float4x4.CreateTranslation(pos) *
                    Float4x4.CreateRotationFromYAngle(yRot) *
                    Float4x4.CreateScale(scale));
            }
            bush.UpdateInstances(bushInstances);
        }

        private static InstancedObject AddInstancedObject(
            INativeApp app,
            TaskRunner taskRunner,
            RenderScene scene,
            Mesh model,
            int renderOrder,
            (string path, bool useMipMaps, bool repeat)[] textureSources,
            string vertShaderPath,
            string fragShaderPath)
        {
            var loader = Load(app, taskRunner, ArrayUtils.Build<string>(
                textureSources.Morph(a => a.path),
                vertShaderPath,
                fragShaderPath));

            var textures = new TextureInfo[textureSources.Length];
            for (int i = 0; i < textures.Length; i++)
                textures[i] = new TextureInfo(
                    loader.GetResult<ITexture>(textureSources[i].path),
                    textureSources[i].useMipMaps,
                    textureSources[i].repeat);

            InstancedObject renderObject = new InstancedObject(
                scene, renderOrder, model, textures,
                loader.GetResult<ShaderProgram>(vertShaderPath),
                loader.GetResult<ShaderProgram>(fragShaderPath));
            scene.AddObject(renderObject);
            return renderObject;
        }

        private static InstancedObject AddInstancedObject(
            INativeApp app,
            TaskRunner taskRunner,
            RenderScene scene,
            int renderOrder,
            (string path, float scale) modelSource,
            (string path, bool useMipMaps, bool repeat)[] textureSources,
            string vertShaderPath,
            string fragShaderPath)
        {
            var loader = Load(app, taskRunner, ArrayUtils.Build<string>(
                modelSource.path,
                textureSources.Morph(a => a.path),
                vertShaderPath,
                fragShaderPath));

            var mesh = loader.GetResult<Mesh>(modelSource.path);
            mesh.Scale(modelSource.scale);

            var textures = new TextureInfo[textureSources.Length];
            for (int i = 0; i < textures.Length; i++)
                textures[i] = new TextureInfo(
                    loader.GetResult<ITexture>(textureSources[i].path),
                    textureSources[i].useMipMaps,
                    textureSources[i].repeat);

            InstancedObject renderObject = new InstancedObject(
                scene, renderOrder, mesh, textures,
                loader.GetResult<ShaderProgram>(vertShaderPath),
                loader.GetResult<ShaderProgram>(fragShaderPath));
            scene.AddObject(renderObject);
            return renderObject;
        }

        private static AttributelessObject AddAttributelessObject(
            INativeApp app,
            TaskRunner taskRunner,
            RenderScene scene,
            int renderOrder,
            int vertexCount,
            (string path, bool useMipMaps, bool repeat)[] textureSources,
            string vertShaderPath,
            string fragShaderPath)
        {
            var loader = Load(app, taskRunner, ArrayUtils.Build<string>(
                textureSources.Morph(a => a.path),
                vertShaderPath,
                fragShaderPath));

            var textures = new TextureInfo[textureSources.Length];
            for (int i = 0; i < textures.Length; i++)
                textures[i] = new TextureInfo(
                    loader.GetResult<ITexture>(textureSources[i].path),
                    textureSources[i].useMipMaps,
                    textureSources[i].repeat);

            AttributelessObject renderObject = new AttributelessObject(
                scene, renderOrder, vertexCount, textures,
                loader.GetResult<ShaderProgram>(vertShaderPath),
                loader.GetResult<ShaderProgram>(fragShaderPath));
            scene.AddObject(renderObject);
            return renderObject;
        }

        private static Loader Load(INativeApp app, TaskRunner taskRunner, params string[] paths)
        {
            //Start loading all the resources
            var loader = new Loader(app, paths);
            loader.StartLoading(taskRunner);

            //Wait for the resources to be loaded
            while (!loader.IsFinished)
                taskRunner.Help();
            
            return loader;
        }
    }
}