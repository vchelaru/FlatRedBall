using System;

namespace ToolTemplate
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //try
            {
                using (Game1 game = new Game1(args))
                {
                    game.Run();
                }
            }
            //catch (Exception e)
            {
                //System.Windows.Forms.MessageBox.Show(e.ToString());

            }
        }
    }
}

