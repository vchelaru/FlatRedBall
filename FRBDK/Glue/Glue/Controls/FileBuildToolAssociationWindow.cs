using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;
using System.Reflection;
using EditorObjects.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.Controls
{
    public partial class FileBuildToolAssociationWindow : Form
    {
        string mExternallyBuiltFileDirectory;

        IList<BuildToolAssociation> mBuildToolList;

        public BuildToolAssociation SelectedBuildToolAssociation
        {
            get
            {
                return listBox1.SelectedItem as BuildToolAssociation;
            }
        }

        public FileBuildToolAssociationWindow(IList<BuildToolAssociation> buildToolList)
        {
            mBuildToolList = buildToolList;

            InitializeComponent();
        }

        private void FileBuildToolAssociationWindow_Shown(object sender, EventArgs e)
        {
            foreach (BuildToolAssociation bta in mBuildToolList)
            {
                listBox1.Items.Add(bta);
            }

            if (ProjectManager.GlueProjectSave != null)
            {
                mExternallyBuiltFileDirectory = ProjectManager.GlueProjectSave.ExternallyBuiltFileDirectory;
                ExternalFileRootTextBox.Text = mExternallyBuiltFileDirectory;
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            UpdateExternalBuildDirectory();

            ProjectManager.GlueSettingsSave.Save();
            GlueCommands.Self.ProjectCommands.SaveProjects();
            BuildToolAssociationManager.Self.SaveProjectSpecificBuildTools();

            this.Close();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            propertyGrid1.SelectedObject = SelectedBuildToolAssociation;
            UpdateExampleLabel();
        }

        private void UpdateExampleLabel()
        {
            if(SelectedBuildToolAssociation == null)
            {
                ExampleLabel.Text = "Select an item to see an example command line";
            }
            else
            {
                ExampleLabel.Text = "Example:\n" + SelectedBuildToolAssociation?.ExampleCommandLine;
            }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            BuildToolAssociation newItem =
                new BuildToolAssociation();



            mBuildToolList.Add(newItem);

            listBox1.Items.Add(newItem);

            listBox1.SelectedItem = newItem;
        }
        
        private void listBox1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Delete:
                    object objectToRemove = listBox1.SelectedItem;

                    if (objectToRemove != null)
                    {
                        listBox1.Items.Remove(objectToRemove);

                        mBuildToolList.Remove(
                            (BuildToolAssociation)objectToRemove);
                    }

                    break;
            }

        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            // Instead of inheriting from ListBox let's just use reflection and do it all in-place
            string changedMember = e.ChangedItem.PropertyDescriptor.Name;
            object oldValue = e.OldValue;

            var projectDirectory = FlatRedBall.Glue.Plugins.ExportedImplementations.GlueState.Self.CurrentGlueProjectDirectory;


            // We use relative files here
            if (changedMember == nameof(SelectedBuildToolAssociation.BuildTool))
            {
                string value = (string)e.ChangedItem.Value;

                bool shouldMakeRelative = SelectedBuildToolAssociation.IsBuildToolAbsolute == false;

                if (shouldMakeRelative)
                {

                    string relativeValue = FileManager.MakeRelative(value, projectDirectory);
                    SelectedBuildToolAssociation.BuildTool = relativeValue;
                }
            }
            else if(changedMember == nameof(SelectedBuildToolAssociation.IsBuildToolAbsolute))
            {
                if(SelectedBuildToolAssociation.IsBuildToolAbsolute && FileManager.IsRelative(SelectedBuildToolAssociation.BuildToolProcessed))
                {
                    // Make the build tool absolute:
                    SelectedBuildToolAssociation.BuildTool = FileManager.RemoveDotDotSlash(projectDirectory + SelectedBuildToolAssociation.BuildToolProcessed);
                }
                else if(SelectedBuildToolAssociation.IsBuildToolAbsolute == false && !FileManager.IsRelative(SelectedBuildToolAssociation.BuildToolProcessed))
                {
                    // It's currently absolute, so convert it to relative:
                    string relativeValue = FileManager.MakeRelative(SelectedBuildToolAssociation.BuildToolProcessed, projectDirectory);
                    SelectedBuildToolAssociation.BuildTool = relativeValue;
                }
            }

            MethodInfo methodInfo = typeof(ListBox).GetMethod("RefreshItems", BindingFlags.Instance|BindingFlags.NonPublic);

            methodInfo.Invoke(listBox1, null);

            propertyGrid1.Refresh();

            UpdateExampleLabel();
        }

        private void UpdateExternalBuildDirectory()
        {
            string externallyBuildFileDirectory = ExternalFileRootTextBox.Text;
            if (mExternallyBuiltFileDirectory != externallyBuildFileDirectory)
            {
                string relativeDirectory = externallyBuildFileDirectory;
                string absoluteDirectory = externallyBuildFileDirectory;

                if (!FileManager.IsRelative(relativeDirectory))
                {
                    relativeDirectory = FileManager.MakeRelative(relativeDirectory);
                }
                if (FileManager.IsRelative(absoluteDirectory))
                {
                    absoluteDirectory = FileManager.MakeAbsolute(absoluteDirectory);
                }
                if (System.IO.Directory.Exists(absoluteDirectory))
                {
                    if (ProjectManager.GlueProjectSave != null)
                    {
                        ProjectManager.GlueProjectSave.ExternallyBuiltFileDirectory = relativeDirectory;
                    }
                    GluxCommands.Self.SaveGlux();
                }
                else
                {
                    MessageBox.Show("The directory for externally built content does not exist:\n" + absoluteDirectory);
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }




    }
}
