using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Content.Particle;

namespace ParticleEditorControls.Managers
{
    public class TreeViewManager : Singleton<TreeViewManager>
    {
        #region Fields

        TreeView mTreeView;

        #endregion

        #region Properties

        public TreeNode SelectedTreeNode
        {
            get
            {
                return mTreeView.SelectedNode;
            }
            set
            {
                mTreeView.SelectedNode = value;
            }
        }

        #endregion

        public void Initialize(TreeView treeView)
        {
            mTreeView = treeView;

            mTreeView.AfterSelect += new TreeViewEventHandler(HandleTreeNodeSelect);
        }

        void HandleTreeNodeSelect(object sender, TreeViewEventArgs e)
        {
            PropertyGridManager.Self.RefreshAll();
        }


        public void RefreshTreeView()
        {
            if (ProjectManager.Self.EmitterSaveList != null)
            {
                EmitterSaveList esl = ProjectManager.Self.EmitterSaveList;

                for (int i = 0; i < esl.emitters.Count; i++)
                {
                    EmitterSave es = esl[i];

                    if (GetTreeNode(es) == null)
                    {
                        CreateTreeNodeFor(es);
                    }
                }
            }

            // Check for removals
            for (int i = mTreeView.Nodes.Count - 1; i > -1; i--)
            {
                EmitterSave emitterSave = mTreeView.Nodes[i].Tag as EmitterSave;

                if (ProjectManager.Self.EmitterSaveList.emitters.Contains(emitterSave) == false)
                {
                    mTreeView.Nodes.RemoveAt(i);
                }
            }
        }

        public void RefreshTreeViewFor(EmitterSave es)
        {
            var treeNode = GetTreeNode(es);

            if(treeNode != null)
            {
                if(treeNode.Text != es.Name)
                {
                    treeNode.Text = es.Name;
                }
            }
        }

        private void CreateTreeNodeFor(EmitterSave es)
        {
            TreeNode treeNode = new TreeNode(es.Name);
            treeNode.Tag = es;


            mTreeView.Nodes.Add(treeNode);
        }


        private TreeNode GetTreeNode(EmitterSave es)
        {
            foreach (TreeNode node in mTreeView.Nodes)
            {
                if (node.Tag == es)
                {
                    return node;
                }
            }

            return null;
        }






        internal TreeNode GetTreeNodeFor(EmitterSave value)
        {
            if (value == null)
            {
                return null;
            }
            else
            {
                foreach (TreeNode treeNode in mTreeView.Nodes)
                {
                    if (treeNode.Tag == value)
                    {
                        return treeNode;
                    }
                }

                // if we got here then there's no node:
                return null;
            }
        }



    }
}
