using FlatRedBall.Glue;
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

        public static ImageSource CodeIcon;
        public static ImageSource CollisionsIcon;
        public static ImageSource CollisionIcon;
        public static ImageSource EntityIcon;
        public static ImageSource EntityInstanceIcon;
        public static ImageSource EntityInstanceListIcon;
        public static ImageSource EventIcon;
        public static ImageSource FileIcon;
        public static ImageSource FileIconWildcard;
        public static ImageSource FolderClosedIcon;
        public static ImageSource FolderOpenIcon;
        public static ImageSource LayersIcon;
        public static ImageSource LayerIcon;
        public static ImageSource ScreenIcon;
        public static ImageSource ScreenStartupIcon;
        public static ImageSource StateIcon;
        public static ImageSource VariableIcon;

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

        public ImageSource ImageSource
        {
            get => Get<ImageSource>();
            set => Set(value);
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
                if (Set(value) && value)
                {
                    SelectionLogic.HandleSelected(this);
                }
            }
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
            EntityInstanceIcon = LoadIcon("icon_entity_instance");
            EntityInstanceListIcon = LoadIcon("icon_entity_list");
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
            VariableIcon = LoadIcon("icon_variable");

            ImageSource LoadIcon(string iconName)
            {
                var location = $"/OfficialPluginsCore;component/TreeViewPlugin/Content/{iconName}.png";
                var bitmapImage = new BitmapImage(new Uri(location, UriKind.Relative));
                return bitmapImage;
            }

        }


        public NodeViewModel(NodeViewModel parent)
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

        internal void CollapseToDefinitions()
        {
            if (this.Tag is GlueElement)
            {
                this.IsExpanded = false;
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

        public NodeViewModel AddChild()
        {
            //var cn = this.Node as CompositeNode;
            //if (cn == null)
            //{
            //    return null;
            //}

            //var newChild = new CompositeNode() { Name = "New node" };
            //cn.Children.Add(newChild);
            var vm = new NodeViewModel(this);
            this.Children.Add(vm);
            return vm;
        }


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

                directory = ProjectManager.MakeAbsolute(directory, true);


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
                    string rfsName = FileManager.Standardize(referencedFileSave.Name, null, false).ToLower();
                    string treeNodeFile = FileManager.Standardize(treeNode.GetRelativeFilePath(), null, false).ToLower();

                    // We first need to make sure that the file is part of GlobalContentFiles.
                    // If it is, then we may have tree node in the wrong folder, so let's get rid
                    // of it.  If it doesn't start with globalcontent/ then we shouldn't remove it here.
                    if (rfsName.StartsWith("globalcontent/") && rfsName != treeNodeFile)
                    {
                        treeNode.Parent.Remove(treeNode);
                    }
                }
            }
        }

        #endregion


        public override string ToString()
        {
            return Text;
        }

        ITreeNode ITreeNode.FindByTagRecursive(object tag) => this.GetNodeByTag(tag);

    }
}
