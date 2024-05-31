using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.TreeViewPlugin.ViewModels
{
    class EventsRootViewModel : NodeViewModel
    {
        private GlueElement glueElement;

        public EventsRootViewModel(NodeViewModel parent, GlueElement glueElement) : base(FlatRedBall.Glue.FormHelpers.TreeNodeType.EventsContainerNode, parent)
        {
            this.glueElement = glueElement;

        }

        public override void RefreshTreeNodes(TreeNodeRefreshType treeNodeRefreshType)
        {
            while (this.Children.Count < glueElement.Events.Count)
            {
                int indexAddingAt = this.Children.Count;

                var node = new NodeViewModel(FlatRedBall.Glue.FormHelpers.TreeNodeType.EventNode, this);
                node.ImageSource = EventIcon;
                node.Text = glueElement.Events[indexAddingAt].EventName;
                node.Tag = glueElement.Events[indexAddingAt];
                this.Children.Add(node);
                //newNode.ImageKey = "edit_code.png";
                //newNode.SelectedImageKey = "edit_code.png";
            }

            while (this.Children.Count > glueElement.Events.Count)
            {
                this.Children.RemoveAt(this.Children.Count - 1);

            }

            for (int i = 0; i < glueElement.Events.Count; i++)
            {
                var treeNode = this.Children[i];

                var eventSave = glueElement.Events[i];

                if (treeNode.Tag != eventSave)
                {
                    treeNode.Tag = eventSave;
                }


                string textToSet = eventSave.EventName;

                if (treeNode.Text != textToSet)
                {
                    treeNode.Text = textToSet;
                }
            }
        }
    }
}
