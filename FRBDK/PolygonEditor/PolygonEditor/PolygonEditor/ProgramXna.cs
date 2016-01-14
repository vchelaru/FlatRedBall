using System;
using System.Windows.Forms;

namespace PolygonEditorXna
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
            try
            {
                using (Game1 game = new Game1())
                {
                    game.Run();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }
    }
}

