using System;
using System.Windows.Forms;

namespace GlueView
{
    public static class Program
    {
        public static string[] CommandLineArgs;


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            CommandLineArgs = args;
            try
            {
                using (Game1 game = new Game1())
                {

                    game.Run();


                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Error in GlueView:");
            }
        }
    }
}

