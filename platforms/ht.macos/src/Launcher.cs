using System;

namespace HT.MacOS
{
    public static class Launcher
    {
        static void Main(string[] args)
        {
            Console.WriteLine("[MacOS] Launching program..");

            using(var app = new NativeApp())
            {
                HT.Main.Program.Run(app);
            }

            Console.WriteLine("[MacOS] Program terminated");
        }
    }
}
