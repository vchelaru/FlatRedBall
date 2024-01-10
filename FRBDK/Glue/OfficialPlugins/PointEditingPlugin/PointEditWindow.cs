using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace OfficialPlugins.PointEditingPlugin
{
    public partial class PointEditWindow : UserControl
    {
        #region Fields

        // We can't serialize a list of points - see InstructionSave.cs for info on why not
        List<Vector2> mData;

        #endregion

        #region Properties


        public List<Vector2> Data
        {
            get
            {
                return mData;
            }
            set
            {
                mData = value;

                UpdateToData();
            }
        }

        Vector2 SelectedVector2
        {
            get
            {
                if (TreeView.SelectedNode != null)
                {
                    return (Vector2)TreeView.SelectedNode.Tag;
                }
                else
                {
                    return Vector2.Zero;
                }
            }
        }

        #endregion

        #region Events

        public event EventHandler DataChanged;


        #endregion

        #region Methods


        private void UpdateToData()
        {
            int index = -1;
            if (TreeView.SelectedNode != null)
            {
                index = TreeView.SelectedNode.Index;
            }

            this.TreeView.Nodes.Clear();

            if (mData != null)
            {
                foreach (var item in mData)
                {
                    TreeNode treeNode = new TreeNode(
                        string.Format("({0}, {1})", item.X, item.Y));
                    treeNode.Tag = item;
                    this.TreeView.Nodes.Add(treeNode);
                }
            }

            if (index > -1 && index < TreeView.Nodes.Count)
            {
                TreeView.SelectedNode = TreeView.Nodes[index];
            }
        }

        public PointEditWindow()
        {
            InitializeComponent();
        }

        private void AddPointButton_Click(object sender, EventArgs e)
        {
            if (Data != null)
            {
                Data.Add(new Vector2());

                UpdateToData();

                this.TreeView.SelectedNode = this.TreeView.Nodes[this.TreeView.Nodes.Count - 1];


                CallDataChanged();
            }

        }

        private void XTextBox_TextChanged(object sender, EventArgs e)
        {
            float outValue = 0;

            if (TreeView.SelectedNode != null && float.TryParse(XTextBox.Text, out outValue))
            {
                int index = this.TreeView.SelectedNode.Index;

                if (index != -1)
                {
                    Vector2 vector = SelectedVector2;
                    if (outValue != vector.X)
                    {
                        vector.X = outValue;

                        Data[index] = vector;
                        UpdateToData();

                        CallDataChanged();

                    }
                }
            }
        }

        private void YTextBox_TextChanged(object sender, EventArgs e)
        {
            float outValue = 0;

            if (TreeView.SelectedNode != null && float.TryParse(YTextBox.Text, out outValue))
            {
                int index = this.TreeView.SelectedNode.Index;

                if (index != -1)
                {
                    Vector2 vector = SelectedVector2;
                    if (outValue != vector.Y)
                    {
                        vector.Y = outValue;

                        Data[index] = vector;
                        UpdateToData();

                        CallDataChanged();

                    }
                }
            }
        }

        private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (TreeView.SelectedNode != null)
            {
                if (!XTextBox.Focused)
                {
                    XTextBox.Text = SelectedVector2.X.ToString();
                }
                if (!YTextBox.Focused)
                {
                    YTextBox.Text = SelectedVector2.Y.ToString();
                }
            }
            else
            {
                XTextBox.Text = null;
                YTextBox.Text = null;
            }
        }

        private void CallDataChanged()
        {
            if (DataChanged != null)
            {
                DataChanged(this, null);
            }
        }

        #endregion

        private void RemovePointButton_Click(object sender, EventArgs e)
        {
            if (this.Data != null && this.TreeView.SelectedNode != null)
            {
                int indexToRemove = TreeView.SelectedNode.Index;

                this.Data.RemoveAt(indexToRemove);

                UpdateToData();

                CallDataChanged();

            }
        }
    }
}
