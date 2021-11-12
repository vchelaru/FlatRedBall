using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

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

            if(glueElement is ScreenSave)
            {
                ImageSource = ScreenIcon;
            }
            else if(glueElement is EntitySave)
            {
                ImageSource = EntityIcon;
            }

            IsExpanded = false;
        }

        public override void RefreshTreeNodes()
        {
            base.RefreshTreeNodes();

            Text = glueElement.GetStrippedName();

            if(Tag is ScreenSave asScreenSave)
            {
                var startupScreen = GlueState.Self.CurrentGlueProject.StartUpScreen;

                if(startupScreen == asScreenSave.Name)
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

            foreach(var node in Children)
            {
                node.RefreshTreeNodes();
            }
        }
    }
}
