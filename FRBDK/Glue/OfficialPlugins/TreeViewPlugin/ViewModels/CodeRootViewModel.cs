using FlatRedBall.Glue.Parsing;
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

        public override void RefreshTreeNodes()
        {
            Children.Clear();

            var files = CodeWriter.GetAllCodeFilesFor(glueElement);

            foreach (var file in files)
            {
                // See if there is already a tree node for this
                NodeViewModel foundTreeNode = null;
                string text = FileManager.MakeRelative(file.FullPath);
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
