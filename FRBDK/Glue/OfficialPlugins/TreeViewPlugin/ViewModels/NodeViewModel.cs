using FlatRedBall.Glue;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.TreeViewPlugin.Logic;
using OfficialPlugins.TreeViewPlugin.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OfficialPlugins.TreeViewPlugin.ViewModels
{
    public class NodeViewModel : ViewModel
    {
        #region External DllImport
        [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string x, string y);

        #endregion

        #region Fields/Properties

        public static ImageSource CodeIcon;
        public static ImageSource CollisionsIcon;
        public static ImageSource CollisionIcon;
        public static ImageSource EntityIcon;
        public static ImageSource EntityInstanceIcon;
        public static ImageSource EventIcon;
        public static ImageSource FileIcon;
        public static ImageSource FolderClosedIcon;
        public static ImageSource FolderOpenIcon;
        public static ImageSource LayersIcon;
        public static ImageSource LayerIcon;
        public static ImageSource ScreenIcon;
        public static ImageSource StateIcon;
        public static ImageSource VariableIcon;

        public object Tag { get; set; }
        
        public NodeViewModel Parent { get; private set; }

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


        public ImageSource ImageSource
        {
            get => Get<ImageSource>();
            set => Set(value);
        }


        private ObservableCollection<NodeViewModel> children = new ObservableCollection<NodeViewModel>();

        public ObservableCollection<NodeViewModel> Children
        {
            get
            {
                //this.LoadChildren();
                return children;
            }
        }

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
            EventIcon = LoadIcon("icon_event");
            FileIcon = LoadIcon("icon_file_standard");
            FolderClosedIcon = LoadIcon("icon_folder");
            FolderOpenIcon = LoadIcon("icon_folder_open");
            LayersIcon = LoadIcon("icon_layers");
            LayerIcon = LoadIcon("icon_layer");
            ScreenIcon = LoadIcon("icon_screen");
            StateIcon = LoadIcon("icon_state");
            VariableIcon = LoadIcon("icon_variable");

            ImageSource LoadIcon(string iconName)
            {
                var location = $"/OfficialPluginsCore;component/TreeViewPlugin/Content/{iconName}.png";
                var bitmapImage = new BitmapImage(new Uri(location, UriKind.Relative));
                return bitmapImage;
            }

        }

        internal void ExpandParentsRecursively()
        {
            if(Parent != null)
            {
                Parent.IsExpanded = true;
                Parent.ExpandParentsRecursively();
            }
        }

        public NodeViewModel(NodeViewModel parent)
        {
            //this.Node = Node;
            this.Parent = parent;
            this.IsExpanded = false;

            ImageSource = FolderClosedIcon;
        }

        #endregion

        public virtual void RefreshTreeNodes()
        {

        }

        //private void LoadChildren()
        //{
        //    if (children == null)
        //    {
        //        children = new ObservableCollection<NodeViewModel>();
        //        var cc = this.Node as CompositeNode;
        //        if (cc != null)
        //        {
        //            foreach (var child in cc.Children)
        //            {
        //                // Debug.WriteLine("Creating VM for " + child.Name);
        //                children.Add(new NodeViewModel(child, this));
        //                // Thread.Sleep(1);
        //            }
        //        }
        //    }
        //}

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

        public string GetRelativePath()
        {

            #region Directory tree node
            if (IsDirectoryNode())
            {
                if (Parent.IsRootEntityNode())
                {
                    return "Entities/" + Text + "/";

                }
                if (Parent.IsRootScreenNode())
                {
                    return "Screens/" + Text + "/";

                }
                else if (Parent.IsGlobalContentContainerNode())
                {

                    string contentDirectory = ProjectManager.MakeAbsolute("GlobalContent", true);

                    string returnValue = contentDirectory + Text;
                    if (IsDirectoryNode())
                    {
                        returnValue += "/";
                    }
                    // But we want to make this relative to the project, so let's do that
                    returnValue = ProjectManager.MakeRelativeContent(returnValue);

                    return returnValue;
                }
                else
                {
                    // It's a tree node, so make it have a "/" at the end
                    return Parent.GetRelativePath() + Text + "/";
                }
            }
            #endregion

            #region Global content container

            else if (IsGlobalContentContainerNode())
            {
                var returnValue = GlueState.Self.Find.GlobalContentFilesPath;


                // But we want to make this relative to the project, so let's do that
                returnValue = ProjectManager.MakeRelativeContent(returnValue);



                return returnValue;
            }
            #endregion

            else if (IsFilesContainerNode())
            {
                string valueToReturn = Parent.GetRelativePath();


                return valueToReturn;
            }
            else if (IsFolderInFilesContainerNode())
            {
                return Parent.GetRelativePath() + Text + "/";
            }
            else if (IsElementNode())
            {
                return ((IElement)Tag).Name + "/";
            }
            else if (IsReferencedFile())
            {
                string toReturn = Parent.GetRelativePath() + Text;
                toReturn = toReturn.Replace("/", "\\");
                return toReturn;
            }
            else
            {
                // Improve this to handle embeded stuff
                string textToReturn = Text;

                if (string.IsNullOrEmpty(FlatRedBall.IO.FileManager.GetExtension(textToReturn)))
                {
                    textToReturn += "/";
                }

                return textToReturn;
            }
        }

        public NodeViewModel Root() => Parent == null ? this : Parent.Root();

        #region "Is" methods

        // The "Is" methods are added to make refactoring easier. Not sure if we eventually want to get rid of them:
        public bool IsDirectoryNode()
        {
            if (Parent == null)
            {
                return false;
            }

            if (this is GlueElementNodeViewModel)
                return false;

            if (Tag != null)
            {
                return false;
            }

            if (Parent.IsRootEntityNode() || Parent.IsGlobalContentContainerNode())
                return true;


            if (Parent.IsFilesContainerNode() || Parent.IsDirectoryNode())
            {
                return true;
            }

            else
                return false;
        }

        public bool IsRootEntityNode() => Text == "Entities" && Parent == null;
        public bool IsRootScreenNode() => Text == "Screens" && Parent == null;
        

        public bool IsEntityNode()
        {
            return Tag is EntitySave;
        }

        public bool IsScreenNode() => Tag is ScreenSave;

        public bool IsGlobalContentContainerNode()
        {
            return Text == "Global Content Files" && Parent == null;
        }

        public bool IsFilesContainerNode()
        {
            var parentTreeNode = Parent;
            return Text == "Files" && parentTreeNode != null &&
                (parentTreeNode.IsEntityNode() || parentTreeNode.IsScreenNode());
        }

        public bool IsFolderInFilesContainerNode()
        {
            var parentTreeNode = Parent;

            return Tag == null && parentTreeNode != null &&
                (parentTreeNode.IsFilesContainerNode() || parentTreeNode.IsFolderInFilesContainerNode());

        }

        public bool IsElementNode() => Tag is GlueElement;
        public bool IsReferencedFile() => Tag is ReferencedFileSave;

        #endregion

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
                    if (node.IsDirectoryNode())
                    {
                        SortByTextConsideringDirectories(node.Children, recursive);
                    }
                }
            }

        }

        private static int TreeNodeComparer(NodeViewModel first, NodeViewModel second)
        {
            bool isFirstDirectory = first.IsDirectoryNode();
            bool isSecondDirectory = second.IsDirectoryNode();

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

        public override string ToString()
        {
            return Text;
        }
    }
}
