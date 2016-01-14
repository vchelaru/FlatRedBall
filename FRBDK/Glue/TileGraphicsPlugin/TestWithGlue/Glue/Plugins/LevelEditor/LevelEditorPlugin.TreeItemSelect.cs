using System.ComponentModel.Composition;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.Interfaces;

namespace PluginTestbed.LevelEditor
{
    [Export(typeof(ITreeItemSelect))]
    public partial class LevelEditorPlugin : ITreeItemSelect
    {
        public void ReactToItemSelect(TreeNode selectedTreeNode)
        {
            _selectionInterface.UpdateSelectedNode(false);
        }
    }
}
