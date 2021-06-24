using System;

namespace FlatRedBallDesktopGlTemplate
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (var game = new Game1())
            {
                try
                {
                    game.Run();
                }
                catch (Exception e)
                {
                    System.IO.File.WriteAllText("CrashInfo.txt", e.ToString());
                    throw;
                }

            }
        }
    }
}
