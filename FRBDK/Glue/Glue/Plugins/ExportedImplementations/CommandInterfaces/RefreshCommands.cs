using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using EditorObjects.IoC;
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
            if(element == null)
            {
                throw new NullReferenceException("Attempting to refresh current element tree node but no element is selected");
            }
            RefreshTreeNodeFor(element);
        }

        public void RefreshTreeNodes()
        {
            GlueCommands.Self.DoOnUiThread(() =>
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

                RefreshGlobalContent();
            });
        }

        public void RefreshTreeNodeFor(GlueElement element, TreeNodeRefreshType treeNodeRefreshType = TreeNodeRefreshType.All)
        {
            if(element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            if (ProjectManager.ProjectBase != null)
            {
                GlueCommands.Self.DoOnUiThread(() =>
                {
                    PluginManager.RefreshTreeNodeFor(element, treeNodeRefreshType);
                });
            }
        }

        public void RefreshUi(StateSaveCategory category)
        {
            if (ProjectManager.ProjectBase != null)
            {
                var element = Elements.ObjectFinder.Self.GetElementContaining(category);
                if(element != null)
                {
                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element);
                }
            }
        }

        public void RefreshGlobalContent()
        {
            PluginManager.RefreshGlobalContentTreeNode();
        }

        public void RefreshPropertyGrid()
        {
            GlueCommands.Self.DoOnUiThread(() =>
            {
                MainGlueWindow.Self.PropertyGrid.Refresh();
                //PropertyGridHelper.UpdateDisplayedPropertyGridProperties();
                UpdateDisplayedPropertyGridProperties();
            });

        }

        void UpdateDisplayedPropertyGridProperties()
        {
            var node = GlueState.Self.CurrentTreeNode;

            ///////////////Early Out/////////////////////
            if (node == null)
            {
                MainGlueWindow.Self.PropertyGrid.SelectedObject = null;

                return;
            }
            ////////////End Early Out///////////////////

            var glueState = GlueState.Self;

            if (glueState.CurrentNamedObjectSave != null)
            {
                PropertyGridHelper.UpdateNamedObjectDisplay();
            }
            else if (glueState.CurrentReferencedFileSave != null)
            {
                PropertyGridHelper.UpdateReferencedFileSaveDisplay();
            }
            else if (glueState.CurrentEventResponseSave != null)
            {
                PropertyGridHelper.UpdateEventResponseSaveDisplayer();
            }
            else if (glueState.CurrentCustomVariable != null)
            {
                PropertyGridHelper.UpdateCustomVariableDisplay();
            }
            else if (glueState.CurrentStateSave != null)
            {
                PropertyGridHelper.UpdateStateSaveDisplay();
            }
            else if (glueState.CurrentStateSaveCategory != null)
            {
                PropertyGridHelper.UpdateStateCategorySave();
            }
            else if (glueState.CurrentEntitySave != null)
            {
                PropertyGridHelper.UpdateEntitySaveDisplay();
            }
            else if (glueState.CurrentScreenSave != null)
            {
                PropertyGridHelper.UpdateScreenSaveDisplay();
            }
            else if (node.IsGlobalContentContainerNode() && ProjectManager.GlueProjectSave != null)
            {
                MainGlueWindow.Self.PropertyGrid.SelectedObject = ProjectManager.GlueProjectSave.GlobalContentSettingsSave;
            }
            else
            {
                MainGlueWindow.Self.PropertyGrid.SelectedObject = null;
            }
        }

        public void RefreshVariables()
        {
            PluginManager.CallPluginMethod("Main Property Grid Plugin", "FixNamedObjectCollisionType");
            PluginManager.CallPluginMethod("Main Property Grid Plugin", "RefreshVariables");
        }


        public void RefreshSelection()
        {
            if (!ProjectManager.WantsToCloseProject)
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

        public async Task ClearFixedErrors()
        {
            var errorManager = Container.Get<GlueErrorManager>();
            await TaskManager.Self.AddAsync(() =>
            {
                errorManager.ClearFixedErrors();
            }, "Clearing Fixed Errors", TaskExecutionPreference.AddOrMoveToEnd);
        }

        public void RefreshErrorsFor(ErrorReporterBase errorReporter)
        {
            TaskManager.Self.AddOrRunIfTasked(() =>
            {
                // Old errors here may not be cleared. We need to check if those are fixed:
                var errorManager = Container.Get<GlueErrorManager>();
                errorManager.ClearFixedErrors(errorReporter.ErrorsBelongingToThisReporter);

                var errors = errorReporter.GetAllErrors();
                errorReporter.ErrorsBelongingToThisReporter.Clear();
                if (errors != null)
                {
                    errorReporter.ErrorsBelongingToThisReporter.AddRange(errors);
                }

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
            PluginManager.RefreshDirectoryTreeNodes();
        }
    }
}
