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

namespace GumPluginCore.Managers
{
    public class FileChangeManager : Singleton<FileChangeManager>
    {
        public void HandleFileChange(string fileName)
        {
            string extension = FileManager.GetExtension(fileName);

            bool shouldHandleFileChange = false;

            if (Gum.Managers.ObjectFinder.Self.GumProjectSave != null)
            {
                var gumProjectDirectory =
                    FileManager.GetDirectory(Gum.Managers.ObjectFinder.Self.GumProjectSave.FullFileName);

                shouldHandleFileChange = FileManager.IsRelativeTo(fileName, gumProjectDirectory);
            }

            if (shouldHandleFileChange)
            {
                var isGumTypeExtension = extension == GumProjectSave.ComponentExtension ||
                    extension == GumProjectSave.ScreenExtension ||
                    extension == GumProjectSave.StandardExtension ||
                    extension == GumProjectSave.ProjectExtension;


                if (isGumTypeExtension)
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
                    if(extension == GumProjectSave.ProjectExtension || 
                        Gum.Managers.ObjectFinder.Self.GumProjectSave == null)
                    {
                        GumProjectManager.Self.ReloadGumProject();
                    }
                    else
                    {
                        var gumProject = Gum.Managers.ObjectFinder.Self.GumProjectSave;

                        if (extension == GumProjectSave.ScreenExtension)
                        {
                            ScreenSave screen = null;

                            // It could have been deleted so check...
                            if(System.IO.File.Exists(fileName))
                            {
                                GlueCommands.Self.TryMultipleTimes(() => screen = FileManager.XmlDeserialize<ScreenSave>(fileName));
                            }
                            
                            if(screen != null)
                            {

                                screen.Initialize(screen.DefaultState);
                                // since the gum project didn't change, it should be here
                                var oldScreen = gumProject.Screens.FirstOrDefault(item => item.Name == screen.Name);

                                if(oldScreen != null)
                                {
                                    var oldIndex = gumProject.Screens.IndexOf(oldScreen);

                                    if(oldIndex != -1)
                                    {
                                        gumProject.Screens[oldIndex] = screen;
                                    }
                                }


                            }
                        }
                        else if(extension == GumProjectSave.ComponentExtension)
                        {
                            ComponentSave component = null;
                            if(System.IO.File.Exists(fileName))
                            {
                                GlueCommands.Self.TryMultipleTimes(() => component = FileManager.XmlDeserialize<ComponentSave>(fileName));
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
                        else if(extension == GumProjectSave.StandardExtension)
                        {
                            StandardElementSave standard = null;
                            GlueCommands.Self.TryMultipleTimes(() => standard = FileManager.XmlDeserialize<StandardElementSave>(fileName));
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
                        CodeGeneratorManager.Self.GenerateDerivedGueRuntimes();
                    }
                    else
                    {
                        CodeGeneratorManager.Self.GenerateDueToFileChange(fileName);
                    }

                    // Behaviors could have been added, so generate them
                    CodeGeneratorManager.Self.GenerateAllBehaviors();

                    EventsManager.Self.RefreshEvents();

                    TaskManager.Self.Add(
                        FileReferenceTracker.Self.RemoveUnreferencedFilesFromVsProject,
                        "Removing unreferenced files for Gum project",
                        TaskExecutionPreference.AddOrMoveToEnd);
                }
                else if (extension == BehaviorReference.Extension)
                {
                    // todo: make this take just 1 behavior for speed
                    CodeGeneratorManager.Self.GenerateAllBehaviors();
                }
                else if (extension == "ganx")
                {
                    // Animations have changed, so we need to regenerate animation code.
                    // For now we'll generate everything, but we may want to make this faster
                    // and more precise by only generating the selected element:
                    CodeGeneratorManager.Self.GenerateDerivedGueRuntimes();
                }
                else if (extension == "json")
                {
                    GumPlugin.Managers.EventExportManager.Self.HandleEventExportFile(fileName);
                }
            }
        }

    }
}
