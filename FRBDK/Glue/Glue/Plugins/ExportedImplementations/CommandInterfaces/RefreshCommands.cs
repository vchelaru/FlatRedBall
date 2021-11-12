using System;
using System.Linq;
using System.Windows.Forms;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Glue;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    public class RefreshCommands : IRefreshCommands
    {
        // The error manager is in a plugin. We could put it
        // here but it contains UI and we are trying ot move UI
        // out of Glue, not in, so instead we're going to go through
        public static Action RefreshErrorsAction { get; set; }

        public void RefreshCurrentElementTreeNode()
        {
            var element = GlueState.Self.CurrentElement;
            RefreshTreeNodeFor(element);
        }

        public void RefreshTreeNodes()
        {
            if(TaskManager.Self.IsInTask())
            {
                TaskManager.Self.OnUiThread(RefreshTreeNodes);
            }
            else
            {
                var project = GlueState.Self.CurrentGlueProject;
                var entities = project.Entities.ToArray();
                var screens = project.Screens.ToArray();

                foreach(var entity in entities)
                {
                    RefreshTreeNodeFor(entity);
                }

                foreach(var screen in screens)
                {
                    RefreshTreeNodeFor(screen);
                }
                GlueCommands.Self.DoOnUiThread(() =>

                    ElementViewWindow.ScreensTreeNode.Nodes.SortByTextConsideringDirectories()
                );

                RefreshGlobalContent();
            }
        }

        public void RefreshTreeNodeFor(GlueElement element)
        {
            if (ProjectManager.ProjectBase != null)
            {
                GlueCommands.Self.DoOnUiThread(() =>
                {
                    PluginManager.RefreshTreeNodeFor(element);
                });
            }
        }

        public void RefreshUi(StateSaveCategory category)
        {
            BaseElementTreeNode treeNode = null;
            if (ProjectManager.ProjectBase != null)
            {
                var element = Elements.ObjectFinder.Self.GetElementContaining(category);

                if(element != null)
                {
                    treeNode = GlueState.Self.Find.ElementTreeNode(element);
                }
            }

            if(treeNode != null)
            {
                MainGlueWindow.Self.BeginInvoke(new EventHandler(delegate { treeNode.RefreshStateCategoryUi(category); }));
            }
        }

        public void RefreshGlobalContent()
        {
            PluginManager.RefreshGlobalContentTreeNode();
        }

        public void RefreshPropertyGrid()
        {
            MainGlueWindow.Self.BeginInvoke(new EventHandler(delegate { MainGlueWindow.Self.PropertyGrid.Refresh(); }));
            PropertyGridHelper.UpdateDisplayedPropertyGridProperties();

        }

        public void RefreshVariables()
        {
            PluginManager.CallPluginMethod("Main Property Grid Plugin", "RefreshVariables");
        }


        public void RefreshSelection()
        {
            if (!ProjectManager.WantsToClose)
            {
                MainGlueWindow.Self.BeginInvoke(new EventHandler(RefreshSelectionInternal));
            }

        }

        private void RefreshSelectionInternal(object sender, EventArgs e)
        {
            // During a reload the CurrentElement may no longer be valid:
            var element = GlueState.Self.CurrentElement;
            if (element != null)
            {
                if (GlueState.Self.CurrentCustomVariable != null)
                {
                    GlueState.Self.CurrentCustomVariable = element.GetCustomVariable(GlueState.Self.CurrentCustomVariable.Name);
                }
                else if (GlueState.Self.CurrentReferencedFileSave != null)
                {
                    GlueState.Self.CurrentReferencedFileSave = element.GetReferencedFileSave(GlueState.Self.CurrentReferencedFileSave.Name);
                }
            }
        }

        public void RefreshErrors()
        {
            TaskManager.Self.AddOrRunIfTasked(() => RefreshErrorsAction?.Invoke(),
                "Refreshing Errors",
                TaskExecutionPreference.AddOrMoveToEnd);
        }

        public void RefreshErrorsFor(IErrorReporter errorReporter)
        {
            TaskManager.Self.AddOrRunIfTasked(() =>
            {
                var errors = errorReporter.GetAllErrors();

                if (errors != null)
                {
                    foreach (var error in errors)
                    {
                        lock (GlueState.ErrorListSyncLock)
                        {
                            var id = error.UniqueId;
                            if (GlueState.Self.ErrorList.Errors.Any(item => item.UniqueId == id) == false)
                            {
                                GlueState.Self.ErrorList.Errors.Add(error);
                            }
                        }
                    }
                }

            }, $"Refreshing errors for {errorReporter}");
        }

        public void RefreshDirectoryTreeNodes()
        {
            ElementViewWindow.AddDirectoryNodes();
        }
    }
}
