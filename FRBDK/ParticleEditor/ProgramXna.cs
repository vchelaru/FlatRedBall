using System;

namespace ParticleEditor
{
    static class Program
    {

        internal static string[] CommandLineArguments;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {

            CommandLineArguments = args;

            using (Game1 game = new Game1())
            {
                game.Run();
            }
        }
    }
}

