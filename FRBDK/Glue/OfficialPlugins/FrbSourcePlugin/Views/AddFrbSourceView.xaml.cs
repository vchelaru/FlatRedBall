using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;
using OfficialPlugins.FrbSourcePlugin.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace OfficialPlugins.FrbSourcePlugin.Views;

/// <summary>
/// Interaction logic for AddFrbSourceView.xaml
/// </summary>
public partial class AddFrbSourceView : UserControl
{
    AddFrbSourceViewModel ViewModel => (AddFrbSourceViewModel)DataContext;

    public Action? LinkToSourceClicked;

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
        RemoveFromGrid(nameof(ViewModel.VisualStudioProject));
        RemoveFromGrid(nameof(ViewModel.Title));

        var shouldRemoveSkiaGum = GlueState.Self.CurrentMainProject is not MonoGameDesktopGlNetCoreProject;

        if(!shouldRemoveSkiaGum)
        {
            shouldRemoveSkiaGum = GlueState.Self.SyncedProjects.Any(item => item is not MonoGameDesktopGlNetCoreProject);
        }

        if (shouldRemoveSkiaGum)
        {
            ViewModel.IncludeGumSkia = false;
            RemoveFromGrid(nameof(ViewModel.IncludeGumSkia));
        }

        var frbRootFolderMember = category.Members.First(item => item.Name == nameof(ViewModel.FrbRootFolder));
        frbRootFolderMember.DetailText = "The root folder of the location where FlatRedBall is cloned";
        MakeMemberFileSelectionDisplay(frbRootFolderMember);

        var gumRootFolderMember = category.Members.First(item => item.Name == nameof(ViewModel.GumRootFolder));
        gumRootFolderMember.DetailText = "The root folder of the location where Gum is cloned";
        MakeMemberFileSelectionDisplay(gumRootFolderMember);

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

    private void LinkToSourceButton_Click(object sender, RoutedEventArgs e) => LinkToSourceClicked?.Invoke();
}
