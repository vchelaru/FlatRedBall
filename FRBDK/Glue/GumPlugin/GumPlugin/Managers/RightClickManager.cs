using FlatRedBall.Glue.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Gum.DataTypes;
using System.Windows.Forms;
using FlatRedBall.IO;
using GumPlugin.CodeGeneration;
using Gum.Managers;
using FlatRedBall.Glue.ViewModels;
using GlueFormsCore.FormHelpers;

namespace GumPlugin.Managers
{
    public class RightClickManager : Singleton<RightClickManager>
    {
        
        public void HandleTreeViewRightClick(ITreeNode rightClickedTreeNode, List<GeneralToolStripMenuItem> menuToModify)
        {
            TryAddAddGumScreenItem(rightClickedTreeNode, menuToModify);

            TryAddAddComponentForCurrentEntity(rightClickedTreeNode, menuToModify);

            TryAddAddNewScreenForCurrentScreen(rightClickedTreeNode, menuToModify);

            TryAddRegenerateGumElement(rightClickedTreeNode, menuToModify);
        }

        private void TryAddRegenerateGumElement(ITreeNode rightClickedTreeNode, List<GeneralToolStripMenuItem> menuToModify)
        {
            var file = rightClickedTreeNode.Tag as ReferencedFileSave;

            bool shouldShowRegenerateCodeMenu =
                file != null;

            if(shouldShowRegenerateCodeMenu)
            {
                var extension = FileManager.GetExtension(file.Name);

                shouldShowRegenerateCodeMenu =
                    extension == GumProjectSave.ComponentExtension ||
                    extension == GumProjectSave.ProjectExtension ||
                    extension == GumProjectSave.ScreenExtension ||
                    extension == GumProjectSave.StandardExtension;

            }

            if(file != null && shouldShowRegenerateCodeMenu)
            {
                menuToModify.Add("Regenerate Gum Code", (not, used) =>
                {
                    var fileName = GlueCommands.Self.GetAbsoluteFileName(file);
                    CodeGeneratorManager.Self.GenerateDueToFileChange(fileName);
                });
            }
        }

        private void TryAddAddNewScreenForCurrentScreen(ITreeNode rightClickedTreeNode, List<GeneralToolStripMenuItem> menuToModify)
        {
            var shouldContinue = true;
            if (!rightClickedTreeNode.IsScreenNode())
            {
                shouldContinue = false;
            }

            if (shouldContinue && AppState.Self.GumProjectSave == null)
            {
                shouldContinue = false;
            }

            var frbScreen = rightClickedTreeNode.Tag as FlatRedBall.Glue.SaveClasses.ScreenSave;

            if(shouldContinue)
            {
                var alreadyHasScreen = frbScreen.ReferencedFiles.Any(item => FileManager.GetExtension(item.Name) == "gusx");

                if(alreadyHasScreen)
                {
                    shouldContinue = false;
                }
            }

            if(shouldContinue)
            {
                var newMenuItem = new GeneralToolStripMenuItem($"Create New Gum Screen for {FileManager.RemovePath(frbScreen.Name)}");
                menuToModify.Add(newMenuItem);
                newMenuItem.Click += async (not, used) => await TaskManager.Self.AddAsync(() =>  GumPluginCommands.Self.AddScreenForGlueScreen(frbScreen), $"Adding Gum screen for FRB screen {frbScreen}");
            }
        }

        private void TryAddAddComponentForCurrentEntity(ITreeNode rightClickedTreeNode, List<GeneralToolStripMenuItem> menuToModify)
        {
            var shouldContinue = true;
            if(!rightClickedTreeNode.IsEntityNode())
            {
                shouldContinue = false;
            }

            if(shouldContinue && AppState.Self.GumProjectSave == null)
            {
                shouldContinue = false;
            }

            var entity = rightClickedTreeNode.Tag as EntitySave;
            string gumComponentName = null;
            if(shouldContinue)
            {
                gumComponentName = FileManager.RemovePath(entity.Name) + "Gum";
                bool exists = AppState.Self.GumProjectSave.Components.Any(item => item.Name == gumComponentName);

                if(exists)
                {
                    shouldContinue = false;
                }
            }

            if (shouldContinue)
            {

                var newMenuitem = new GeneralToolStripMenuItem($"Add Gum Component to {FileManager.RemovePath(entity.Name)}");
                menuToModify.Add(newMenuitem);
                newMenuitem.Click += async (not, used) =>
                {
                    var gumComponent = new ComponentSave();
                    gumComponent.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Component"));
                    gumComponent.BaseType = "Container";
                    gumComponent.Name = gumComponentName;

                    string gumProjectFileName = GumProjectManager.Self.GetGumProjectFileName();

                    GumPluginCommands.Self.AddComponentToGumProject(gumComponent);

                    GumPluginCommands.Self.SaveGumxAsync(saveAllElements: false);

                    GumPluginCommands.Self.SaveComponent(gumComponent);



                    AssetTypeInfoManager.Self.RefreshProjectSpecificAtis();

                    var ati = AssetTypeInfoManager.Self.GetAtiFor(gumComponent);

                    var addObjectViewModel = new AddObjectViewModel();
                    addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                    addObjectViewModel.SourceClassType = ati.QualifiedRuntimeTypeName.QualifiedType;
                    addObjectViewModel.ObjectName = "GumObject";

                    await GlueCommands.Self.GluxCommands.AddNewNamedObjectToAsync(addObjectViewModel, entity);
                };
            }
        }

        private bool TryAddAddGumScreenItem(ITreeNode rightClickedTreeNode, List<GeneralToolStripMenuItem> menuToModify)
        {
            bool shouldContinue = true;
            if (!rightClickedTreeNode.IsFilesContainerNode() || !rightClickedTreeNode.Parent.IsScreenNode())
            {
                shouldContinue = false;
            }

            ReferencedFileSave gumxRfs = null;

            if (shouldContinue)
            {
                // Let's get all the available Screens:
                gumxRfs = GumProjectManager.Self.GetRfsForGumProject();
                shouldContinue = gumxRfs != null;

            }

            if (shouldContinue)
            {
                string fullFileName = GlueCommands.Self.GetAbsoluteFileName(gumxRfs);

                if (System.IO.File.Exists(fullFileName))
                {
                    string error;

                    // Calling Load does a deep load.  We only want references, so we're
                    // going to do a shallow load for perf reasons.
                    //GumProjectSave gps = GumProjectSave.Load(fullFileName, out error);
                    GumProjectSave gps = FileManager.XmlDeserialize<GumProjectSave>(fullFileName);

                    if (gps.ScreenReferences.Count != 0)
                    {
                        var menuToAddScreensTo = new GeneralToolStripMenuItem("Add Gum Screen");

                        menuToModify.Add(menuToAddScreensTo);

                        foreach (var gumScreen in gps.ScreenReferences)
                        {
                            var screenMenuItem = new GeneralToolStripMenuItem(gumScreen.Name);
                            screenMenuItem.Click += HandleAddGumScreenToFrbScreen;
                            menuToAddScreensTo.DropDownItems.Add(screenMenuItem);
                        }
                    }
                }
            }

            return shouldContinue;
        }

        private void HandleAddGumScreenToFrbScreen(object sender, EventArgs e)
        {
            string screenName = ((System.Windows.Controls.MenuItem)sender).Header as string;

            if(!string.IsNullOrEmpty(screenName))
            {
                AddGumScreenScreenByName(screenName, GlueState.Self.CurrentScreenSave);
            }

        }

        public void AddGumScreenScreenByName(string gumScreenName, FlatRedBall.Glue.SaveClasses.ScreenSave glueScreen)
        {
            string fullFileName = AppState.Self.GumProjectFolder + "Screens/" +
                gumScreenName + "." + GumProjectSave.ScreenExtension;

            if (System.IO.File.Exists(fullFileName))
            {
                bool cancelled = false;

                var newRfs = FlatRedBall.Glue.FormHelpers.RightClickHelper.AddSingleFile(
                    fullFileName, ref cancelled, glueScreen);

                // prior to doing any codegen, need to refresh the project specific ATIs:
                AssetTypeInfoManager.Self.RefreshProjectSpecificAtis();


                var gumElement = CodeGeneratorManager.GetElementFrom(newRfs);

                if(gumElement != null)
                {
                    newRfs.RuntimeType =
                        GueDerivingClassCodeGenerator.Self.GetQualifiedRuntimeTypeFor(gumElement);

                    GlueCommands.Self.GluxCommands.SaveGlux();
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(glueScreen);
                    CodeGeneratorManager.Self.GenerateCodeFor(gumElement);

                }
            }
            else
            {
                var message = "Could not find the file for the Gum screen " + gumScreenName + $"\nSearched in:\n{fullFileName}";

                if (AppState.Self.GumProjectSave == null)
                {
                    message += "\nThis is probably happening because the Gum project is null";
                }
                else if(string.IsNullOrWhiteSpace(AppState.Self.GumProjectFolder))
                {
                    message += "\nThe project does have a Gum project loaded, but it does not have an associated filename";
                }

                MessageBox.Show(message);
            }
        }
    }
}
