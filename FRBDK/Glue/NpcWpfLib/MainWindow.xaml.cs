using Npc.Managers;
using Npc.ViewModels;
using Ookii.Dialogs.Wpf;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using ToolsUtilities;

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

            InitializeViewModel();

            this.DataContext = ViewModel;

            this.Loaded += HandleLoaded;
        }

        private void InitializeViewModel()
        {
            ViewModel = new NewProjectViewModel();
            ViewModel.owner = this;
            ViewModel.OpenSlnFolderAfterCreation = true;
            ViewModel.IsCreateProjectDirectoryChecked = true;

            foreach (var item in Npc.Data.EmptyTemplates.Projects)
            {
                ViewModel.AvailableProjects.Add(item);
            }
            ViewModel.SelectedProject = Npc.Data.EmptyTemplates.Projects.FirstOrDefault();



            string defaultNewProjectLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\FlatRedBallProjects\";
            ViewModel.ProjectDestinationLocation = defaultNewProjectLocation;
            ViewModel.IsCancelButtonVisible = true;

            ViewModel.PropertyChanged += HandlePropertyChanged;
            //ProcessCommandLineArguments();
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(ViewModel.SelectedProject):
                    if(ViewModel.SelectedProject is AddNewLocalProjectOption && 
                        // Vista is pretty new at this point so we should be okay...
                        VistaFolderBrowserDialog.IsVistaFolderDialogSupported)
                    {
                        var dialog = new VistaFolderBrowserDialog();
                        dialog.Description = "Select the folder that contains the template .sln file";
                        dialog.UseDescriptionForTitle = true; // This applies to the Vista style dialog only, not the old dialog.

                        if ((bool)dialog.ShowDialog(this))
                        {
                            var folder = dialog.SelectedPath;

                            var project = new PlatformProjectInfo();
                            project.FriendlyName = "Local Project";
                            project.LocalSourceFile = folder;
                            // assume true:
                            project.SupportedInGlue = true;
                            project.Namespace = GetNamespaceForProjectIn(project.LocalSourceFile);

                            ViewModel.AvailableProjects.Insert(ViewModel.AvailableProjects.Count - 1, project);
                            ViewModel.SelectedProject = project;
                        }



                    }
                    break;
            }
        }

        private string GetNamespaceForProjectIn(FilePath localSourceFile)
        {
            var csproj = FileManager.GetAllFilesInDirectory(localSourceFile.FullPath, ".csproj", int.MaxValue).FirstOrDefault();

            if(csproj != null)
            {
                var contents = FileManager.FromFileText(csproj);

                if(contents.Contains("<RootNamespace>"))
                {
                    var namespaceStart = contents.IndexOf("<RootNamespace>") + "<RootNamespace>".Length;
                    var namespaceEnd = contents.IndexOf("</RootNamespace>");

                    var namespaceString = contents.Substring(namespaceStart, namespaceEnd - namespaceStart);

                    return namespaceString;
                }
                else
                {
                    var csprojFilePath = new FilePath(csproj);
                    return csprojFilePath.CaseSensitiveNoPathNoExtension;
                }
            }
            else
            {
                return null;
            }
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            ProjectNameTextBox.Focus();
            ProjectNameTextBox.SelectAll();
        }

        public void ProcessCommandLineArguments(string arguments)
        {
            string[] args = null;
            // todo- this needs to handle spaces in file paths...
            if(!string.IsNullOrEmpty(arguments))
            {
                args = arguments.Split(' ');
            }
            CommandLineManager.Self.ProcessCommandLineArguments(args);

            if (!string.IsNullOrEmpty(CommandLineManager.Self.ProjectLocation))
            {
                ViewModel.ProjectDestinationLocation = CommandLineManager.Self.ProjectLocation;
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
            ViewModel.SourceCheckboxVisibility = 
                CommandLineManager.Self.ShowSourceCheckbox ? Visibility.Visible : Visibility.Collapsed;

            if (!string.IsNullOrEmpty(CommandLineManager.Self.DefaultDestinationDirectory) && 
                System.IO.Directory.Exists(CommandLineManager.Self.DefaultDestinationDirectory))
            {
                ViewModel.ProjectDestinationLocation = CommandLineManager.Self.DefaultDestinationDirectory;
            }
        }

        void SelectLocationClicked(object sender, RoutedEventArgs args)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = fbd.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                ViewModel.ProjectDestinationLocation = fbd.SelectedPath;
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
