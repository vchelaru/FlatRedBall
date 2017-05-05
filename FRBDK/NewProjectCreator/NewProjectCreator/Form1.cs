using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Ionic.Zip;
using System.Diagnostics;
using FlatRedBall.IO;
using System.IO;
using FlatRedBall.Utilities;
using NewProjectCreator.Remote;
using NewProjectCreator.Managers;
using NewProjectCreator.ViewModels;
using System.Reflection;

namespace NewProjectCreator
{
    public partial class Form1 : Form
    {
        NewProjectViewModel viewModel;

        #region Properties

        public string InfoBarLabelText
        {
            get
            {
                return InfoBarLabel.Text;
            }
            set
            {
                InfoBarLabel.Text = value;
            }
        }


        #endregion



        public Form1()
        {
            InitializeComponent();

            DataLoader.LoadAvailableProjectsFromCsv();

            viewModel = new NewProjectViewModel();
            viewModel.OpenSlnFolderAfterCreation = true;

            SetInitialProjectLocation();
            //RemoteFileManager.Initialize();

            ProcessCommandLineArguments();
            

            UseDifferentNamespaceCheckBoxChanged(null, null);

        }

        private void ProcessCommandLineArguments()
        {
            CommandLineManager.Self.ProcessCommandLineArguments();

            if (!string.IsNullOrEmpty(CommandLineManager.Self.ProjectLocation))
            {
                ProjectLocationTextBox.Text = CommandLineManager.Self.ProjectLocation;
                CreateProjectDirectoryCheckBoxChanged(null, null);
            }

            if (!string.IsNullOrEmpty(CommandLineManager.Self.DifferentNamespace))
            {

                DifferentNamespaceCheckbox.Checked = true;

                DifferentNamespaceTextbox.Text = CommandLineManager.Self.DifferentNamespace;
            }

            viewModel.EmptyProjectsOnly = CommandLineManager.Self.EmptyProjectsOnly;

            if(!string.IsNullOrEmpty(CommandLineManager.Self.OpenedBy))
            {
                // If this was opened by a different app, don't show the .sln
                viewModel.OpenSlnFolderAfterCreation = false;
            }
        }

        private void SetInitialProjectLocation()
        {
            string folderName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\FlatRedBallProjects\";
            ProjectLocationTextBox.Text = folderName;
        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            FileManager.PreserveCase = true;


            ((System.Windows.Controls.Control)WpfHost.Child).DataContext = viewModel;
        }

        private void HandleSelectLocationClick(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog(this);

            if (result == DialogResult.OK)
            {
                ProjectLocationTextBox.Text = fbd.SelectedPath;

                UseDifferentNamespaceCheckBoxChanged(null, null);
            }
        }

        public void ApplyViewToViewModel()
        {

            viewModel.ProjectName = this.ProjectNameTextBox.Text;

            viewModel.UseDifferentNamespace = DifferentNamespaceCheckbox.Checked;

            viewModel.DifferentNamespace = DifferentNamespaceTextbox.Text;

            viewModel.CheckForNewVersions = CheckForNewVersionCheckBox.Checked;

            // todo - the logic for this is in Form1.cs and it should be in the VM
            viewModel.ProjectLocation = ProjectLocationTextBox.Text;


            viewModel.CreateProjectDirectory = CreateProjectDirectoryCheckBox.Checked;
            
        }

        private void MakeMyProjectClick(object sender, EventArgs e)
        {
            ApplyViewToViewModel();

            string whyIsntValid = viewModel.GetWhyIsntValid();

            if (!string.IsNullOrEmpty(whyIsntValid))
            {
                MessageBox.Show(whyIsntValid);
            }
            else
            {
                bool succeeded = ProjectCreationHelper.MakeNewProject(viewModel);

                if(succeeded)
                {
                    this.Close();
                }
            }
        }

        private void UseDifferentNamespaceCheckBoxChanged(object sender, EventArgs e)
        {
            DifferentNamespaceTextbox.Visible =
                DifferentNamespaceCheckbox.CheckState == CheckState.Checked;
        }

        

        private void ProjectNameTextBox_TextChanged(object sender, EventArgs e)
        {
            UseDifferentNamespaceCheckBoxChanged(null, null);

            if (!DifferentNamespaceTextbox.Visible)
            {
                DifferentNamespaceTextbox.Text = ProjectNameTextBox.Text;
            }

            CreateProjectDirectoryCheckBoxChanged(null, null);
        }

        private void ProjectLocationTextBox_TextChanged(object sender, EventArgs e)
        {
            UseDifferentNamespaceCheckBoxChanged(null, null);


            CreateProjectDirectoryCheckBoxChanged(null, null);
        }

        private void ProjectTypeListBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void CreateProjectDirectoryCheckBoxChanged(object sender, EventArgs e)
        {
            
            if (CreateProjectDirectoryCheckBox.CheckState == CheckState.Checked)
            {
                ApplyViewToViewModel();
                // this is heavy but whatever, we'll fix it up later
                FinalDirectoryLabel.Text = viewModel.CombinedProjectDirectory;
            }
            else
            {

                FinalDirectoryLabel.Text = ProjectLocationTextBox.Text;
            }
        }

        private void viewTemplateZipFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(FileManager.UserApplicationDataForThisApplication);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(AssemblyVersion);
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }
    }
}
