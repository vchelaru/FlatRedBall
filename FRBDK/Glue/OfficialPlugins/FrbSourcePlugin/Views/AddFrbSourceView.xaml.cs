using OfficialPlugins.FrbSourcePlugin.Managers;
using OfficialPlugins.FrbSourcePlugin.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace OfficialPlugins.FrbSourcePlugin.Views
{
    /// <summary>
    /// Interaction logic for AddFrbSourceView.xaml
    /// </summary>
    public partial class AddFrbSourceView : UserControl
    {
        public AddFrbSourceViewModel ViewModel { get; private set; } = new AddFrbSourceViewModel();

        public Action LinkToSourceClicked;

        public AddFrbSourceView()
        {
            InitializeComponent();

            // Github for desktop has a standard folder for source files, so let's default to that if it exists
            
            
            if (System.IO.Directory.Exists(AddSourceManager.DefaultFrbFilePath))
            {
                ViewModel.FrbRootFolder = AddSourceManager.DefaultFrbFilePath;
            }
            if(System.IO.Directory.Exists(AddSourceManager.DefaultGumFilePath))
            {
                ViewModel.GumRootFolder = AddSourceManager.DefaultGumFilePath;
            }

            this.DataUiGrid.Instance = ViewModel;

            var category = this.DataUiGrid.Categories[0];
            category.Name = "";

            MakeMemberFileSelectionDisplay(category.Members.First(item => item.Name == nameof(ViewModel.FrbRootFolder)));
            MakeMemberFileSelectionDisplay(category.Members.First(item => item.Name == nameof(ViewModel.GumRootFolder)));

            this.DataUiGrid.InsertSpacesInCamelCaseMemberNames();
            
        }

        void MakeMemberFileSelectionDisplay(InstanceMember instanceMember)
        {
            instanceMember.PreferredDisplayer = typeof(FileSelectionDisplay);
            instanceMember.PropertiesToSetOnDisplayer[nameof(FileSelectionDisplay.IsFolderDialog)] = true;
        }

        private void LinkToSourceButton_Click(object sender, RoutedEventArgs e) => LinkToSourceClicked();
    }
}
