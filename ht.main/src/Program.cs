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
            {
                while(true)
                {
                    nativeApp.Update();
                    System.Threading.Thread.Sleep(100);
                }
            }
        }
    }
}