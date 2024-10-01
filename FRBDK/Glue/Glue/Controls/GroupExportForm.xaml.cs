using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.IO;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Gum.DataTypes;
using ScreenSave = FlatRedBall.Glue.SaveClasses.ScreenSave;
using Xceed.Wpf.Toolkit.Primitives;

namespace FlatRedBall.Glue.Controls;
/// <summary>
/// Interaction logic for GroupExportForm.xaml
///
/// Invoked when the user selects the ''export group'' item under ''projects'' tab
/// </summary>
public partial class GroupExportForm
{
    #region Fields

    private TreeViewItem _mScreensTreeNode;
    private TreeViewItem _mEntitiesTreeNode;
    private readonly char[] _splitChars = { '\\', '/' };

    #endregion

    /// <summary>
    /// Elements selected by the user to export (copy, not to delete or alter the original!) to a (new) file on disc.
    /// </summary>
    public IEnumerable<GlueElement> SelectedElements => 
        from TreeViewItem node 
        in ToExportTreeView.Items 
        select node.Tag as GlueElement;

    /// <summary>
    /// Returns true if any element has been selected for export.
    /// </summary>
    public bool HasSelectedElements => ToExportTreeView.Items.Count != 0;

    /// <summary>
    /// Default constructor; also fills in the existing entities and screens of the project to the left-side tree-view.
    /// </summary>
    public GroupExportForm()
    {
        InitializeComponent();

        FillAllAvailableTreeView();
    }

    private void FillAllAvailableTreeView()
    {
        var treeView = this.AllElementsTreeView;

        _mScreensTreeNode = new TreeViewItem() {
            Name = "Screens",
            Header = CreateTreeViewItemContent("Screens")
        };
        treeView.Items.Add(_mScreensTreeNode);

        foreach (ScreenSave screenSave in ObjectFinder.Self.GlueProject.Screens)
        {
            AddTreeNodeFor(screenSave);
        }

        _mEntitiesTreeNode = new TreeViewItem() {
            Name = "Entities",
            Header = CreateTreeViewItemContent("Entities"),

        };
        treeView.Items.Add(_mEntitiesTreeNode);

        foreach (var entitySave in ObjectFinder.Self.GlueProject.Entities)
        {
            AddTreeNodeFor(entitySave);
        }

        PerformDeepSort(treeView.Items);
    }

    private static void PerformDeepSort(ItemCollection treeNodeCollection)
    {
        treeNodeCollection.SortByTextConsideringDirectories();

        foreach (TreeViewItem treeNode in treeNodeCollection)
        {
            PerformDeepSort(treeNode.Items);
        }
    }

    private void AddTreeNodeFor(EntitySave entitySave)
    {
        string fullName = entitySave.Name;

        string directory = FileManager.GetDirectory(fullName, RelativeType.Relative);

        var directoryNode = GetOrCreateDirectoryNode(directory, AllElementsTreeView.Items);
        var newName = FileManager.RemovePath(entitySave.Name);
        var elementNode = new TreeViewItem
        {
            Name = newName,
            Header = CreateTreeViewItemContent(newName),
            Tag = entitySave
        };
        directoryNode.Items.Add(elementNode);
    }

    private void AddTreeNodeFor(ScreenSave screenSave)
    {
        var directory = FileManager.GetDirectory(screenSave.Name, RelativeType.Relative);
        var directoryNode = GetOrCreateDirectoryNode(directory, AllElementsTreeView.Items);
        var newName = FileManager.RemovePath(screenSave.Name);

        var elementNode = new TreeViewItem() {
            Name = newName,
            Header = CreateTreeViewItemContent(newName),
            Tag = screenSave
        };
        directoryNode.Items.Add(elementNode);
    }

    private TreeViewItem GetOrCreateDirectoryNode(string directory, ItemCollection treeNodeCollection)
    {
        var splits = directory.Split(_splitChars, StringSplitOptions.RemoveEmptyEntries);
        string currentCategory = splits[0];

        var foundNode = treeNodeCollection
            .Cast<TreeViewItem>()
            .FirstOrDefault(subNode => String.Equals(subNode.Name, currentCategory, StringComparison.OrdinalIgnoreCase));

        if (foundNode == null)
        {
            foundNode = new TreeViewItem
            {
                Name = currentCategory,
                Header = CreateTreeViewItemContent(currentCategory),
                Foreground = new SolidColorBrush(Color.FromRgb(255, 140, 0))
            };
            treeNodeCollection.Add(foundNode);
        }

        if (splits.Length == 1)
        {
            return foundNode;
        }

        int firstSlash = directory.IndexOfAny(_splitChars);
        string subString = directory.Substring(firstSlash + 1);

        return GetOrCreateDirectoryNode(subString, foundNode.Items);
    }

    private static object CreateTreeViewItemContent(string label)
    {
        return new Label() { Content = label };
    }

    private void AddAsSelectedElement(GlueElement element)
    {
        var node = new TreeViewItem
        {
            Tag = element,
            Header = CreateTreeViewItemContent(element.Name)
        };
        ToExportTreeView.Items.Add(node);
    }

    private void OkButton_Click(object sender, EventArgs e)
    {
        this.DialogResult = true;
        this.Close();
    }

    private void CancelButton_Click(object sender, EventArgs e)
    {
        this.DialogResult = false;
        this.Close();
    }

    private TreeViewItem CurrentSelectedItem { get; set; }

    private void AllElementsTreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        CurrentSelectedItem = e.NewValue is TreeViewItem { Tag: not null } selectedItem ? selectedItem : null;
    }

    private void AllElementsTreeView_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (CurrentSelectedItem != null)
        {
            AddAsSelectedElement(CurrentSelectedItem.Tag as GlueElement);
            (CurrentSelectedItem.Parent as TreeViewItem)!.Items.Remove(CurrentSelectedItem);
            CurrentSelectedItem = null;
        }
    }
}
