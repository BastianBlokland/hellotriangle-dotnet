using System;

namespace HT.Win32
{
    public static class Launcher
    {
        static void Main(string[] args)
        {
            Console.WriteLine("[Win32] Launching program..");

            using(var app = new NativeApp())
            {
                HT.Main.Program.Run(app);
            }

            Console.WriteLine("[Win32] Program terminated");
        }
    }
}
