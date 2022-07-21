using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.TreeViewPlugin.ViewModels
{
    class CodeRootViewModel : NodeViewModel
    {
        private GlueElement glueElement;

        public CodeRootViewModel(NodeViewModel parent, GlueElement glueElement) : base(parent)
        {
            this.glueElement = glueElement;
        }

        public override void RefreshTreeNodes(TreeNodeRefreshType treeNodeRefreshType)
        {
            Children.Clear();

            var files = CodeWriter.GetAllCodeFilesFor(glueElement);

            foreach (var file in files)
            {
                // See if there is already a tree node for this
                NodeViewModel foundTreeNode = null;

                // Why include the folder? Why not just the name of the file? 
                //if(glueElement is ScreenSave)
                //{
                //    var path = GlueState.Self.CurrentGlueProjectDirectory + "Screens/";
                //    text = FileManager.MakeRelative(text, path);
                //}
                //else
                //{
                //    var path = GlueState.Self.CurrentGlueProjectDirectory + "Entities/";
                //    text = FileManager.MakeRelative(text, path);
                //}
                string text = file.NoPath;


                foreach (var treeNode in Children)
                {
                    if (treeNode.Text == text)
                    {
                        foundTreeNode = treeNode;
                        break;
                    }
                }

                if (foundTreeNode == null)
                {
                    var treeNode = new NodeViewModel(this);
                    treeNode.ImageSource = CodeIcon;
                    treeNode.Text = text;
                    Children.Add(treeNode);
                }
            }
        }
    }
}
