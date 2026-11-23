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

namespace GumPlugin.Managers;

public class GumxPropertiesManager
{

    public bool IsReactingToProperyChanges { get; internal set; } = true;

    public bool GetShouldAutoCreateGumScreens()
    {
        var gumRfs = GumProjectManager.Self.GetRfsForGumProject();
        if (gumRfs != null)
        {
            return gumRfs.Properties.GetValue<bool>(nameof(GumViewModel.AutoCreateGumScreens));
        }
        else
        {
            return false;
        }
    }

    public async void HandlePropertyChanged(string propertyChanged)
    {
        if(IsReactingToProperyChanges)
        {
            if (propertyChanged == nameof(GumViewModel.UseAtlases))
            {
                UpdateUseAtlases();
            }
            else if(propertyChanged == nameof(GumViewModel.AutoCreateGumScreens))
            {
                // Do we need to do anything?
            }
            else if(propertyChanged == nameof(GumViewModel.ShowDottedOutlines))
            {
                UpdateShowDottedOutlines();
            }
            else if(propertyChanged == nameof(GumViewModel.EmbedCodeFiles) ||
                propertyChanged == nameof(GumViewModel.IncludeNoFiles)
                )
            {
                UpdateCodeOrDllAdd();
            }
            else if (propertyChanged == nameof(GumViewModel.IncludeFormsInComponents))
            {
                await CodeGeneratorManager.Self.GenerateDerivedGueRuntimesAsync();

            }
            else if(propertyChanged == nameof(GumViewModel.IncludeComponentToFormsAssociation))
            {
                TaskManager.Self.Add(
                    () => { CodeGeneratorManager.Self.GenerateAndSaveRuntimeAssociations(); },
                    $"Regenerating runtime associations because of changed {propertyChanged}");
            }
            else if(propertyChanged == nameof(GumViewModel.ShowMouse))
            {
                TaskManager.Self.Add(
                    () => { GlueCommands.Self.GenerateCodeCommands.GenerateGlobalContentCode(); },
                    $"Regenerating global content because of changed property {nameof(GumViewModel.ShowMouse)}"
                    );
            }
            else if(propertyChanged == nameof(GumViewModel.MakeGumInstancesPublic))
            {
                await CodeGeneratorManager.Self.GenerateDerivedGueRuntimesAsync();
            }
            else if(propertyChanged == nameof(GumViewModel.IsMatchGameResolutionInGumChecked))
            {
                GumPluginCommands.Self.UpdateGumToGlueResolution();
            }

            GlueCommands.Self.GluxCommands.SaveProjectAndElements();
        }
    }

    private void UpdateCodeOrDllAdd()
    {
        var gumRfs = GumProjectManager.Self.GetRfsForGumProject();
        if (gumRfs != null)
        {
            var behavior = (FileAdditionBehavior)gumRfs.Properties.GetValue<FileAdditionBehavior>(nameof(FileAdditionBehavior));

            EmbeddedResourceManager.Self.UpdateCodeInProjectPresence(behavior);

            GlueCommands.Self.ProjectCommands.SaveProjects();
        }
    }

    private void UpdateShowDottedOutlines()
    {
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

    /// <summary>
    /// The UseAtlases property changed: applies it, and without atlases adds the Gum project's image files to
    /// the content project.
    /// </summary>
    public void UpdateUseAtlases()
    {
        ApplyUseAtlases();
        UpdateGumFileMembershipIfNotAtlased();
    }

    /// <summary>
    /// Tells file reference tracking whether the Gum project uses atlases, and drops the references it cached
    /// for the Gum files, which depend on it. Reads and caches only, so a project load calls this.
    /// </summary>
    public void ApplyUseAtlases()
    {
        var gumRfs = GumProjectManager.Self.GetRfsForGumProject();

        if (gumRfs != null)
        {
            FileReferenceTracker.Self.UseAtlases = GetUseAtlases(gumRfs);

            var absoluteFileName = GlueCommands.Self.GetAbsoluteFilePath(gumRfs);

            // clear the cache for all screens, components, and standards - because whether we use atlases or not has changed
            var gumFiles = GlueCommands.Self.FileCommands.GetFilePathsReferencedBy(absoluteFileName.FullPath, TopLevelOrRecursive.TopLevel);

            foreach (var file in gumFiles)
            {
                GlueCommands.Self.FileCommands.ClearFileCache(file);
            }
        }
    }

    /// <summary>
    /// Without atlases, every image the Gum project references belongs in the content project, so this adds the
    /// missing ones and saves the .csproj. A .csproj fix-up: the editor's load and regen run it through the
    /// plugin's project code generation, not the model load.
    /// </summary>
    public void UpdateGumFileMembershipIfNotAtlased()
    {
        var gumRfs = GumProjectManager.Self.GetRfsForGumProject();

        if (gumRfs != null && GetUseAtlases(gumRfs) == false)
        {
            TaskManager.Self.Add(
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

    static bool GetUseAtlases(ReferencedFileSave gumRfs) =>
        gumRfs.Properties.GetValue<bool>(nameof(GumViewModel.UseAtlases));

}
