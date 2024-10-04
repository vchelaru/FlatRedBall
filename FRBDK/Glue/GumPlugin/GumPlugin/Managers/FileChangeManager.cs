using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using GumPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumPlugin.Managers;

public class FileChangeManager : Singleton<FileChangeManager>
{
    public void RegisterAdditionalContentTypes()
    {

        AvailableAssetTypes.Self.AdditionalExtensionsToTreatAsAssets.Add("behx");
    }

    public async void HandleFileChange(FilePath filePath, FileChangeType fileChangeType)
    {
        string extension = filePath.Extension;

        bool shouldHandleFileChange = false;

        if (Gum.Managers.ObjectFinder.Self.GumProjectSave != null)
        {
            var gumProjectDirectory =
                FileManager.GetDirectory(Gum.Managers.ObjectFinder.Self.GumProjectSave.FullFileName);

            shouldHandleFileChange = FileManager.IsRelativeTo(filePath.FullPath, gumProjectDirectory);
        }

        if (shouldHandleFileChange)
        {
            var isGumProjectOrElementExtension = extension == GumProjectSave.ComponentExtension ||
                extension == GumProjectSave.ScreenExtension ||
                extension == GumProjectSave.StandardExtension ||
                extension == GumProjectSave.ProjectExtension;


            if (isGumProjectOrElementExtension)
            {
                await HandleGumProjectOrElementFileChange(filePath, extension);
            }
            else if (extension == BehaviorReference.Extension)
            {
                // replace this behavior
                if(filePath.Exists())
                {
                    try
                    {
                        var newBehavior =
                            FileManager.XmlDeserialize<BehaviorSave>(filePath.FullPath);

                        // remove behavior
                        AppState.Self.GumProjectSave.Behaviors.RemoveAll(item => item.Name == newBehavior.Name);
                        AppState.Self.GumProjectSave.Behaviors.Add(newBehavior);
                    }
                    catch(Exception ex)
                    {
                        GlueCommands.Self.PrintError(ex.ToString());
                    }
                }


                // todo: make this take just 1 behavior for speed
                CodeGeneratorManager.Self.GenerateAllBehaviors();
                CodeGeneratorManager.Self.GenerateAndSaveRuntimeAssociations();
            }
            else if (extension == "ganx")
            {
                // Animations have changed, so we need to regenerate animation code.
                // For now we'll generate everything, but we may want to make this faster
                // and more precise by only generating the selected element:
                await CodeGeneratorManager.Self.GenerateDerivedGueRuntimesAsync();
            }
            else if (extension == "json")
            {
                await GumPlugin.Managers.EventExportManager.Self.HandleEventExportFileChanged(filePath.FullPath);
            }
        }
    }

    private static async Task HandleGumProjectOrElementFileChange(FilePath filePath, string extension)
    {
        // November 1, 2015
        // Why do we reload the
        // entire project and not
        // just the object that changed?
        // November 21, 2020
        // This kills performance when a lot
        // of files change, like if the user saves 
        // all in a larger Gum project
        //GumProjectManager.Self.ReloadGumProject();
        if (extension == GumProjectSave.ProjectExtension ||
            Gum.Managers.ObjectFinder.Self.GumProjectSave == null)
        {
            GumProjectManager.Self.ReloadGumProject();

            GlueCommands.Self.GenerateCodeCommands.GenerateGame1();
        }
        else
        {
            var gumProject = Gum.Managers.ObjectFinder.Self.GumProjectSave;

            if (extension == GumProjectSave.ScreenExtension)
            {
                ScreenSave screen = null;

                // It could have been deleted so check...
                if (filePath.Exists())
                {
                    GlueCommands.Self.TryMultipleTimes(() => screen = FileManager.XmlDeserialize<ScreenSave>(filePath.FullPath));
                }

                if (screen != null)
                {

                    screen.Initialize(screen.DefaultState);
                    // since the gum project didn't change, it should be here
                    var oldScreen = gumProject.Screens.FirstOrDefault(item => item.Name == screen.Name);

                    if (oldScreen != null)
                    {
                        var oldIndex = gumProject.Screens.IndexOf(oldScreen);

                        if (oldIndex != -1)
                        {
                            gumProject.Screens[oldIndex] = screen;
                        }
                    }


                }
            }
            else if (extension == GumProjectSave.ComponentExtension)
            {
                ComponentSave component = null;
                if (filePath.Exists())
                {
                    GlueCommands.Self.TryMultipleTimes(() => component = FileManager.XmlDeserialize<ComponentSave>(filePath.FullPath));
                    component.Initialize(component.DefaultState);

                    // since the gum project didn't change, it should be here
                    var oldComponent = gumProject.Components.FirstOrDefault(item => item.Name == component.Name);

                    if (oldComponent != null)
                    {
                        var oldIndex = gumProject.Components.IndexOf(oldComponent);

                        if (oldIndex != -1)
                        {
                            gumProject.Components[oldIndex] = component;
                        }
                    }
                }

            }
            else if (extension == GumProjectSave.StandardExtension)
            {
                StandardElementSave standard = null;
                GlueCommands.Self.TryMultipleTimes(() => standard = FileManager.XmlDeserialize<StandardElementSave>(filePath.FullPath));
                standard.Initialize(standard.DefaultState);

                var oldStandard = gumProject.StandardElements.FirstOrDefault(item => item.Name == standard.Name);

                if (oldStandard != null)
                {
                    var oldIndex = gumProject.StandardElements.IndexOf(oldStandard);

                    if (oldIndex != -1)
                    {
                        gumProject.StandardElements[oldIndex] = standard;
                    }
                }
            }
        }

        // refresh the cache:
        Gum.Managers.ObjectFinder.Self.DisableCache();
        Gum.Managers.ObjectFinder.Self.EnableCache();

        // Something could have changed - more components could have been added
        AssetTypeInfoManager.Self.RefreshProjectSpecificAtis();

        if (extension == GumProjectSave.ProjectExtension)
        {
            await CodeGeneratorManager.Self.GenerateDerivedGueRuntimesAsync();
        }
        else
        {
            CodeGeneratorManager.Self.GenerateDueToFileChange(filePath);
        }

        // Behaviors could have been added, so generate them
        CodeGeneratorManager.Self.GenerateAllBehaviors();

        EventsManager.Self.RefreshEvents();

        TaskManager.Self.Add(
            FileReferenceTracker.Self.RemoveUnreferencedFilesFromVsProject,
            "Removing unreferenced files for Gum project",
            TaskExecutionPreference.AddOrMoveToEnd);
    }
}
