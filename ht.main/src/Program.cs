using System;

using HT.Engine.Platform;
using HT.Engine.Rendering;
using HT.Engine.Math;
using HT.Engine.Utils;

namespace HT.Main
{
    public static class Program
    {
        public static void Run(INativeApp nativeApp, Logger logger = null)
        {
            ShaderProgram vert = new ShaderProgram(nativeApp, "test.vert");
            ShaderProgram frag = new ShaderProgram(nativeApp, "test.frag");

            using(var host = new Host(
                nativeApp: nativeApp,
                applicationName: "Test",
                applicationVersion: 1,
                logger: logger))
            using(var window = host.CreateWindow(
                windowSize: new Int2(800, 600),
                scene: new RenderScene(
                    clearColor: ColorUtils.Black,
                    vertProg: vert,
                    fragProg: frag)))
            {
                bool running = true;
                window.CloseRequested += () => running = false;

                while(running)
                {
                    nativeApp.Update();
                    window.Draw();
                }
            }
        }
    }
}