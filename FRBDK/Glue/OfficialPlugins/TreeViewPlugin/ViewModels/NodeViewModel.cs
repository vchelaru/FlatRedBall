using FlatRedBall.Glue;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using OfficialPlugins.TreeViewPlugin.Logic;
using OfficialPlugins.TreeViewPlugin.Models;
using OfficialPlugins.TreeViewPlugin.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OfficialPlugins.TreeViewPlugin.ViewModels
{
    #region Enums

    public enum SearchVisibility
    {
        MatchExplicitly,
        HaveVisibileChildren,
        MatchOrHaveVisibleChildren
    }

    #endregion

    public class NodeViewModel : ViewModel, ITreeNode
    {
        #region External DllImport
        [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string x, string y);

        #endregion

        #region Static ImageSource Members

        public static BitmapImage CodeIcon;
        public static BitmapImage CollisionsIcon;
        public static BitmapImage CollisionIcon;
        public static BitmapImage EntityIcon;
        public static BitmapImage EntityDerivedIcon;
        public static BitmapImage EntityInstanceIcon;
        public static BitmapImage EntityInstanceIsContainerIcon;
        public static BitmapImage EntityInstanceListIcon;
        public static BitmapImage EntityInstanceListDerivedIcon;
        public static BitmapImage EventIcon;
        public static BitmapImage FileIcon;
        public static BitmapImage FileIconWildcard;
        public static BitmapImage FolderClosedIcon;
        public static BitmapImage FolderOpenIcon;
        public static BitmapImage LayersIcon;
        public static BitmapImage LayerIcon;
        public static BitmapImage ScreenIcon;
        public static BitmapImage ScreenStartupIcon;
        public static BitmapImage StateIcon;
        public static BitmapImage TileShapeCollectionIcon;
        public static BitmapImage VariableIcon;
        public static BitmapImage VariableIconDerived;

        public static BitmapImage FromSource(string source)
        {
            if (source == CodeIcon.UriSource.OriginalString) return CodeIcon;
            if (source == CollisionsIcon.UriSource.OriginalString) return CollisionsIcon;
            if (source == CollisionIcon.UriSource.OriginalString) return CollisionIcon;
            if (source == EntityIcon.UriSource.OriginalString) return EntityIcon;
            if (source == EntityDerivedIcon.UriSource.OriginalString) return EntityDerivedIcon;
            if (source == EntityInstanceIcon.UriSource.OriginalString) return EntityInstanceIcon;
            if (source == EntityInstanceIsContainerIcon.UriSource.OriginalString) return EntityInstanceIsContainerIcon;
            if (source == EntityInstanceListIcon.UriSource.OriginalString) return EntityInstanceListIcon;
            if (source == EntityInstanceListDerivedIcon.UriSource.OriginalString) return EntityInstanceListDerivedIcon;
            if (source == EventIcon.UriSource.OriginalString) return EventIcon;
            if (source == FileIcon.UriSource.OriginalString) return FileIcon;
            if (source == FileIconWildcard.UriSource.OriginalString) return FileIconWildcard;
            if (source == FolderClosedIcon.UriSource.OriginalString) return FolderClosedIcon;
            if (source == FolderOpenIcon.UriSource.OriginalString) return FolderOpenIcon;
            if (source == LayersIcon.UriSource.OriginalString) return LayersIcon;
            if (source == LayerIcon.UriSource.OriginalString) return LayerIcon;
            if (source == ScreenIcon.UriSource.OriginalString) return ScreenIcon;
            if (source == ScreenStartupIcon.UriSource.OriginalString) return ScreenStartupIcon;
            if (source == StateIcon.UriSource.OriginalString) return StateIcon;
            if (source == TileShapeCollectionIcon.UriSource.OriginalString) return TileShapeCollectionIcon;
            if (source == VariableIcon.UriSource.OriginalString) return VariableIcon;
            if (source == VariableIconDerived.UriSource.OriginalString) return VariableIconDerived;

            return null;
        }

        #endregion

        #region Fields/Properties

        public object Tag { get; set; }
        
        // Not sure if we should have the setter be private or if it's okay to assign this. I think 
        // the amount that interacts with the NodeViewModel is very limited so for now we can leave it as public
        public NodeViewModel Parent { get; set; }

        ITreeNode ITreeNode.Parent => this.Parent;

        public bool HasItems
        {
            get
            {
                //this.LoadChildren();
                return this.children.Count > 0;
            }
        }

        public void Detach()
        {
            this.Parent.Children.Remove(this);
            this.Parent = null;
        }

        public FontWeight FontWeight
        {
            get => Get<FontWeight>();
            set => Set(value);
        }

        public BitmapImage ImageSource
        {
            get => Get<BitmapImage>();
            set => Set(value);
        }

        public bool IsEditable { get; set; } = false;

        string textBeforeEditing;
        public bool IsEditing
        {
            get => Get<bool>();
            set
            {
                if(value != IsEditing)
                {
                    if(value)
                    {
                        textBeforeEditing = Text;
                    }
                    Set(value);

                    if(!value)
                    {
                        HandleRenameThroughEdit();
                    }
                }
            }
        }


        public Visibility Visibility
        {
            get => Get<Visibility>();
            set => Set(value);
        }


        private ObservableCollection<NodeViewModel> children = new ObservableCollection<NodeViewModel>();

        public ObservableCollection<NodeViewModel> Children
        {
            get => children;
        }

        public ObservableCollection<NodeViewModel> VisibleChildren => Children;

        IEnumerable<ITreeNode> ITreeNode.Children => children;

        /// <summary>
        /// Used only when displaying filtered search results
        /// </summary>
        public double SearchTermMatchWeight { get; set; }

        public string Text 
        {
            get => Get<string>();
            set
            {
                //this.Node.Name = value;
                Set(value);
            }
        }

        public bool IsExpanded
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsSelected
        {
            get => Get<bool>();
            set
            {
                if (Set(value))
                {
                    if(value)
                    {
                        SelectionLogic.HandleSelected(this);
                    }
                    else
                    {
                        SelectionLogic.HandleDeselection(this);
                    }
                }
            }
        }

        public void SelectNoFocus()
        {
            Set(true, nameof(IsSelected));
        }

        public int Level
        {
            get => Get<int>();
            set => Set(value);
        }

        #endregion

        #region Constructors

        static NodeViewModel()
        {
            CodeIcon = LoadIcon("icon_code");
            CollisionIcon = LoadIcon("icon_collision");
            CollisionsIcon = LoadIcon("icon_collisions");
            EntityIcon = LoadIcon("icon_entity");
            EntityDerivedIcon = LoadIcon("icon_entity_derived");
            EntityInstanceIcon = LoadIcon("icon_entity_instance");
            EntityInstanceIsContainerIcon = LoadIcon("icon_entity_instance_iscontainer");
            EntityInstanceListIcon = LoadIcon("icon_entity_list");
            EntityInstanceListDerivedIcon = LoadIcon("icon_entity_list_derived");
            EventIcon = LoadIcon("icon_event");
            FileIcon = LoadIcon("icon_file_standard");
            FileIconWildcard = LoadIcon("icon_file_wildcard");
            FolderClosedIcon = LoadIcon("icon_folder");
            FolderOpenIcon = LoadIcon("icon_folder_open");
            LayersIcon = LoadIcon("icon_layers");
            LayerIcon = LoadIcon("icon_layer");
            ScreenIcon = LoadIcon("icon_screen");
            ScreenStartupIcon = LoadIcon("icon_screen_startup");
            StateIcon = LoadIcon("icon_state");
            TileShapeCollectionIcon = LoadIcon("icon_tile_shape_collection");
            VariableIcon = LoadIcon("icon_variable");
            VariableIconDerived = LoadIcon("icon_variable_derived");

            BitmapImage LoadIcon(string iconName)
            {
                var location = $"/OfficialPluginsCore;component/TreeViewPlugin/Content/{iconName}.png";
                var bitmapImage = new BitmapImage(new Uri(location, UriKind.Relative));
                return bitmapImage;
            }

        }


        public NodeViewModel(NodeViewModel parent = null)
        {
            Visibility = Visibility.Visible;
            //this.Node = Node;
            this.Parent = parent;
            this.IsExpanded = false;

            FontWeight = FontWeights.Normal;

            ImageSource = FolderClosedIcon;
        }

        #endregion

        public virtual void RefreshTreeNodes(TreeNodeRefreshType treeNodeRefreshType)
        {

        }


        internal void CollapseRecursively()
        {
            this.IsExpanded = false;
            foreach(var child in this.Children)
            {
                child.CollapseRecursively();
            }
        }

        internal void DeselectResursively()
        {
            this.IsSelected = false;

            foreach (var child in this.Children)
            {
                child.DeselectResursively();
            }
        }

        internal void CollapseToDefinitions()
        {
            if (this.Tag is GlueElement)
            {
                this.IsExpanded = false;
            }
            else if((this as ITreeNode).IsFolderForEntities())
            {
                this.IsExpanded = true;
            }

            foreach (var child in this.Children)
            {
                child.CollapseToDefinitions();
            }
        }

        public void Focus(MainTreeViewControl mainView)
        {
            var container = mainView.MainTreeView.ItemContainerGenerator.ContainerFromItem(this) as ListBoxItem;
            // This is needed to handle focusing because otherwise clicks on teh treeview don't focus.
            if (container != null)
            {
                try
                {
                    container.Focus();
                    System.Windows.Input.Keyboard.Focus(container);
                }
                catch (Exception ex)
                {
                    // not sure why but it can crash. Added breakpoint here to see if I can catch what's up. If it does fail for
                    // other users we prob don't want to do anything, just fail silently.
                    int m = 3;
                }
            }
        }

        #region Parent-based Methods

        internal void ExpandParentsRecursively()
        {
            if(Parent != null)
            {
                Parent.IsExpanded = true;
                Parent.ExpandParentsRecursively();
            }
        }

        public NodeViewModel Root() => Parent == null ? this : Parent.Root();

        #endregion

        #region Children-based methods

        void ITreeNode.SortByTextConsideringDirectories() => this.SortByTextConsideringDirectories();
        public void SortByTextConsideringDirectories(ObservableCollection<NodeViewModel> treeNodeCollection = null, bool recursive = false)
        {
            if(treeNodeCollection == null)
            {
                treeNodeCollection = Children;
            }

            int lastObjectExclusive = treeNodeCollection.Count;
            int whereObjectBelongs;
            for (int i = 0 + 1; i < lastObjectExclusive; i++)
            {
                var first = treeNodeCollection[i];
                var second = treeNodeCollection[i - 1];
                if (TreeNodeComparer(first, second) < 0)
                {
                    if (i == 1)
                    {
                        var treeNode = treeNodeCollection[i];
                        treeNodeCollection.RemoveAt(i);

                        treeNodeCollection.Insert(0, treeNode);
                        continue;
                    }

                    for (whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                    {
                        second = treeNodeCollection[whereObjectBelongs];
                        if (TreeNodeComparer(treeNodeCollection[i], second) >= 0)
                        {
                            var treeNode = treeNodeCollection[i];

                            treeNodeCollection.RemoveAt(i);
                            treeNodeCollection.Insert(whereObjectBelongs + 1, treeNode);
                            break;
                        }
                        else if (whereObjectBelongs == 0 && TreeNodeComparer(treeNodeCollection[i], treeNodeCollection[0]) < 0)
                        {
                            var treeNode = treeNodeCollection[i];
                            treeNodeCollection.RemoveAt(i);
                            treeNodeCollection.Insert(0, treeNode);
                            break;
                        }
                    }
                }
            }

            if (recursive)
            {
                foreach (var node in treeNodeCollection)
                {
                    if (((ITreeNode)node).IsDirectoryNode())
                    {
                        SortByTextConsideringDirectories(node.Children, recursive);
                    }
                }
            }

        }

        private static int TreeNodeComparer(NodeViewModel first, NodeViewModel second)
        {
            bool isFirstDirectory = ((ITreeNode)first).IsDirectoryNode();
            bool isSecondDirectory = ((ITreeNode)second).IsDirectoryNode();

            if (isFirstDirectory && !isSecondDirectory)
            {
                return -1;
            }
            else if (!isFirstDirectory && isSecondDirectory)
            {
                return 1;
            }
            else
            {

                //return first.Text.CompareTo(second.Text);
                // This will put Level9 before Level10
                return StrCmpLogicalW(first.Text, second.Text);
            }
        }

        internal NodeViewModel GetNodeByTag(object tag)
        {
            if(tag == this.Tag)
            {
                return this;
            }
            else
            {
                foreach(var child in Children)
                {
                    var node = child.GetNodeByTag(tag);

                    if(node != null)
                    {
                        return node;
                    }
                }
            }
            return null;
        }

        public void Remove(ITreeNode child)
        {
            var childAsViewModel = child as NodeViewModel;

            this.Children.Remove(childAsViewModel);
            childAsViewModel.Parent = null;
        }

        public void Add(ITreeNode child)
        {
            var childAsViewModel = child as NodeViewModel;
            this.Children.Add(childAsViewModel);
            childAsViewModel.Parent = this;
        }

        public ITreeNode FindByName(string name)
        {
            return this.Children.FirstOrDefault(item => item.Text == name);
        }

        public void RemoveGlobalContentTreeNodesIfDoesntExist(ITreeNode treeNode)
        {
            var vm = treeNode as NodeViewModel;
            if (((ITreeNode)treeNode).IsDirectoryNode())
            {
                string directory = treeNode.GetRelativeFilePath();

                directory = GlueCommands.Self.GetAbsoluteFileName(directory, true);


                if (!Directory.Exists(directory))
                {
                    // The directory isn't here anymore, so kill it!
                    treeNode.Parent.Remove(treeNode);

                }
                else
                {
                    // The directory is valid, but let's check subdirectories
                    for (int i = vm.Children.Count - 1; i > -1; i--)
                    {
                        RemoveGlobalContentTreeNodesIfDoesntExist(vm.Children[i]);
                    }
                }
            }
            else // assume content for now
            {

                ReferencedFileSave referencedFileSave = treeNode.Tag as ReferencedFileSave;

                if (!ProjectManager.GlueProjectSave.GlobalFiles.Contains(referencedFileSave))
                {
                    treeNode.Parent.Remove(treeNode);
                }
                else
                {
                    // The RFS may be contained, but see if the file names match
                    string rfsName = FileManager.Standardize(referencedFileSave.Name, null, false);
                    string treeNodeFile = FileManager.Standardize(treeNode.GetRelativeFilePath(), null, false);

                    // We first need to make sure that the file is part of GlobalContentFiles.
                    // If it is, then we may have tree node in the wrong folder, so let's get rid
                    // of it.  If it doesn't start with globalcontent/ then we shouldn't remove it here.
                    if (rfsName.StartsWith("globalcontent/", StringComparison.OrdinalIgnoreCase) && !String.Equals(rfsName, treeNodeFile, StringComparison.OrdinalIgnoreCase))
                    {
                        treeNode.Parent.Remove(treeNode);
                    }
                }
            }
        }

        #endregion


        private async void HandleRenameThroughEdit()
        {
            if(this.Text == textBeforeEditing)
            {
                return;
            }
            if(Tag is GlueElement element)
            {
                await GlueCommands.Self.GluxCommands.ElementCommands.RenameElement(element, Text);

                // This updates the tree node back in case RenameElement doesn't allow the rename to happen.
                GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element);
            }
            else if(Tag is ReferencedFileSave rfs)
            {
                var parentEntity = ObjectFinder.Self.GetElementContaining(rfs);

                // RFS names have the full path (relative to content root). I know, this is different than how
                // Screens and Entities work but....I dont' want to change that now.
                var noDirectory = FlatRedBall.IO.FileManager.GetDirectory(rfs.Name, RelativeType.Relative);
                var newName = noDirectory + Text;


                GlueCommands.Self.FileCommands.RenameReferencedFileSave(rfs, newName);
                if(parentEntity != null)
                {
                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(parentEntity);
                }
                else
                {
                    GlueCommands.Self.RefreshCommands.RefreshGlobalContent();
                }


            }
            else if(Tag is NamedObjectSave nos)
            {
                var nosElement = ObjectFinder.Self.GetElementContaining(nos);

                await GlueCommands.Self.GluxCommands.RenameNamedObjectSave(nos, Text);

                GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(nosElement);

            }
            else if(Tag == null)
            {
                // it's a folder:
                // This needs to have the old name before the new name is set:
                var newName = this.Text;
                this.Text = textBeforeEditing;

                await GlueCommands.Self.GluxCommands.RenameFolder(this, newName);


                GlueCommands.Self.RefreshCommands.RefreshTreeNodes(); // just do it all? 
            }
        }

        public override string ToString()
        {
            return Text;
        }

        ITreeNode ITreeNode.FindByTagRecursive(object tag) => this.GetNodeByTag(tag);

    }
}
