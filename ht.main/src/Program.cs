using System;

using HT.Engine.Platform;
using HT.Engine.Math;

namespace HT.Main
{
    public static class Program
    {
        public static void Run(INativeApp nativeApp)
        {
            bool running = true;

            var window = nativeApp.CreateWindow(new Int2(600, 400), "test");
            window.CloseRequested += () => running = false;

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