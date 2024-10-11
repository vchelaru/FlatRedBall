using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
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
using System.Windows.Shapes;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.Refactoring.Views
{
    /// <summary>
    /// Interaction logic for RenameModificationWindow.xaml
    /// </summary>
    public partial class RenameModificationWindow : Window
    {
        public RenameModificationWindow()
        {
            InitializeComponent();

            this.Loaded += HandleLoaded;
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            this.OkButton.Focus();
        }

        public void SetFrom(RenameModifications renameModifications)
        {
            var treeView = this.TreeViewInstance;

            treeView.Items.Clear();

            if (renameModifications.CodeFilesAffectedByRename.Count > 0)
            {
                var codeFilesNode = new TreeViewItem();
                codeFilesNode.IsExpanded = true;
                codeFilesNode.Header = "Code Files";
                treeView.Items.Add(codeFilesNode);

                foreach (var item in renameModifications.CodeFilesAffectedByRename)
                {
                    var itemNode = new TreeViewItem();
                    itemNode.Header = item;
                    codeFilesNode.Items.Add(itemNode);
                }
            }

            if (renameModifications.ElementsWithChangedBaseType.Count > 0)
            {
                var elementsNode = new TreeViewItem();
                elementsNode.IsExpanded = true;
                elementsNode.Header = "Elements with Changed Base Types";
                treeView.Items.Add(elementsNode);

                foreach (var item in renameModifications.ElementsWithChangedBaseType)
                {
                    var itemNode = new TreeViewItem();
                    itemNode.Header = item;
                    elementsNode.Items.Add(itemNode);
                }
            }

            if (renameModifications.ObjectsWithChangedBaseEntity.Count > 0)
            {
                var objectsNode = new TreeViewItem();
                objectsNode.IsExpanded = true;
                objectsNode.Header = "Objects with Changed Base Entity";
                treeView.Items.Add(objectsNode);

                foreach (var item in renameModifications.ObjectsWithChangedBaseEntity)
                {
                    var itemNode = new TreeViewItem();
                    itemNode.Header = item;
                    objectsNode.Items.Add(itemNode);
                }
            }

            if (renameModifications.ObjectsWithChangedGenericBaseEntity.Count > 0)
            {
                var objectsNode = new TreeViewItem();
                objectsNode.IsExpanded = true;
                objectsNode.Header = "Objects with Changed Generic Base Entity";
                treeView.Items.Add(objectsNode);

                foreach (var item in renameModifications.ObjectsWithChangedGenericBaseEntity)
                {
                    var itemNode = new TreeViewItem();
                    itemNode.Header = item;
                    objectsNode.Items.Add(itemNode);
                }
            }

            if (renameModifications.ChangedCollisionRelationships.Count > 0)
            {
                var relationshipsNode = new TreeViewItem();
                relationshipsNode.IsExpanded = true;
                relationshipsNode.Header = "Changed Collision Relationships";
                treeView.Items.Add(relationshipsNode);

                foreach (var item in renameModifications.ChangedCollisionRelationships)
                {
                    var itemNode = new TreeViewItem();
                    itemNode.Header = item;
                    relationshipsNode.Items.Add(itemNode);
                }
            }

            if (renameModifications.ChangedNamedObjectVariables.Count > 0)
            {
                var variablesNode = new TreeViewItem();
                variablesNode.IsExpanded = true;
                variablesNode.Header = "Changed Named Object Variables";
                treeView.Items.Add(variablesNode);

                foreach (var item in renameModifications.ChangedNamedObjectVariables)
                {
                    var itemNode = new TreeViewItem();
                    itemNode.Header = item;
                    variablesNode.Items.Add(itemNode);
                }
            }

            if (renameModifications.ChangedCustomVariables.Count > 0)
            {
                var variablesNode = new TreeViewItem();
                variablesNode.IsExpanded = true;
                variablesNode.Header = "Changed Custom Variables";
                treeView.Items.Add(variablesNode);

                foreach (var item in renameModifications.ChangedCustomVariables)
                {
                    var itemNode = new TreeViewItem();
                    itemNode.Header = item;
                    variablesNode.Items.Add(itemNode);
                }
            }

            if (!string.IsNullOrEmpty(renameModifications.StartupScreenChange))
            {
                var startupScreenNode = new TreeViewItem();
                startupScreenNode.IsExpanded = true;
                startupScreenNode.Header = "Startup Screen Change";
                treeView.Items.Add(startupScreenNode);

                var itemNode = new TreeViewItem();
                itemNode.Header = renameModifications.StartupScreenChange;
                startupScreenNode.Items.Add(itemNode);
            }


        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

    }
}
