using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using FlatRedBall.Performance.Measurement;

namespace FlatRedBallProfiler.Managers
{
    #region ViewMode

    public enum ViewMode
    {
        Expaned,
        Collapsed
    }

    #endregion

    public class TreeViewManager
    {
        static TreeViewManager mSelf;
        TreeView mTreeView;
        ViewMode mViewMode;

        public static TreeViewManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new TreeViewManager();
                }
                return mSelf;
            }
        }

        public ViewMode ViewMode
        {
            get
            {
                return mViewMode;
            }
            set
            {
                mViewMode = value;
                RefreshUI();
            }
        }

        Section SectionBasedOnViewMode
        {
            get
            {
                if (this.ViewMode == Managers.ViewMode.Expaned)
                {
                    return ProjectManager.Self.Section;
                }
                else
                {
                    return ProjectManager.Self.MergedSection;
                }
            }
        }

        public void Initialize(TreeView treeView)
        {
            mTreeView = treeView;
        }


        public void RefreshUI()
        {
            mTreeView.Nodes.Clear();


            TreeNode newNode = CreateTreeNode(SectionBasedOnViewMode);

            AppendTimeInMilliseconds(newNode);

            
            mTreeView.Nodes.Add(newNode);

            AddTreeNodesFor(SectionBasedOnViewMode, newNode.Nodes);
            




        }

        private void AddTreeNodesFor(FlatRedBall.Performance.Measurement.Section section, TreeNodeCollection treeNodeCollection)
        {
            TreeNode mostExpensive = null;
            float mostExpensiveValue = -1; // so it is always smaller than even the smallest starting value:
            double timeOfSubSections = 0;

            List<TreeNode> list = new List<TreeNode>();
            // This may be a revisited node:
            foreach (TreeNode node in treeNodeCollection)
            {
                list.Add(node);
            }

            foreach (var subSection in section.Children)
            {
                TreeNode treeNode = null;


                treeNode = CreateTreeNode(subSection);

                if (subSection.Time > mostExpensiveValue)
                {
                    mostExpensive = treeNode;
                    mostExpensiveValue = subSection.Time;
                }

                timeOfSubSections += subSection.Time;

                list.Add(treeNode);
                
                // adding tree nodes will just add times to existing
                // tree nodes if they already exist
                AddTreeNodesFor(subSection, treeNode.Nodes);

            }

            if (mostExpensive != null)
            {
                AppendTimeInMilliseconds(mostExpensive);
            }

            if(section.Time != 0)
            {
                double ratioAccountedFor = timeOfSubSections / section.Time;

                if (ratioAccountedFor < .98)
                {
                    TreeNode node = new TreeNode();
                    node.Text = "???? " + (100 - ratioAccountedFor*100).ToString("0.00") + "%";
                    list.Insert(0, node);
                }
            }
            treeNodeCollection.AddRange(list.ToArray());

        }

        private static void AppendTimeInMilliseconds(TreeNode mostExpensive)
        {
            mostExpensive.BackColor = Color.Orange;
            float time = ((Section)mostExpensive.Tag).Time;
            time *= 1000;
            mostExpensive.Text += " (" + time.ToString("0.00") + " ms)";
        }

        private static TreeNode CreateTreeNode(FlatRedBall.Performance.Measurement.Section subSection)
        {
            TreeNode treeNode = new TreeNode();
            treeNode.Tag = subSection;

            SetTreeNodeText(subSection, treeNode);
            return treeNode;
        }

        private static void SetTreeNodeText(FlatRedBall.Performance.Measurement.Section subSection, TreeNode treeNode)
        {
            string percentage = "100%";

            if (subSection.Parent != null)
            {
                float ratio = subSection.Time / subSection.Parent.Time;

                percentage = (ratio * 100).ToString("0.00") + "%";

            }
            string text = subSection.Name + " " + percentage;

            treeNode.Text = text;
        }
    }
}
