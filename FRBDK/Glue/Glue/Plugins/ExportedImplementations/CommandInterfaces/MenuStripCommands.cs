using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    public static class MenuStripExtensionMethods
    {
        public static ToolStripMenuItem GetItem(this MenuStrip menuStrip, string name)
        {
            foreach (ToolStripMenuItem item in menuStrip.Items)
            {
                if (item.Text == name)
                {
                    return item;
                }
            }
            return null;

        }
    }
}
