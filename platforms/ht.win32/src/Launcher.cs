using System;
using System.Threading;

using HT.Engine.Utils;

namespace HT.Win32
{
    public static class Launcher
    {
        static void Main(string[] args)
        {
            //Give a name to the main-thread (nice for debugging purposes)
            Thread.CurrentThread.Name = "Win32-Main";

            Logger logger = new Logger();
            logger.Log($"Win32-{nameof(Launcher)}", "Launching program"); 

            using (var app = new NativeApp(logger))
            {
                HT.Main.Program.Run(app, logger);
            }

            logger.Log($"Win32-{nameof(Launcher)}", "Program terminated"); 
        }
    }
}
