using System.ComponentModel.Composition;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.Interfaces;

namespace PluginTestbed.StateChains
{
    [Export(typeof(ITreeItemSelect))]
	public partial class StateChainsPlugin : ITreeItemSelect
	{
        public void ReactToItemSelect(TreeNode selectedTreeNode)
        {
            _control.CurrentEntitySave = GlueCommands.TreeNodeCommands.GetSelectedEntitySave();
        }
	}
}
