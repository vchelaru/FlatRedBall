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
        AddFrbSourceViewModel ViewModel => DataContext as AddFrbSourceViewModel;

        public Action LinkToSourceClicked;

        public AddFrbSourceView()
        {
            InitializeComponent();

            this.DataContextChanged += HandleDataContextChanged;
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.DataUiGrid.Instance = ViewModel;

            var category = this.DataUiGrid.Categories[0];
            category.Name = "";

            RemoveFromGrid(nameof(ViewModel.AlreadyLinkedMessageVisibility));

            MakeMemberFileSelectionDisplay(category.Members.First(item => item.Name == nameof(ViewModel.FrbRootFolder)));
            MakeMemberFileSelectionDisplay(category.Members.First(item => item.Name == nameof(ViewModel.GumRootFolder)));

            this.DataUiGrid.InsertSpacesInCamelCaseMemberNames();

            return;

            void RemoveFromGrid(string memberName)
            {
                category.Members.RemoveAll(item => item.Name == memberName);
            }
        }


        void MakeMemberFileSelectionDisplay(InstanceMember instanceMember)
        {
            instanceMember.PreferredDisplayer = typeof(FileSelectionDisplay);
            instanceMember.PropertiesToSetOnDisplayer[nameof(FileSelectionDisplay.IsFolderDialog)] = true;
        }

        private void LinkToSourceButton_Click(object sender, RoutedEventArgs e) => LinkToSourceClicked();
    }
}
