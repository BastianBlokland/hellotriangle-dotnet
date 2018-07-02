using System;
using System.IO;
using System.Threading;

using HT.Engine.Platform;
using HT.Engine.Rendering;
using HT.Engine.Math;
using HT.Engine.Utils;
using HT.Engine.Tasks;

namespace HT.Main
{
    public static class Program
    {
        public static void Run(INativeApp nativeApp, Logger logger = null)
        {
            HT.Engine.Resources.Mesh mesh;
            using (var parser = new HT.Engine.Parsing.ColladaParser(
                inputStream: nativeApp.ReadFile(Path.Combine("models", "spaceship.dae"))))
            {
                mesh = parser.Parse();
            }
            HT.Engine.Resources.ByteTexture texture;
            using (var parser = new HT.Engine.Parsing.TruevisionTgaParser(
                inputStream: nativeApp.ReadFile(Path.Combine("textures", "spaceship_color.tga"))))
            {
                texture = parser.Parse();
            }
            ShaderProgram vert = new ShaderProgram(nativeApp, "test.vert");
            ShaderProgram frag = new ShaderProgram(nativeApp, "test.frag");

            using (var taskRunner = new TaskRunner(logger))
            using (var host = new Host(
                nativeApp: nativeApp,
                applicationName: "Test",
                applicationVersion: 1,
                logger: logger))
            using (var window = host.CreateWindow(
                windowSize: (x: 800, y: 600),
                scene: new RenderScene(
                    clearColor: ColorUtils.Yellow,
                    renderobjects: new [] { new RenderObject(mesh, texture, vert, frag) })))
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