using System;
using System.Collections.Generic;
using System.Windows.Forms;

using AIEditor.Gui;

namespace AIEditor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            bool showPopupOnException = false;

            if (showPopupOnException)
            {
                try
                {
                    Form1 frm = new Form1();
                    if (frm.IsDisposed)
                        return;

                    frm.Run(args);
                }
                catch (Exception e)
                {
                    System.Windows.Forms.MessageBox.Show(e.ToString());
                }
            }
            else
            {
                Form1 frm = new Form1();
                if (frm.IsDisposed)
                    return;

                frm.Run(args);
            }
        }
    }
}