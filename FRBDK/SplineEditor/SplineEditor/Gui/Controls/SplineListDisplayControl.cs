using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Math.Splines;
using SplineEditor.Gui.Displayers;
using SplineEditor.Commands;
using SplineEditor.States;
using ToolTemplate.Entities;

namespace SplineEditor.Gui.Controls
{
    public partial class SplineListDisplayControl : UserControl
    {

        #region Fields

        bool mSuppressSelectionEvents;

        SplinePointDisplayer mSplinePointDisplayer;
        SplineDisplayer mSplineDisplayer;

        string[] VelocityTypeOptions = new string[]
        {
            "Use Spline Velocity",
            "Use Fixed Velocity:"
        };

        #endregion

        #region Properties

        public List<FlatRedBall.Math.Splines.Spline> Splines { get; set; }

        public FlatRedBall.Math.Splines.Spline SelectedSpline 
        {
            get
            {
                if (TreeView.SelectedNode == null)
                {
                    return null;
                }
                else
                {
                    return TreeView.SelectedNode.Tag as Spline;
                }
            }
            set
            {
                if (SelectedSpline != value)
                {
                    if (value == null)
                    {
                        TreeView.SelectedNode = null;
                    }
                    else
                    {
                        TreeNode found = null;
                        TreeView.SelectedNode = null;
                        foreach (TreeNode candidate in TreeView.Nodes)
                        {
                            if (candidate.Tag == value)
                            {
                                TreeView.SelectedNode = candidate;
                                break;
                            }
                        }
                    }

                    UpdatePropertyGrid();
                }
            }
        }

        public SplinePoint SelectedSplinePoint
        {
            get
            {
                if (TreeView.SelectedNode == null)
                {
                    return null;
                }
                else
                {
                    return TreeView.SelectedNode.Tag as SplinePoint;
                }
            }
            set
            {
                if (value != SelectedSplinePoint)
                {
                    if (value == null)
                    {
                        if (SelectedSplinePoint != null)
                        {
                            TreeView.SelectedNode = null;
                        }
                    }
                    else
                    {
                        TreeNode found = null;
                        TreeView.SelectedNode = null;
                        foreach (TreeNode container in TreeView.Nodes)
                        {
                            foreach (TreeNode subnode in container.Nodes)
                            {
                                if (subnode.Tag == value)
                                {
                                    TreeView.SelectedNode = subnode;
                                    break;
                                }
                            }
                        }
                    }

                    UpdatePropertyGrid();
                }

            }
        }

        #endregion

        #region Events

        public event EventHandler SplineSelect;
        public event EventHandler SplinePointSelect;


        #endregion

        public SplineListDisplayControl()
        {
            InitializeComponent();

            InitializeVelocityTypeComboBox();

            mSplineDisplayer = new SplineDisplayer();
            mSplinePointDisplayer = new SplinePointDisplayer();

            mSplineDisplayer.RefreshOnTimer = false;
            mSplinePointDisplayer.RefreshOnTimer = false;
        }

        private void InitializeVelocityTypeComboBox()
        {
            for(int i = 0; i < VelocityTypeOptions.Length; i++)
            {

                this.VelocityTypeComboBox.Items.Add(VelocityTypeOptions[i]);
            }

            int index = (int)AppState.Self.Preview.PreviewVelocityType;

            this.VelocityTypeComboBox.Text = VelocityTypeOptions[index];
        }


        internal void UpdateListDisplay()
        {
            mSuppressSelectionEvents = true;

            object lastSelection = null;
            if (TreeView.SelectedNode != null)
            {
                lastSelection = TreeView.SelectedNode.Tag;
            }

            TreeView.Nodes.Clear();
            if (this.Splines != null)
            {
                foreach (var item in this.Splines)
                {
                    TreeNode newNode = CreateTreeNodeFor(item);
                    TreeView.Nodes.Add(newNode);
                }
            }

            if (lastSelection is Spline)
            {
                this.SelectedSpline = lastSelection as Spline;
            }
            else if (lastSelection is SplinePoint)
            {
                this.SelectedSplinePoint = lastSelection as SplinePoint;
            }

            mSuppressSelectionEvents = false;
        }

        internal void UpdateListDisplay(Spline spline)
        {
            var foundNode = GetTreeNodeFor(spline);

            if (foundNode != null)
            {
                UpdateSplineTreeNode(spline, foundNode);

            }
        }

        internal void UpdateListDisplay(SplinePoint point)
        {
            var foundNode = GetTreeNodeFor(point);
            if (foundNode != null)
            {
                foundNode.Text = TextForPoint(point);
            }
        }

        private static TreeNode CreateTreeNodeFor(Spline item)
        {
            TreeNode newNode = new TreeNode(item.Name);
            UpdateSplineTreeNode(item, newNode);

            return newNode;
        }

        private static void UpdateSplineTreeNode(Spline item, TreeNode treeNode)
        {
            // I know, crazy, but this can really slow performance
            // with large tree nodes
            if (treeNode.Text != item.Name)
            {
                treeNode.Text = item.Name;
            }

            treeNode.Tag = item;
            if (treeNode.Nodes.Count != 0)
            {
                treeNode.Nodes.Clear();
            }

            foreach (var point in item)
            {
                TreeNode pointNode = new TreeNode(TextForPoint(point));
                pointNode.Tag = point;
                treeNode.Nodes.Add(pointNode);
            }
        }

        private static string TextForPoint(SplinePoint point)
        {
            return "Time: " + point.Time.ToString();
        }

        private TreeNode GetTreeNodeFor(Spline spline)
        {
            foreach (TreeNode splineTreeNode in TreeView.Nodes)
            {
                if (splineTreeNode.Tag == spline)
                {
                    return splineTreeNode;
                }
            }
            return null;
        }

        private TreeNode GetTreeNodeFor(SplinePoint splinePoint)
        {
            foreach(TreeNode splineTreeNode in TreeView.Nodes)
            {
                foreach(TreeNode splinePointTreeNode in splineTreeNode.Nodes)
                {
                    if(splinePointTreeNode.Tag == splinePoint)
                    {
                        return splinePointTreeNode;
                    }
                }
            }
            return null;
        }

        private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (!mSuppressSelectionEvents)
            {
                if (SelectedSpline != null && SplineSelect != null)
                {
                    SplineSelect(this, null);

                }

                if (SelectedSplinePoint != null && SplinePointSelect != null)
                {
                    SplinePointSelect(this, null);

                }
                UpdatePropertyGrid();
            }
        }

        private void UpdatePropertyGrid()
        {
            if (SelectedSpline != null)
            {
                //GuiData.PropertyGrid.SelectedObject = mSplineDisplayer ;
                mSplineDisplayer.PropertyGrid = this.PropertyGrid;
                mSplineDisplayer.Instance = SelectedSpline;
                PropertyGrid.Refresh();
            }
            else if (SelectedSplinePoint != null)
            {
                mSplinePointDisplayer.PropertyGrid = PropertyGrid;
                mSplinePointDisplayer.Instance = SelectedSplinePoint;
                PropertyGrid.Refresh();
            }
        }

        private void PreviewButton_Click(object sender, EventArgs e)
        {
            AppCommands.Self.Preview.CreateSplineCrawler();
        }

        private void VelocityTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = VelocityTypeComboBox.SelectedIndex;

            AppState.Self.Preview.PreviewVelocityType = (PreviewVelocityType)index;

            UpdateVelocityTextBox();
        }

        private void UpdateVelocityTextBox()
        {
            if (AppState.Self.Preview.PreviewVelocityType == PreviewVelocityType.UseSplineVelocity)
            {
                VelocityTextBox.Visible = false;
            }
            else
            {
                VelocityTextBox.Visible = true;
                VelocityTextBox.Text = AppState.Self.Preview.ConstantPreviewVelocity.ToString();
            }
        }

        private void VelocityTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void ApplyValueFromTextBox()
        {
            float valueBefore = AppState.Self.Preview.ConstantPreviewVelocity;

            float newValue = valueBefore;

            if (float.TryParse(VelocityTextBox.Text, out newValue))
            {
                AppState.Self.Preview.ConstantPreviewVelocity = newValue;
            }
            else
            {
                VelocityTextBox.Text = AppState.Self.Preview.ConstantPreviewVelocity.ToString();
            }
        }

        private void VelocityTextBox_Leave(object sender, EventArgs e)
        {
            ApplyValueFromTextBox();

        }

        private void VelocityTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyValueFromTextBox();

            }
        }
    }
}
