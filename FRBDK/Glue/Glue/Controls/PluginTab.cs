using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins;

namespace FlatRedBall.Glue.Controls
{
    public class PluginTab : TabPage
    {
        public delegate void ClosedByUserDelegate(object sender);
        public event ClosedByUserDelegate ClosedByUser;

        bool mDrawX = true;

        MenuItem moveToMenuItem;

        public TabControl LastTabControl { get; set; }

        TabControl ParentTabControl
        {
            get
            {
                return Parent as TabControl;
            }
        }

        public DateTime LastTimeClicked
        {
            get;
            set;
        }

        public bool DrawX
        {
            get { return mDrawX; }
            set { mDrawX = value; }
        }


        public PluginTab() : base()
        {
            this.ContextMenu = new ContextMenu();

            moveToMenuItem = this.ContextMenu.MenuItems.Add("MoveTo");

        }

        public void RefreshMoveToCommands()
        {
            moveToMenuItem.MenuItems.Clear();
            if (ParentTabControl != PluginManager.LeftTab)
            {
                moveToMenuItem.MenuItems.Add("Left Tab", HandleMoveToLeftTab);
            }

            if (ParentTabControl != PluginManager.RightTab)
            {
                moveToMenuItem.MenuItems.Add("Right Tab", HandleMoveToRightTab);
            }

            if (ParentTabControl != PluginManager.TopTab)
            {
                moveToMenuItem.MenuItems.Add("Top Tab", HandleMoveToTopTab);
            }

            if (ParentTabControl != PluginManager.BottomTab)
            {
                moveToMenuItem.MenuItems.Add("Bottom Tab", HandleMoveToBottomTab);
            }

            if (ParentTabControl != PluginManager.CenterTab)
            {
                moveToMenuItem.MenuItems.Add("Center Tab", HandleMoveToCenterTab);
            }
        }

        private void HandleMoveToLeftTab(object sender, EventArgs e)
        {
            ParentTabControl.TabPages.Remove(this);
            FlatRedBall.Glue.Plugins.PluginManager.LeftTab.TabPages.Add(this);
        }

        private void HandleMoveToRightTab(object sender, EventArgs e)
        {
            ParentTabControl.TabPages.Remove(this);
            FlatRedBall.Glue.Plugins.PluginManager.RightTab.TabPages.Add(this);

        }

        private void HandleMoveToTopTab(object sender, EventArgs e)
        {
            ParentTabControl.TabPages.Remove(this);
            FlatRedBall.Glue.Plugins.PluginManager.TopTab.TabPages.Add(this);

        }

        private void HandleMoveToBottomTab(object sender, EventArgs e)
        {
            ParentTabControl.TabPages.Remove(this);
            FlatRedBall.Glue.Plugins.PluginManager.BottomTab.TabPages.Add(this);

        }

        private void HandleMoveToCenterTab(object sender, EventArgs e)
        {
            ParentTabControl.TabPages.Remove(this);
            FlatRedBall.Glue.Plugins.PluginManager.CenterTab.TabPages.Add(this);

        }



        

        public void CloseTabByUser()
        {
            if (ClosedByUser != null)
                ClosedByUser(this);
        }
    }
}
