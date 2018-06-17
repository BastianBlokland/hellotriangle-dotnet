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
            using(var host = new Host(nativeApp: nativeApp, applicationName: "Test", applicationVersion: 1, logger: logger))
            using(var window = host.CreateWindow(new Int2(800, 600), new RenderScene(clearColor: ColorUtils.Olive)))
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