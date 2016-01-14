using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Performance.Measurement;

namespace FlatRedBallProfiler
{
    public class ApplicationState : Singleton<ApplicationState>
    {
        TreeView mTreeView;

        public Section CurrentSection
        {
            get
            {
                if (CurrentTreeNode != null)
                {
                    return CurrentTreeNode.Tag as Section;
                }
                else
                {
                    return null;
                }
            }
        }

        public TreeNode CurrentTreeNode
        {
            get
            {
                return mTreeView.SelectedNode;
            }
        }

        public void Initialize(TreeView treeView)
        {
            mTreeView = treeView;
        }

    }
}
