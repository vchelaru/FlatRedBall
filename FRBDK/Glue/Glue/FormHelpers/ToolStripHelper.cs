using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBall.Glue.FormHelpers
{
    public class ToolStripHelper
    {
        static ToolStripHelper mSelf;

        public static ToolStripHelper Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new ToolStripHelper();
                }
                return mSelf;
            }
        }

        public ToolStripMenuItem GetItem(MenuStrip menuStrip, string name)
        {
            return menuStrip?.Items.Cast<ToolStripMenuItem>().FirstOrDefault(item => item.Name == name);
        }

    }
}
