using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Glue;
using FlatRedBall.Glue.Events;
using System.Windows.Forms;

namespace FlatRedBall.Glue.FormHelpers
{
    public class VisibilityManager
    {
        private static void RemoveTabFromMain(TabPage tabPage)
        {
            TabControl tabControl = MainGlueWindow.Self.MainTabControl;
            if (tabControl.Controls.Contains(tabPage))
            {
                tabControl.Controls.Remove(tabPage);
            }
        }

        private static void AddTabToMain(TabPage tabPage)
        {
            TabControl tabControl = MainGlueWindow.Self.MainTabControl;
            if (!tabControl.Controls.Contains(tabPage))
            {
                tabControl.Controls.Add(tabPage);
            }

        }



    }
}
