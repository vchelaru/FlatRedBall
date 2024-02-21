using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using FlatRedBall.IO;
using L = Localization;

namespace FlatRedBall.Glue.Controls;

/// <summary>
/// Interaction logic for FileAssociationWindow.xaml
/// </summary>
public partial class FileAssociationWindow : IPropertyGrid
{
    public FileAssociationWindow()
    {
        InitializeComponent();
        Refresh();
        this.PropertyGrid.OnSelectionChanged += OnSelectionChanged;
        this.SelectedRow = this.PropertiesValues[0];
        this.SelectedItemLabel.Content = PropertiesValues[0].Property;
    }

    private PropertyGridRow SelectedRow { get; set; }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedRow = e.AddedItems[0] as PropertyGridRow;
        this.SelectedItemLabel.Content = SelectedRow!.Property;
    }

    public ObservableCollection<PropertyGridRow> PropertiesValues { get; set; }
    private void OkButton_Click(object sender, EventArgs e)
    {
        this.Close();
    }

    private void MakeAbsoluteButton_Click(object sender, EventArgs e)
    {
        string extensionToMakeAbsolute = SelectedRow.Property;

        string oldApplicationName = EditorData.FileAssociationSettings.GetApplicationForExtension(extensionToMakeAbsolute);

        if (oldApplicationName != "<DEFAULT>" && FileManager.IsRelative(oldApplicationName))
        {
            string newApplicationName = FileManager.MakeAbsolute(oldApplicationName);

            EditorData.FileAssociationSettings.SetApplicationForExtension(extensionToMakeAbsolute, newApplicationName);
            EditorData.FileAssociationSettings.ReplaceApplicationInList(oldApplicationName, newApplicationName);
            Refresh();
        }
    }

    private void MakeRelativeToProjectButton_Click(object sender, EventArgs e)
    {
        string extensionToMakeAbsolute = SelectedRow.Property;

        string oldApplicationName = EditorData.FileAssociationSettings.GetApplicationForExtension(extensionToMakeAbsolute);

        if (oldApplicationName != "<DEFAULT>" && !FileManager.IsRelative(oldApplicationName))
        {
            string newApplicationName = FileManager.MakeRelative(oldApplicationName);
            
            EditorData.FileAssociationSettings.SetApplicationForExtension(extensionToMakeAbsolute, newApplicationName);
            EditorData.FileAssociationSettings.ReplaceApplicationInList(oldApplicationName, newApplicationName);
            Refresh();
        }
    }

    private void Refresh()
    {
        PropertiesValues = new ObservableCollection<PropertyGridRow>(
            EditorData.FileAssociationSettings.ExtensionApplicationAssociations.Select(e =>
                new PropertyGridRow(e.Key, e.Value)));
    }
}