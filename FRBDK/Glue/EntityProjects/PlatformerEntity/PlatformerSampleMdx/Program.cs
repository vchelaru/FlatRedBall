using System;
using System.Collections.Generic;
using System.Windows.Forms;


namespace PlatformerSample
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (GameWindow gameWindow = new GameWindow())
            {
                gameWindow.Run();

            }
        }
    }
}