using System;

using HT.Engine.Platform;
using HT.Engine.Rendering;
using HT.Engine.Math;

namespace HT.Main
{
    public static class Program
    {
        public static void Run(INativeApp nativeApp)
        {
            bool running = true;

            var window = nativeApp.CreateWindow(size: new Int2(600, 400), minSize: new Int2(150, 150), title: "test");
            window.CloseRequested += () => running = false;

            using(var host = new Host(nativeApp: nativeApp, applicationName: "Test", applicationVersion: 1))
            using(var renderSurface = window.CreateSurface(host))
            using(var renderer = new RenderDevice(host.FindSuitableDevice(renderSurface)))
            {
                Console.WriteLine("Initialized with device: " + renderer.PhysicalDevice.Name);

                int tick = 0;
                while(running)
                {
                    window.Title = (++tick).ToString();
                    nativeApp.Update();
                    System.Threading.Thread.Sleep(100);
                }
            }
        }
    }
}