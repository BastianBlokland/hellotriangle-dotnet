using System;

using HT.Engine.Utils;

namespace HT.Win32
{
    public static class Launcher
    {
        static void Main(string[] args)
        {
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
