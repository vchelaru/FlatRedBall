using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;

using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.CreatedClass;
using FlatRedBall.Glue.ViewModels;

namespace FlatRedBall.Glue.Controls
{
    public partial class CustomClassWindow : Form
    {
        CustomClassViewModel viewModel;

        public CustomClassSave CurrentCustomClassSave
        {
            get
            {
                if (TreeView.SelectedNode == null)
                {
                    return null;
                }
                else
                {
                    return TreeView.SelectedNode.Root().Tag as CustomClassSave;
                }
            }
        }

        public CustomClassWindow()
        {
            InitializeComponent();

            viewModel = new CustomClassViewModel();

            UpdateTreeView();
        }

        public void SelectFile(ReferencedFileSave rfs)
        {
            // see if this has a created class
            var customClass = ObjectFinder.Self.GetCustomClassFor(rfs);

            TreeNode treeNodeToSelect = null;

            if(customClass != null)
            {
                var treeNode = this.TreeView.Nodes
                    .FirstOrDefault(item => item.Tag == customClass);

                if(treeNode != null)
                {
                    var subnode = treeNode.Nodes
                        .FirstOrDefault(item => item.Text == rfs.Name);

                    treeNodeToSelect = subnode;
                }
            }
            
            TreeView.SelectedNode = treeNodeToSelect;
        }
        
        private void NewClassButton_Click(object sender, EventArgs e)
        {
            TextInputWindow tiw = new TextInputWindow();
            tiw.Message = "Enter new class name";

            DialogResult result = tiw.ShowDialog(this);

            if (result == DialogResult.OK)
            {
                string newClassName = tiw.Result;

                CustomClassSave newClass;

                var response = GlueCommands.Self.GluxCommands.AddNewCustomClass(newClassName, out newClass);

                if(response.OperationResult == Plugins.ExportedInterfaces.CommandInterfaces.OperationResult.Failure)
                {
                    MessageBox.Show(response.Message);
                }
                else
                {
                    UpdateTreeView();
                }
            }
        }

        private void UpdateTreeView()
        {
            UpdateTreeView(true);
        }

        private void UpdateTreeView(bool refreshClasses)
        {
            if (refreshClasses)
            {
                TreeView.Nodes.Clear();
            }

            for(int i = 0; i < ProjectManager.GlueProjectSave.CustomClasses.Count; i++)
            {
                CustomClassSave ccs = ProjectManager.GlueProjectSave.CustomClasses[i];

                TreeNode treeNode = null;
                if (refreshClasses)
                {
                    treeNode = new TreeNode(ccs.Name);
                    treeNode.Tag = ccs;

                    TreeView.Nodes.Add(treeNode);
                }
                else
                {
                    treeNode = TreeView.Nodes[i];
                    treeNode.Nodes.Clear();
                }

                foreach (string fileUsingCustomClass in ccs.CsvFilesUsingThis)
                {
                    treeNode.Nodes.Add(fileUsingCustomClass);

                }
            }
        }



        private void UseThisClassButton_Click(object sender, EventArgs e)
        {
            ReferencedFileSave currentReferencedFile = GlueState.Self.CurrentReferencedFileSave;

            viewModel.HandleUseThisClassClick(CurrentCustomClassSave);

            UpdateTreeView(false);

            SelectFile(currentReferencedFile);
        }

        private void DoneButton_Click(object sender, EventArgs e)
        {
            this.Close();

            GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

            GlueCommands.Self.GenerateCodeCommands.GenerateCustomClassesCode();

            GlueCommands.Self.GenerateCodeCommands.GenerateGlobalContentCode();

        }

        private void TreeView_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Delete:

                    TreeNode node = TreeView.SelectedNode;

                    bool shouldRemoveCurrent = false;

                    if (node.Parent == null)
                    {
                        // This thing is a class
                        DialogResult result = 
                            System.Windows.Forms.MessageBox.Show("Are you sure you want to remove the " + node.Text + " class?  All CSV types will return to their default types.",
                            "Remove Class?",
                            MessageBoxButtons.YesNo);

                        if (result == System.Windows.Forms.DialogResult.Yes)
                        {
                            ProjectManager.GlueProjectSave.CustomClasses.Remove(CurrentCustomClassSave);
                            GluxCommands.Self.SaveProjectAndElements();
                            shouldRemoveCurrent = true;
                        }
                    }
                    else
                    {
                        // This thing is a CSV file that is using this class

                        shouldRemoveCurrent = CustomClassController.Self.SetCsvRfsToUseCustomClass(
                            GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(node.Text), null, force:false);
                    }

                    if (shouldRemoveCurrent)
                    {
                        if (node.Parent != null)
                        {
                            node.Parent.Nodes.Remove(node);
                        }
                        else
                        {
                            TreeView.Nodes.Remove(node);
                        }
                    }


                    break;


            }
        }

        void UpdateToModel()
        {
            var classSave = CurrentCustomClassSave;

            if (classSave == null)
            {
                this.ClassNameTextBox.Text = "";
                this.GenerateDataClassComboBox.Enabled = false;
                this.GenerateDataClassComboBox.Checked = false;
            }
            else
            {
                this.ClassNameTextBox.Text = classSave.Name;
                this.GenerateDataClassComboBox.Enabled = true;
                this.GenerateDataClassComboBox.Checked = classSave.GenerateCode;
                this.CustomNamespaceTextbox.Text = classSave.CustomNamespace;
            }


        }

        private void GenerateDataClassComboBox_CheckedChanged(object sender, EventArgs e)
        {
            var classSave = CurrentCustomClassSave;

            if (classSave != null)
            {
                classSave.GenerateCode = GenerateDataClassComboBox.Checked;

                GlueCommands.Self.GluxCommands.SaveProjectAndElements();
            }
        }

        private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            UpdateToModel();
        }

        private void CustomNamespaceTextbox_TextChanged(object sender, EventArgs e)
        {

        }

        private void CustomNamespaceTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (CurrentCustomClassSave != null)
                {
                    CurrentCustomClassSave.CustomNamespace = CustomNamespaceTextbox.Text;

                    GlueCommands.Self.GluxCommands.SaveProjectAndElements();
                }
            }
        }

        private void CustomClassWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (CurrentCustomClassSave != null)
            {
                CurrentCustomClassSave.CustomNamespace = CustomNamespaceTextbox.Text;

                GlueCommands.Self.GluxCommands.SaveProjectAndElements();
            }
        }


    }
}
