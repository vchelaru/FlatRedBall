using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.TreeViewPlugin
{
    [Export(typeof(PluginBase))]
    public class TreeViewWinformsPlugin : EmbeddedPlugin
    {
        public override void StartUp()
        {
            AssignEvents();
        }

        private void AssignEvents()
        {
            this.RefreshTreeNodeFor = HandleRefreshTreeNode;
            this.RefreshGlobalContentTreeNode = HandleRefreshGlobalContentTreeNode;
        }

        private void HandleRefreshGlobalContentTreeNode()
        {
            ElementViewWindow.UpdateGlobalContentTreeNodes();
        }

        private void HandleRefreshTreeNode(GlueElement element)
        {
            var elementTreeNode = GlueState.Self.Find.ElementTreeNode(element);

            var project = GlueState.Self.CurrentGlueProject;

            var shouldShow = !element.IsHiddenInTreeView &&
                (
                (element is ScreenSave asScreen && project.Screens.Contains(asScreen)) ||
                (element is EntitySave asEntity && project.Entities.Contains(asEntity)));

            if (elementTreeNode == null)
            {
                if (shouldShow)
                {
                    if (element is ScreenSave screen)
                    {
                        elementTreeNode = AddScreenInternal(screen);
                    }
                    else if (element is EntitySave entitySave)
                    {
                        elementTreeNode = ElementViewWindow.AddEntity(entitySave);
                    }
                    elementTreeNode?.RefreshTreeNodes();
                }
            }
            else
            {
                if (!shouldShow)
                {
                    // remove it!
                    if (element is ScreenSave screen)
                    {
                        ElementViewWindow.RemoveScreen(screen);
                    }
                    else if (element is EntitySave entitySave)
                    {
                        ElementViewWindow.RemoveEntity(entitySave);
                    }
                }
                else
                {
                    elementTreeNode?.RefreshTreeNodes();
                }
            }
        }
        
        BaseElementTreeNode AddScreenInternal(ScreenSave screenSave)
        {
            string screenFileName = screenSave.Name + ".cs";
            string screenFileWithoutExtension = FileManager.RemoveExtension(screenFileName);

            var screenTreeNode = new ScreenTreeNode(FileManager.RemovePath(screenFileWithoutExtension));
            screenTreeNode.CodeFile = screenFileName;

            ElementViewWindow.ScreensTreeNode.Nodes.Add(screenTreeNode);
            ElementViewWindow.ScreensTreeNode.Nodes.SortByTextConsideringDirectories();

            string generatedFile = screenFileWithoutExtension + ".Generated.cs";
            screenTreeNode.GeneratedCodeFile = generatedFile;

            screenTreeNode.SaveObject = screenSave;

            return screenTreeNode;
        }
    }
}
