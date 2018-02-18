using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.SaveClasses;
using EditorObjects.Parsing;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Managers;
using GumPlugin.ViewModels;

namespace GumPlugin.Managers
{
    class GumxPropertiesManager
    {
        public bool GetAutoCreateGumScreens()
        {
            var gumRfs = GumProjectManager.Self.GetRfsForGumProject();
            if (gumRfs != null)
            {
                return gumRfs.Properties.GetValue<bool>("AutoCreateGumScreens");
            }
            else
            {
                return false;
            }
        }

        public void HandlePropertyChanged(string propertyChanged)
        {
            if (propertyChanged == "UseAtlases")
            {
                UpdateUseAtlases();
            }
            else if(propertyChanged == "AutoCreateGumScreens")
            {
                // Do we need to do anything?
            }
            else if(propertyChanged == nameof(GumViewModel.ShowDottedOutlines))
            {
                UpdateShowDottedOutlines();
            }
            else if(propertyChanged == nameof(GumViewModel.AddDll) ||
                propertyChanged == nameof(GumViewModel.EmbedCodeFiles) ||
                propertyChanged == nameof(GumViewModel.IncludeNoFiles)
                )
            {
                UpdateCodeOrDllAdd();
            }
            else if (propertyChanged == nameof(GumViewModel.IncludeFormsInComponents))
            {
                TaskManager.Self.AddSync(
                    CodeGeneratorManager.Self.GenerateDerivedGueRuntimes,
                    $"Regenerating Gum derived runtimes because of changed {propertyChanged}");

            }
            else if(propertyChanged == nameof(GumViewModel.IncludeComponentToFormsAssociation))
            {
                TaskManager.Self.AddSync(
                    () => { CodeGeneratorManager.Self.GenerateAndSaveRuntimeAssociations(); },
                    $"Regenerating runtime associations because of changed {propertyChanged}");
            }
            GlueCommands.Self.GluxCommands.SaveGlux();
        }

        private void UpdateCodeOrDllAdd()
        {
            var gumRfs = GumProjectManager.Self.GetRfsForGumProject();
            if (gumRfs != null)
            {
                var behavior = (FileAdditionBehavior)gumRfs.Properties.GetValue<FileAdditionBehavior>(nameof(FileAdditionBehavior));

                EmbeddedResourceManager.Self.UpdateCodeInProjectPresence(behavior);
            }
        }

        private void UpdateShowDottedOutlines()
        {
            AssetTypeInfoManager.Self.RefreshGumxLoadCode();


            var gumRfs = GumProjectManager.Self.GetRfsForGumProject();

            if (gumRfs != null)
            {
                // At the time of this writing the
                // gumx file should always be part of
                // global content, but who knows what will
                // happen in the future so I'm going to make
                // this code work regardless of where it's added:
                var container = gumRfs.GetContainer();
                if (container == null)
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateGlobalContentCode();
                }
                else
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(container);
                }
            }
        }

        public void UpdateUseAtlases()
        {
            var gumRfs = GumProjectManager.Self.GetRfsForGumProject();

            if (gumRfs != null)
            {
                bool useAtlases = 
                    gumRfs.Properties.GetValue<bool>("UseAtlases");

                FileReferenceTracker.Self.UseAtlases = useAtlases;

                var absoluteFileName = GlueCommands.Self.GetAbsoluteFileName(gumRfs);

                // clear the cache for all screens, components, and standards - because whether we use atlases or not has changed
                var gumFiles = GlueCommands.Self.FileCommands.GetFilesReferencedBy(absoluteFileName, TopLevelOrRecursive.TopLevel);

                foreach (var file in gumFiles)
                {
                    GlueCommands.Self.FileCommands.ClearFileCache(file);
                }

                if (useAtlases == false)
                {
                    // If useAtlases is set to false, then that means that 
                    // a lot of new files need to be added to the project.
                    TaskManager.Self.AddSync(
                        () =>
                        {
                            bool wasAnythingAdded = GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(gumRfs);
                            if(wasAnythingAdded)
                            {
                                GlueCommands.Self.ProjectCommands.SaveProjects();
                            }
                        },
                        $"Refreshing files in content project for {gumRfs.Name}"); 
                }
            }
        }

    }
}
