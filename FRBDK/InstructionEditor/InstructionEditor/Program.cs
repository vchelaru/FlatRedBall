using System;

namespace InstructionEditor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            //try
            //{
                using (Game1 game = new Game1())
                {
                    game.Run();
                }
            //}
            //catch (Exception e)
            //{
            //    System.Windows.Forms.MessageBox.Show(e.ToString());

            //}
        }
    }
}

