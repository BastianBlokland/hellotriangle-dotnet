using System;
using System.Threading;

using HT.Engine.Utils;

namespace HT.MacOS
{
    public static class Launcher
    {
        static void Main(string[] args)
        {
            //Give a name to the main-thread (nice for debugging purposes)
            Thread.CurrentThread.Name = "MacOS-Main";

            Logger logger = new Logger();
            logger.Log($"MacOS-{nameof(Launcher)}", "Launching program");

            using (var app = new NativeApp(logger))
            {
                HT.Main.Program.Run(app, logger);
            }

            logger.Log($"MacOS-{nameof(Launcher)}", "Program terminated");
        }
    }
}
