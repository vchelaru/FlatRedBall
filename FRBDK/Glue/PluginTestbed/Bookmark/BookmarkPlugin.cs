using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.Windows.Forms;
using FlatRedBall.Glue;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins;

namespace PluginTestbed.Bookmark
{
    [Export(typeof(ITreeViewRightClick)), Export(typeof(ILeftTab))]
    public class BookmarkPlugin : FlatRedBall.Glue.Plugins.Interfaces.ITreeViewRightClick, ILeftTab
    {
        ucBookmark mControl;
        PluginTab mTab;
        TabControl mTabControl;

        #region ITreeViewRightClick Members

        public void ReactToRightClick(TreeNode rightClickedTreeNode, ContextMenuStrip menuToModify)
        {
            ToolStripMenuItem toolStripItem = new ToolStripMenuItem("Bookmark");
            toolStripItem.Click += new EventHandler(BookmarkClick);

            menuToModify.Items.Add(toolStripItem);
        }
        void BookmarkClick(object sender, EventArgs e)
        {
            mControl.AddItem(
                EditorLogic.CurrentTreeNode);
        }
        #endregion

        
        #region IPlugin Members

        public string FriendlyName
        {
            get { return "Bookmark"; }
        }

        public Version Version
        {
            get { return new Version(1,0); }
        }

        public void StartUp()
        {
            
        }

        public bool ShutDown(PluginShutDownReason shutDownReason)
        {
            if (mTab != null)
            {
                mTabControl.Controls.Remove(mTab);
            }
            mTabControl = null;
            mTab = null;
            mControl = null;

            return true;
        }

        #endregion



        public void InitializeTab(TabControl tabControl)
        {
            mControl = new ucBookmark();
            mTab = new PluginTab();
            mTabControl = tabControl;

            mTab.ClosedByUser += new PluginTab.ClosedByUserDelegate(mTab_ClosedByUser);

            mTab.Text = "  Bookmarks";
            mTab.Controls.Add(mControl);
            mControl.Dock = DockStyle.Fill;

            mTabControl.Controls.Add(mTab);
        }

        void mTab_ClosedByUser(object sender)
        {
            PluginManager.ShutDownPlugin(this);
        }
    }
}
