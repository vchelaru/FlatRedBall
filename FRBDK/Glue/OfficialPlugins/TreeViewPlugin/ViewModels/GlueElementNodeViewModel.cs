using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

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



        public GlueElementNodeViewModel(NodeViewModel parent, GlueElement glueElement, bool createChildrenNodes) : base(parent)
        {
            Tag = glueElement;
            this.glueElement = glueElement;

            this.IsEditable = true;

            if (createChildrenNodes)
            {
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
            }

            if (glueElement is ScreenSave)
            {
                ImageSource = ScreenIcon;
            }
            else if (glueElement is EntitySave)
            {
                if (string.IsNullOrEmpty(glueElement.BaseElement))
                {
                    ImageSource = EntityIcon;
                }
                else
                {
                    ImageSource = EntityDerivedIcon;
                }
            }

            Text = glueElement.GetStrippedName();

            IsExpanded = false;
        }

        public override void RefreshTreeNodes(TreeNodeRefreshType treeNodeRefreshType)
        {
            base.RefreshTreeNodes(treeNodeRefreshType);

            Text = glueElement.GetStrippedName();

            if (Tag is ScreenSave asScreenSave)
            {
                var startupScreen = GlueState.Self.CurrentGlueProject.StartUpScreen;

                if (startupScreen == asScreenSave.Name)
                {
                    ImageSource = ScreenStartupIcon;
                    FontWeight = FontWeights.Bold;
                }
                else
                {
                    ImageSource = ScreenIcon;
                    FontWeight = FontWeights.Normal;
                }
            }
            else if (Tag is EntitySave asEntitySave)
            {
                if (string.IsNullOrEmpty(asEntitySave.BaseEntity))
                {
                    ImageSource = EntityIcon;
                }
                else
                {
                    ImageSource = EntityDerivedIcon;
                }
            }

            if(treeNodeRefreshType == TreeNodeRefreshType.CustomVariables)
            {
                VariablesNode.RefreshTreeNodes(treeNodeRefreshType);
            }
            else if(treeNodeRefreshType == TreeNodeRefreshType.NamedObjects)
            {
                ObjectsNode.RefreshTreeNodes(treeNodeRefreshType);
            }
            else if (treeNodeRefreshType == TreeNodeRefreshType.StateSaves)
{
    this.StatesNode.RefreshTreeNodes(treeNodeRefreshType);
}
else
{
    // could add more here for the sake of performance, but only if needed
    foreach (var node in Children)
    {
        node.RefreshTreeNodes(treeNodeRefreshType);
    }
}
        }
    }
}
