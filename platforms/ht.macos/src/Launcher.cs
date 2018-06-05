using System;

namespace HT.MacOS
{
    public static class Launcher
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var prog = new HT.Main.Program();
            //Logic here :)
            prog.Dispose();
        }
    }
}
