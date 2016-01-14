using System;
using System.Collections.Generic;
using System.Text;

using PolygonEditor.Gui;

namespace PolygonEditor
{
    class Program
    {

        [STAThread]
        static void Main(string[] args)
        {
            using (GameForm gameForm = new GameForm())
            {
                //try
                //{
                    gameForm.Run(args);
                //}
                //catch (Exception e)
                //{
                //    System.Windows.Forms.MessageBox.Show("Error:\n" + e.ToString());
                //}

            }

        }
    }
}
