using System.Windows.Forms;
using System;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.Interfaces;

namespace PluginTestbed.Bookmark
{
    public partial class ucBookmark : UserControl
    {
        public ucBookmark()
        {
            InitializeComponent();
        }

        public void AddItem(TreeNode node)
        {
            lbBookmarks.Items.Add(node);
        }

        private void lbBookmarks_SelectedIndexChanged(object sender, EventArgs e)
        {
            ElementViewWindow.SelectedNode = lbBookmarks.SelectedItem as TreeNode;
        }

        private void lbBookmarks_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                lbBookmarks.Items.Remove(lbBookmarks.SelectedItem);
            }
        }
    }
}
