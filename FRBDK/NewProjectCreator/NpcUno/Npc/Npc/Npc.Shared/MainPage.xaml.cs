using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Npc.ViewModels;
using Npc.Managers;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Npc
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public NewProjectViewModel ViewModel { get; set; }


        public MainPage()
        {
            this.InitializeComponent();

            ViewModel = new NewProjectViewModel();
            ViewModel.OpenSlnFolderAfterCreation = true;

            string folderName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\FlatRedBallProjects\";
            ViewModel.ProjectLocation = folderName;

            ProcessCommandLineArguments();

            this.DataContext = ViewModel;
        }

        private void ProcessCommandLineArguments()
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

        }
    }
}
