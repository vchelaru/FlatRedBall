using System;
using System.ComponentModel.Composition;
using FlatRedBall.Glue;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;

namespace PluginTestbed.SceneProgramChooser
{
    [Export(typeof(ITreeViewRightClick))]
    public partial class SceneProgramChooserPlugin : ITreeViewRightClick
	{
        public void ReactToRightClick(System.Windows.Forms.TreeNode rightClickedTreeNode, System.Windows.Forms.ContextMenuStrip menuToModify)
        {
            //if (!rightClickedTreeNode.IsReferencedFile() ||
            //    FileManager.GetExtension(((ReferencedFileSave) rightClickedTreeNode.Tag).Name) != "scnx") return;
            //menuToModify.Items.Add("-");
            //menuToModify.Items.Add("Open with Sprite Editor").Click += OnOpenWithSpriteEditorClick;
            //menuToModify.Items.Add("Open with Tile Editor").Click += OnOpenWithTileEditorClick;
        }



	}
}
