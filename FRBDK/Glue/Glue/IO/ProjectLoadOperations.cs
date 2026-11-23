using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.FactoryPlugin;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.IO
{
    /// <summary>
    /// The work opening a project in the editor does on top of loading its model: everything that writes files,
    /// fixes up the .csproj, or prepares editor state. <see cref="ProjectLoader.LoadProject(string, ProjectLoadScope, GlueFormsCore.Controls.InitializationWindowWpf)"/>
    /// calls these, in order, for <see cref="ProjectLoadScope.EditorSession"/>. A headless tool that loads with
    /// <see cref="ProjectLoadScope.ModelOnly"/> calls the ones its command needs, so the editor and the tool share
    /// one implementation of each.
    /// </summary>
    public class ProjectLoadOperations
    {
        readonly IGlueCommands glueCommands;
        readonly IGlueState glueState;

        public ProjectLoadOperations(IGlueCommands glueCommands, IGlueState glueState)
        {
            this.glueCommands = glueCommands;
            this.glueState = glueState;
        }

        /// <summary>
        /// Rebuilds every referenced file whose built output is older than its source, one at a time (some
        /// builders, such as LibreOffice for .csv, fail when run in parallel).
        /// </summary>
        public void BuildAllOutOfDateFiles()
        {
            TaskManager.Self.AddOrRunIfTasked(() =>
            {
                var project = glueState.CurrentGlueProject;
                if (project != null)
                {
                    foreach (var screen in project.Screens)
                    {
                        BuildIfOutOfDate(screen.ReferencedFiles);
                    }
                    foreach (var entity in project.Entities)
                    {
                        BuildIfOutOfDate(entity.ReferencedFiles);
                    }
                    BuildIfOutOfDate(project.GlobalFiles);
                }
            },
            "Build all out of date files");
        }

        void BuildIfOutOfDate(List<ReferencedFileSave> files)
        {
            if (files.Any(item => item == null))
            {
                throw new ArgumentException("List contains null files, which it should not!");
            }

            foreach (var rfs in files)
            {
                if (rfs.GetIsBuiltFileOutOfDate())
                {
                    string error = rfs.PerformExternalBuild(runAsync: false);

                    if (!string.IsNullOrEmpty(error))
                    {
                        FileErrorReporter.ReportError(glueCommands.GetAbsoluteFileName(rfs), error, false);
                    }
                }
            }
        }

        /// <summary>
        /// Offers to re-create the custom code file (Screens/MyScreen.cs) of any element that is missing one.
        /// </summary>
        public void CheckForMissingCustomCodeFiles()
        {
            // A project Glue does not generate code for has no custom code file to be missing - nothing
            // ever creates one. Without this the prompt fires for every screen and every entity, on
            // every load, offering to re-create a file that should not exist.
            if (!CodeWritePolicy.WritesCodeForCurrentProject)
            {
                return;
            }

            var project = glueState.CurrentGlueProject;
            foreach (var element in project.Screens.Cast<GlueElement>().Concat(project.Entities))
            {
                string fileToSearchFor = FileManager.RelativeDirectory + element.Name + ".cs";

                if (!System.IO.File.Exists(fileToSearchFor))
                {
                    var message = "The following file is missing\n\n" + fileToSearchFor +
                        "\n\nwhich is used by\n\n" + element.ToString() + "\n\nWhat would you like to do?";

                    // Escape (no button click) returns default(DialogResult) (None), which isn't OK, so this
                    // falls through to "do nothing" - same as the original "Ignore this problem" behavior.
                    var choice = DialogService.ShowChoice(message,
                        ("Re-create an empty custom code file", DialogResult.OK),
                        ("Ignore this problem", DialogResult.Cancel));

                    if (choice == DialogResult.OK)
                    {
                        glueCommands.GenerateCodeCommands.GenerateElementCustomCode(element);
                    }
                }
            }
        }

        /// <summary>
        /// Writes IEntityFactory.Generated.cs and PoolList.Generated.cs, which every factory's generated code uses.
        /// </summary>
        public void GeneratePerformanceTypes() => FactoryElementCodeGenerator.AddGeneratedPerformanceTypes();

        /// <summary>
        /// Saves what the load's in-memory fix-ups (<c>GlueProjectSave.FixErrors</c>) changed.
        /// </summary>
        public void SaveFixedUpElements(ChangedObjects changedObjects)
        {
            if (changedObjects.DidGlobalContentChange)
            {
                glueCommands.GluxCommands.SaveGlujFile();
            }
            foreach (var entity in changedObjects.ChangedEntitiySaves)
            {
                glueCommands.GluxCommands.SaveElementAsync(entity);
            }
            foreach (var screen in changedObjects.ChangedScreenSaves)
            {
                glueCommands.GluxCommands.SaveElementAsync(screen);
            }
        }

        /// <summary>
        /// Has every plugin write its project-level code and files (<see cref="PluginBase.GenerateProjectCode"/>):
        /// Gum runtimes, Tiled's embedded classes, the camera setup, file aliases and the like. Plugins queue most
        /// of this as tasks.
        /// </summary>
        public void GeneratePluginProjectCode() => PluginManager.GenerateProjectCode();

        public void GenerateAllCode() => glueCommands.GenerateCodeCommands.GenerateAllCode();

        /// <summary>
        /// GitHub issue #2103: a Screen/Entity deleted or renamed outside Glue's own delete/rename flow
        /// (a manual .glux edit, a merge, an older Glue version) can leave its ".Generated.cs" still
        /// referenced in the .csproj, producing CS2001 the next time the project builds. Run after
        /// <see cref="GenerateAllCode"/>: any file a live element still owns has then been (re)created, so
        /// anything still missing and unclaimed by a current element is a leftover from an incomplete cleanup,
        /// not a fresh checkout waiting on codegen. Also covers Gum Forms-generated files (GitHub issue
        /// #2185), whose ownership GumPlugin reports back through the plugin call below since core Glue has no
        /// ownership model for Gum elements.
        /// </summary>
        public void RemoveOrphanedGeneratedCsprojEntries()
        {
            var mainProject = glueState.CurrentMainProject;
            var project = glueState.CurrentGlueProject;
            if (mainProject == null || project == null)
            {
                return;
            }

            var allElements = project.Screens.Cast<GlueElement>().Concat(project.Entities);

            var formsOwnedRelativePaths =
                PluginManager.CallPluginMethod("Gum Plugin", "GetFormsGeneratedRelativePaths") as IEnumerable<string>;

            var removed = mainProject.RemoveOrphanedGeneratedCompileItems(allElements, formsOwnedRelativePaths);

            if (removed.Count > 0)
            {
                foreach (var relativePath in removed)
                {
                    PluginManager.ReceiveOutput("Removed orphaned csproj entry (no backing file, no owning element): " + relativePath);
                }

                glueCommands.ProjectCommands.SaveProjects();
            }
        }

        /// <summary>
        /// Records which content files the .csproj has that nothing in the project references. Removing a file
        /// later compares against this list to find the files that removal left unreferenced, so it must run
        /// before any file is removed.
        /// </summary>
        public void RefreshUnreferencedFiles() => UnreferencedFilesManager.Self.TryRefreshUnreferencedFiles(true);
    }
}
