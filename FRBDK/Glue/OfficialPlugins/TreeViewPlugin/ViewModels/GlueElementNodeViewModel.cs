using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.TreeViewPlugin.ViewModels
{
    public class GlueElementNodeViewModel : NodeViewModel
    {
        GlueElement glueElement;

        NodeViewModel FilesNode;
        NodeViewModel ObjectsNode;
        NodeViewModel VariablesNode;
        NodeViewModel StatesNode;
        NodeViewModel EventsNode;
        NodeViewModel CodeNode;



        public GlueElementNodeViewModel(NodeViewModel parent, GlueElement glueElement) : base(parent)
        {
            Tag = glueElement;
            this.glueElement = glueElement;

            FilesNode = new ReferencedFilesRootNodeViewModel(this, glueElement) { Text = "Files" };
            Children.Add(FilesNode);

            ObjectsNode = new NamedObjectsRootNodeViewModel(this, glueElement) { Text = "Objects" };
            Children.Add(ObjectsNode);

            VariablesNode = new CustomVariablesRootNodeViewModel(this, glueElement) { Text = "Variables" };
            Children.Add(VariablesNode);

            StatesNode = new StatesRootNodeViewModel(this, glueElement) { Text = "States" };
            Children.Add(StatesNode);

            EventsNode = new EventsRootViewModel(this, glueElement) { Text = "Events" };
            Children.Add(EventsNode);

            CodeNode = new CodeRootViewModel(this, glueElement) { Text = "Code" };
            Children.Add(CodeNode);

            IsExpanded = false;
        }

        public override void RefreshTreeNodes()
        {
            base.RefreshTreeNodes();

            Text = glueElement.GetStrippedName();

            foreach(var node in Children)
            {
                node.RefreshTreeNodes();
            }
        }
    }
}
