using FlatRedBall.IO;
using OfficialPlugins.FrbSourcePlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
            var githubFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GitHub");
            var frbFilePath = System.IO.Path.Combine(githubFilePath, "FlatRedBall");
            var gumFilePath = System.IO.Path.Combine(githubFilePath, "Gum");
            if (System.IO.Directory.Exists( frbFilePath  ))
            {
                ViewModel.FrbRootFolder = frbFilePath;
            }
            if(System.IO.Directory.Exists(gumFilePath))
            {
                ViewModel.GumRootFolder = gumFilePath;
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
