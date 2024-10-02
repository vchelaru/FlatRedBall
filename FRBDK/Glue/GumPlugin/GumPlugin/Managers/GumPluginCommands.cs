using FlatRedBall.Glue.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using FlatRedBall.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.IO;
using Gum.Managers;
using Polenter.Serialization;
using GumPlugin.ViewModels;
using GumPlugin.Controls;

namespace GumPlugin.Managers
{
    public class GumPluginCommands : Singleton<GumPluginCommands>
    {
        public GumViewModel GumViewModel { get; set; }
        public GumControl GumControl { get; set; }
        #region File Commands

        public async Task SaveGumxAsync(bool saveAllElements = false)
        {
            await TaskManager.Self.AddAsync(() =>
            {
                var gumProjectFileName = GumProjectManager.Self.GetGumProjectFileName();

                GlueCommands.Self.FileCommands.IgnoreNextChangeOnFile(gumProjectFileName.FullPath);
                GlueCommands.Self.TryMultipleTimes(() => AppState.Self.GumProjectSave.Save(gumProjectFileName.FullPath, saveAllElements));
            }, "Saving gum projects");
        }

        #endregion

        #region Component

        internal void AddComponent(FilePath filePath)
        {
            var component = FileManager.XmlDeserialize<ComponentSave>(filePath.Standardized);

            AddComponentToGumProject(component);

            component.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Component"));
        }

        internal void AddStandardElement(FilePath filePath)
        {
            var standardElement = FileManager.XmlDeserialize<StandardElementSave>(filePath.FullPath);

            AddStandardElementToGumProject(standardElement);

            standardElement.Initialize(StandardElementsManager.Self.GetDefaultStateFor(standardElement.Name));
        }

        internal void AddComponentToGumProject(ComponentSave gumComponent)
        {
            var gumProject = AppState.Self.GumProjectSave;
            gumProject.Components.Add(gumComponent);
            var elementReference = new ElementReference
            {
                ElementType = ElementType.Component,
                Name = gumComponent.Name
            };
            gumProject.ComponentReferences.Add(elementReference);
            gumProject.ComponentReferences.Sort((first, second) => first.Name.CompareTo(second.Name));
            FlatRedBall.Glue.Plugins.PluginManager.ReceiveOutput("Added Gum component " + gumComponent.Name);
        }

        internal void AddStandardElementToGumProject(StandardElementSave standardElement)
        {
            var gumProject = AppState.Self.GumProjectSave;
            AppState.Self.GumProjectSave.StandardElements.Add(standardElement);
            var elementReference = new ElementReference
            {
                ElementType = ElementType.Standard,
                Name = standardElement.Name
            };
            gumProject.StandardElementReferences.Add(elementReference);
            gumProject.StandardElements.Sort((first, second) => first.Name.CompareTo(second.Name));
            FlatRedBall.Glue.Plugins.PluginManager.ReceiveOutput("Added Gum standard element" + standardElement.Name);
        }

        internal void SaveComponent(ComponentSave gumComponent)
        {
            var gumProjectFileName = GumProjectManager.Self.GetGumProjectFileName();

            var directory = FileManager.GetDirectory(gumProjectFileName.FullPath) + ElementReference.ComponentSubfolder + "/";
            string componentFileName =
                directory + gumComponent.Name + "." + GumProjectSave.ComponentExtension;


            gumComponent.Save(componentFileName);

        }

        internal bool IsComponentFileReferenced(FilePath componentFilePath)
        {
            var gumDirectory = FileManager.GetDirectory(AppState.Self.GumProjectSave.FullFileName);

            var componentDirectory = new FilePath(gumDirectory + "Components/");

            var isInComponentsDirectory = componentDirectory.IsRootOf(componentFilePath);

            var found = false;
            if (isInComponentsDirectory)
            {
                var componentName = FileManager.RemoveExtension(FileManager.MakeRelative(
                    componentFilePath.Standardized, componentDirectory.Standardized)).Replace("\\", "/").ToLowerInvariant() ;

                found = AppState.Self.GumProjectSave.Components
                    .Any(item => item.Name.Replace("\\", "/").ToLowerInvariant() == componentName);
            }

            return found;
        }

        #endregion

        #region Screen

        internal void AddScreenToGumProject(ScreenSave gumScreen)
        {
            AppState.Self.GumProjectSave.Screens.Add(gumScreen);
            var elementReference = new ElementReference
            {
                ElementType = ElementType.Screen,
                Name = gumScreen.Name
            };
            AppState.Self.GumProjectSave.ScreenReferences.Add(elementReference);
            AppState.Self.GumProjectSave.ScreenReferences.Sort((first, second) => first.Name.CompareTo(second.Name));
            FlatRedBall.Glue.Plugins.PluginManager.ReceiveOutput("Added Gum screen " + gumScreen.Name);

        }

        internal bool IsScreenFileReferenced(FilePath screenFilePath)
        {
            var gumDirectory = FileManager.GetDirectory(AppState.Self.GumProjectSave.FullFileName);

            var screenDirectory = new FilePath( gumDirectory + "Screens/");

            var isInScreensDirectory = screenDirectory.IsRootOf(screenFilePath);

            var found = false;
            if(isInScreensDirectory)
            {
                var screenName = FileManager.MakeRelative(
                    screenFilePath.Standardized, screenDirectory.Standardized);

                found = AppState.Self.GumProjectSave.Screens
                    .Any(item => item.Name.ToLowerInvariant() == screenName.ToLowerInvariant());

            }

            return found;
        }

        internal void SaveScreen(ScreenSave gumScreen)
        {
            var gumProjectFileName = GumProjectManager.Self.GetGumProjectFileName();

            var directory = FileManager.GetDirectory(gumProjectFileName.FullPath) + ElementReference.ScreenSubfolder + "/";
            string screenFileName =
                directory + gumScreen.Name + "." + GumProjectSave.ScreenExtension;


            gumScreen.Save(screenFileName);

        }

        public string GetGumScreenNameFor(FlatRedBall.Glue.SaveClasses.ScreenSave glueScreen)
        {
            if(glueScreen.Name.StartsWith("Screens/") || glueScreen.Name.StartsWith("Screens\\"))
            {
                return glueScreen.Name.Substring("Screens/".Length).Replace("/", "\\");
            }
            else
            {
                return FileManager.RemovePath(glueScreen.Name) + "Gum";
            }
        }

        internal async Task AddScreenForGlueScreen(FlatRedBall.Glue.SaveClasses.ScreenSave glueScreen)
        {
            string gumScreenName = GetGumScreenNameFor(glueScreen);

            bool exists = AppState.Self.GumProjectSave.Screens.Any(item => item.Name == gumScreenName);
            if (!exists)
            {
                Gum.DataTypes.ScreenSave gumScreen = new Gum.DataTypes.ScreenSave();
                gumScreen.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Screen"));
                gumScreen.Name = gumScreenName;

                var gumProjectFileName = GumProjectManager.Self.GetGumProjectFileName();

                AddScreenToGumProject(gumScreen);

                await SaveGumxAsync(saveAllElements: false);
                SaveScreen(gumScreen);
            }

            // this is a new screen, so let's generate the associations:
            CodeGeneratorManager.Self.GenerateAndSaveRuntimeAssociations();

            // Select the screen to add the file to this
            // Update May 19 2022 - I don't think this is needed anymore
            //GlueState.Self.CurrentScreenSave = glueScreen;

            RightClickManager.Self.AddGumScreenScreenByName(gumScreenName, glueScreen);
        }

        internal async Task RemoveScreen(ScreenSave gumScreen, bool save = true)
        {
            await TaskManager.Self.AddAsync(async () =>
            {
                AppState.Self.GumProjectSave.Screens.Remove(gumScreen);
                AppState.Self.GumProjectSave.ScreenReferences.RemoveAll(item => item.Name == gumScreen.Name);

                if (save)
                {
                    await SaveGumxAsync();
                }

            }, $"Gum Plugin - Reacting to removed screen {gumScreen}");

        }

        #endregion

        #region Behavior

        internal void AddBehavior(BehaviorSave behavior)
        {
            AppState.Self.GumProjectSave.Behaviors.Add(behavior);
            var behaviorReference = new BehaviorReference
            {
                Name = behavior.Name
            };
            AppState.Self.GumProjectSave.BehaviorReferences.Add(behaviorReference);
            AppState.Self.GumProjectSave.BehaviorReferences.Sort((first, second) =>
            {
                if (first.Name == null)
                {
                    return 0;
                }
                else
                {
                    return first.Name.CompareTo(second.Name);
                }
            });
            FlatRedBall.Glue.Plugins.PluginManager.ReceiveOutput("Added Gum behavior " + behavior.Name);

        }

        internal void SaveBehavior(BehaviorSave behavior)
        {
            var gumProjectFileName = GumProjectManager.Self.GetGumProjectFileName();
            var directory = FileManager.GetDirectory(gumProjectFileName.FullPath) + BehaviorReference.Subfolder + "/";
            string behaviorFileName =
                directory + behavior.Name + "." + BehaviorReference.Extension;

            behavior.Save(behaviorFileName);

        }

        #endregion

        public void RefreshGumViewModel()
        {

            if (AppState.Self.GumProjectSave != null)
            {
                var rfs = GumProjectManager.Self.GetRfsForGumProject();
                var wasUpdating = GumViewModel.IsUpdatingFromGlueObject;
                GumViewModel.IsUpdatingFromGlueObject = true;
                GumViewModel.SetFrom(AppState.Self.GumProjectSave, rfs);
                GumViewModel.IsUpdatingFromGlueObject = wasUpdating;

                // This doesn't update. Not sure why....
                if(GumControl != null)
                {
                    GumControl.DataContext = null;
                    GumControl.DataContext = GumViewModel;
                }

            }
        }

        internal void SaveStandardElement(StandardElementSave gumStandardElement)
        {
            var gumProjectFileName = GumProjectManager.Self.GetGumProjectFileName();

            var directory = FileManager.GetDirectory(gumProjectFileName.FullPath) + ElementReference.StandardSubfolder + "/";
            string standardsFileName =
                directory + gumStandardElement.Name + "." + GumProjectSave.StandardExtension;

            gumStandardElement.Save(standardsFileName);
            //var serializer = new SharpSerializer();
            //serializer.Serialize(gumStandardElement, standardsFileName);

        }

        internal void UpdateGumToGlueResolution()
        {
            _ = TaskManager.Self.AddOrRunIfTasked(async () =>
            {
                var displaySettings = GlueState.Self.CurrentGlueProject?.DisplaySettings;

                if (displaySettings != null && AppState.Self.GumProjectSave != null)
                {
                    var gumProject = AppState.Self.GumProjectSave;

                    var gumScale = displaySettings.ScaleGum;

                    var effectiveWidth = 100 * displaySettings.ResolutionWidth / (decimal)gumScale;
                    var effectiveHeight = 100 * displaySettings.ResolutionHeight / (decimal)gumScale;

                    if (gumProject.DefaultCanvasWidth != effectiveWidth ||
                        gumProject.DefaultCanvasHeight != effectiveHeight)
                    {
                        gumProject.DefaultCanvasWidth = (int) effectiveWidth;
                        gumProject.DefaultCanvasHeight = (int) effectiveHeight;

                        await GumPluginCommands.Self.SaveGumxAsync();
                    }

                }
            }, "Setting Gum Resolution");


        }

    }
}
