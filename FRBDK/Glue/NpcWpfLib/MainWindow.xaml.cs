using Npc.Managers;
using Npc.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Npc
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public NewProjectViewModel ViewModel { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new NewProjectViewModel();
            ViewModel.owner = this;
            ViewModel.OpenSlnFolderAfterCreation = true;
            ViewModel.IsCreateProjectDirectoryChecked = true;

            foreach (var item in Npc.Data.EmptyTemplates.Projects)
            {
                ViewModel.AvailableProjects.Add(item);
            }
            ViewModel.SelectedProject = Npc.Data.EmptyTemplates.Projects.FirstOrDefault();

            string folderName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\FlatRedBallProjects\";
            ViewModel.ProjectLocation = folderName;
            ViewModel.IsCancelButtonVisible = true;
            //ProcessCommandLineArguments();

            this.DataContext = ViewModel;

            this.Loaded += HandleLoaded;
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            ProjectNameTextBox.Focus();
            ProjectNameTextBox.SelectAll();
        }

        public void ProcessCommandLineArguments(string arguments)
        {
            CommandLineManager.Self.ProcessCommandLineArguments();

            if (!string.IsNullOrEmpty(CommandLineManager.Self.ProjectLocation))
            {
                ViewModel.ProjectLocation = CommandLineManager.Self.ProjectLocation;
            }

            if (!string.IsNullOrEmpty(CommandLineManager.Self.DifferentNamespace))
            {
                ViewModel.IsDifferentNamespaceChecked = true;
                ViewModel.DifferentNamespace = CommandLineManager.Self.DifferentNamespace;
            }

            if (!string.IsNullOrEmpty(CommandLineManager.Self.OpenedBy))
            {
                // If this was opened by a different app, don't show the .sln
                ViewModel.OpenSlnFolderAfterCreation = false;
            }
        }

        void SelectLocationClicked(object sender, RoutedEventArgs args)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = fbd.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                ViewModel.ProjectLocation = fbd.SelectedPath;
            }
        }

        async void HandleMakeMyProjectClicked(object sender, RoutedEventArgs args)
        {
            await BeginMakingProject();
        }

        private async Task BeginMakingProject()
        {
            var validationResponse = ViewModel.ValidationResponse;

            if (validationResponse.Succeeded == false)
            {
                System.Windows.MessageBox.Show(validationResponse.Message);
            }
            else
            {
                bool succeeded = false;
                try
                {
                    succeeded = await ProjectCreationHelper.MakeNewProject(ViewModel);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.ToString());
                }

                if (succeeded)
                {
                    this.DialogResult = true;
                    this.Close();
                }
            }
        }

        private async void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                await BeginMakingProject();
            }
            else if(e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
        }

    }
}
