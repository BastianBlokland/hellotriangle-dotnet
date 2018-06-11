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
            using(var surface = window.CreateSurface(host))
            using(var renderer = new Renderer(host.FindSuitableDevice(surface), surface, window.ClientRect.Size))
            {
                SetTitle(window, renderer);
                
                while(running)
                {
                    nativeApp.Update();

                    if(!window.IsMovingOrResizing && renderer.SurfaceSize != window.ClientRect.Size)
                    {
                        renderer.SetupSwapchain(window.ClientRect.Size);
                        SetTitle(window, renderer);
                    }

                    if(window.Minimized)
                        System.Threading.Thread.Sleep(100);
                    else
                        renderer.Draw();
                }
            }
        }

        private static void SetTitle(INativeWindow window, Renderer renderer)
            => window.Title = $"{renderer.Device.Name} {renderer.SurfaceSize}";
    }
}