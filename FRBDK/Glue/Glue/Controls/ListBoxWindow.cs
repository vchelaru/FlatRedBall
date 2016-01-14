using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Controls
{
    public partial class ListBoxWindow : Form
    {
        List<Button> mButtons = new List<Button>();

        public bool ShowCheckBoxes
        {
            get { return TreeView.CheckBoxes; }
            set { TreeView.CheckBoxes = value; }
        }

        public string Message
        {
            get { return DisplayTextLabel.Text; }
            set { DisplayTextLabel.Text = value; }
        }

        public IEnumerable<TreeNode> CheckedTreeNodes
        {
            get
            {
                foreach (TreeNode treeNode in TreeView.Nodes)
                {
                    if (treeNode.Checked)
                    {
                        yield return treeNode;
                    }
                }
            }
        }

        public ListBoxWindow()
        {
            InitializeComponent();

            AddButton("OK", System.Windows.Forms.DialogResult.OK);

            StartPosition = FormStartPosition.Manual;
        }

        public void AddItem(object objectToAdd)
        {
            TreeNode treeNode = new TreeNode(objectToAdd.ToString());
            treeNode.Tag = objectToAdd;
            TreeView.Nodes.Add(treeNode);

            
        }

        public void ClearButtons()
        {
            foreach (Button button in mButtons)
            {
                this.flowLayoutPanel1.Controls.Remove(button);
            }

            mButtons.Clear();

        }

        public void AddButton(string message, DialogResult result)
        {
            Button button = new Button();

            button.Text = message;
            button.DialogResult = result;
            button.Size = new Size(
                flowLayoutPanel1.Width - 3, button.Size.Height);

            this.flowLayoutPanel1.Controls.Add(button);
            mButtons.Add(button);
        }

        private void ListBoxWindow_ResizeEnd(object sender, EventArgs e)
        {
            this.DisplayTextLabel.MaximumSize = new Size(
                this.Width - 20,
                DisplayTextLabel.MaximumSize.Height);
        }

        private void flowLayoutPanel1_Resize(object sender, EventArgs e)
        {
            foreach (Button button in flowLayoutPanel1.Controls)
            {
                button.Width = flowLayoutPanel1.Width - 5;
            }
        }

        protected override void OnShown(EventArgs e)
        {

            Location = new Point(ListBoxWindow.MousePosition.X - this.Width/2, ListBoxWindow.MousePosition.Y - this.Height/2);

            base.OnShown(e);
        }
    }
}
