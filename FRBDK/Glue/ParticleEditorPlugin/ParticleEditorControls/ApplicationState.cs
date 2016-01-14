using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParticleEditorControls.Managers;
using System.Windows.Forms;
using FlatRedBall.Content.Particle;

namespace ParticleEditorControls
{
    public class ApplicationState : Singleton<ApplicationState>
    {
        public TreeNode SelectedTreeNode
        {
            get
            {
                return TreeViewManager.Self.SelectedTreeNode;
            }
            set
            {
                TreeViewManager.Self.SelectedTreeNode = value;
            }
        }

        public EmitterSave SelectedEmitterSave
        {
            get
            {
                var treeNode = SelectedTreeNode;

                if (treeNode != null && treeNode.Tag is EmitterSave)
                {
                    return treeNode.Tag as EmitterSave;
                }
                return null;
            }
            set
            {
                TreeNode treeNode = TreeViewManager.Self.GetTreeNodeFor(value);

                SelectedTreeNode = treeNode;

            }
        }
    }
}
