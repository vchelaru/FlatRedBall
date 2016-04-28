using System;

namespace GlueTestProject
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args2)
        {
            using (Game1 game = new Game1())
            {
#if UNIT_TESTS
                AppDomain.CurrentDomain.UnhandledException
                    += delegate(object sender, UnhandledExceptionEventArgs args)
                           {
                               var exception = (Exception) args.ExceptionObject;
                               Console.Error.Write("Unhandled exception: " + exception);
                               Environment.Exit(1);
                           };
#endif
                game.Run();
            }
        }
    }
#endif
}

