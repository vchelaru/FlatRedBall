using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;
using FlatRedBall.Glue.FormHelpers;

namespace FlatRedBall.Glue.Controls
{
    public partial class GroupExportForm : Form
    {
        #region Fields

        TreeNode mScreensTreeNode;
        TreeNode mEntitiesTreeNode;
        char[] splitChars = new char[] { '\\', '/' };

        #endregion

        public IEnumerable<IElement> SelectedElements
        {
            get
            {
                foreach (TreeNode node in ToExportTreeView.Nodes)
                {
                    yield return node.Tag as IElement;

                }
            }
        }

        public bool HasSelectedElements
        {
            get
            {
                return ToExportTreeView.Nodes.Count != 0;
            }
        }

        public GroupExportForm()
        {
            InitializeComponent();

            FillAllAvailableTreeView();

        }

        private void FillAllAvailableTreeView()
        {
            TreeView treeView = this.AllElementsTreeView;

            mScreensTreeNode = new TreeNode("Screens");
            treeView.Nodes.Add(mScreensTreeNode);

            foreach (ScreenSave screenSave in ObjectFinder.Self.GlueProject.Screens)
            {
                AddTreeNodeFor(screenSave);
            }

            mEntitiesTreeNode = new TreeNode("Entities");
            treeView.Nodes.Add(mEntitiesTreeNode);

            foreach (EntitySave entitySave in ObjectFinder.Self.GlueProject.Entities)
            {
                AddTreeNodeFor(entitySave);
            }

            PerformDeepSort(treeView.Nodes);
        }

        private void PerformDeepSort(TreeNodeCollection treeNodeCollection)
        {
            treeNodeCollection.SortByTextConsideringDirectories();

            foreach (TreeNode treeNode in treeNodeCollection)
            {
                PerformDeepSort(treeNode.Nodes);

            }

        }

        private void AddTreeNodeFor(EntitySave entitySave)
        {
            string fullName = entitySave.Name;

            string directory = FileManager.GetDirectory(fullName, RelativeType.Relative);

            TreeNode directoryNode = GetOrCreateDirectoryNode(directory, AllElementsTreeView.Nodes);
            TreeNode elementNode = new TreeNode(FileManager.RemovePath(entitySave.Name));
            elementNode.Tag = entitySave;
            directoryNode.Nodes.Add(elementNode);
        }

        private void AddTreeNodeFor(ScreenSave screenSave)
        {
            string fullName = screenSave.Name;

            string directory = FileManager.GetDirectory(fullName, RelativeType.Relative);

            TreeNode directoryNode = GetOrCreateDirectoryNode(directory, AllElementsTreeView.Nodes);
            TreeNode elementNode = new TreeNode(FileManager.RemovePath(screenSave.Name));
            elementNode.Tag = screenSave;
            directoryNode.Nodes.Add(elementNode);
        }
        
        private TreeNode GetOrCreateDirectoryNode(string directory, TreeNodeCollection treeNodeCollection)
        {
            string[] splits = directory.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
            string currentCategory = splits[0];

            TreeNode foundNode = null;

            foreach (TreeNode subNode in treeNodeCollection)
            {
                if (subNode.Text == currentCategory)
                {
                    foundNode = subNode;
                    break;
                }
            }

            if (foundNode == null)
            {
                foundNode = new TreeNode();
                foundNode.Text = currentCategory;
                foundNode.ForeColor = Color.DarkOrange;
                treeNodeCollection.Add(foundNode);
            }

            if (splits.Length == 1)
            {
                return foundNode;
            }
            else
            {
                int firstSlash = directory.IndexOfAny(splitChars);
                string subString = directory.Substring(firstSlash + 1);

                return GetOrCreateDirectoryNode(subString, foundNode.Nodes);
            }


            throw new NotImplementedException();
        }

        private void AllElementsTreeView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            TreeNode node = AllElementsTreeView.GetNodeAt(e.X, e.Y);

            if (node != null && node.Tag != null)
            {
                node.Parent.Nodes.Remove(node);
                AddAsSelectedElement(node.Tag as IElement);
            }
        }

        private void AddAsSelectedElement(IElement element)
        {
            TreeNode node = new TreeNode();
            node.Tag = element;
            node.Text = element.Name;
            ToExportTreeView.Nodes.Add(node);
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }


    }
}
