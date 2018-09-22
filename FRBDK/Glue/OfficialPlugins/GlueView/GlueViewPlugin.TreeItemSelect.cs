using System.ComponentModel.Composition;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.Interfaces;

namespace OfficialPlugins.GlueView
{
    [Export(typeof(ITreeItemSelect))]
    public partial class GlueViewPlugin : ITreeItemSelect
    {
        public void ReactToItemSelect(TreeNode selectedTreeNode)
        {
            _selectionInterface.UpdateSelectedNode(false);
            gview2SelectionInterface.UpdateSelectedNode();
        }
    }
}
