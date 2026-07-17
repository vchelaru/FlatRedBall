using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using Glue;
using FlatRedBall.Glue.Controls;
using EditorObjects.Parsing;
using FlatRedBall.Glue.VSHelpers;
using KellermanSoftware.CompareNetObjects;
using System.Text.RegularExpressions;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System.Windows.Forms;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Parsing;
using System.Security.Cryptography.Pkcs;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.IO
{
    public static class UpdateReactor
    {
        #region Fields/Properties

        public const string ReloadingProjectDescription = "Reloading Project";
        //static object mUpdateFileLock = new object();

        #endregion

        public static async Task<bool> UpdateFile(FilePath changedFile, FileChangeType changeType = FileChangeType.Modified)
        {
            bool handled = false;
            ///////////////Early Out////////////////////
            if(GlueState.Self.CurrentMainProject == null)
            {
                return handled;
            }
            ////////////End Early Out//////////////////

            bool shouldSave = false;
                
            var projectFileName = GlueState.Self.CurrentMainProject?.FullFileName.FullPath;

            handled = TryHandleProjectFileChanges(changedFile.FullPath);
            bool isGlueProjectOrElementFile = GetIfIsGlueProjectOrElementFile(changedFile.FullPath, projectFileName);
            if (!handled && isGlueProjectOrElementFile)
            {
                if (!ProjectManager.WantsToCloseProject)
                {
                    await ReloadGlux();
                }
                handled = true;
            }

            if (! handled)
            {
                var isContent = GlueCommands.Self.FileCommands.IsContent(changedFile) ||
                    // If a folder changes relative to the content directory, then consider that content so
                    // plugins can respond to the changed directory
                    changedFile.IsRelativeTo(GlueState.Self.ContentDirectory);

                if(isContent)
                {
                    PluginManager.ReactToChangedFile(changedFile, changeType);
                }
            }

            #region If it's a file that references other content we may need to update the project

            if (FileHelper.DoesFileReferenceContent(changedFile.FullPath))
            {
                ReferencedFileSave rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(changedFile);


                if (rfs != null)
                {
                    string error;
                    rfs.RefreshSourceFileCache(false, out error);

                    if (!string.IsNullOrEmpty(error))
                    {
                        FileErrorReporter.ReportError(rfs.Name, error, false);
                    }
                    else
                    {
                        handled = true;
                    }

                    handled |= GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(rfs);
                    shouldSave = true;
                }
                else
                {
                    // There may not be a RFS for this in Glue, but even if there's not,
                    // this file may be referenced by other RFS's.  I don't want to do a full
                    // project scan, so we'll just see if this file is part of Visual Studio.  If so
                    // then let's add its children

                    if (ProjectManager.ContentProject.IsFilePartOfProject(changedFile.FullPath))
                    {
                        FilePath changedFilePath = changedFile;
                        shouldSave |= GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(
                            GlueState.Self.CurrentMainProject, changedFilePath, false, false);
                        handled |= shouldSave;

                    }

                }
            }

            #endregion

            var extension = changedFile.Extension;

            #region If it's a .cs file, we should see if we've added a new .cs file, and if so refresh the Element for it
            if (extension == "cs")
            {
                TaskManager.Self.OnUiThread(() => ReactToChangedCodeFile(changedFile.FullPath));

            }


            #endregion

            #region Maybe it's a directory that was added or removed

            if (changedFile.Extension == "")
            {
                MainGlueWindow.Self.Invoke((MethodInvoker)delegate
                {
                    try
                    {
                        // It's a directory, so let's just rebuild our directory TreeNodes
                        GlueCommands.Self.RefreshCommands.RefreshDirectoryTreeNodes();
                    }
                    catch (System.IO.IOException)
                    {
                        // this could be because something else is accessing the directory, so sleep, try again
                        System.Threading.Thread.Sleep(100);
                        GlueCommands.Self.RefreshCommands.RefreshDirectoryTreeNodes();
                    }
                });
            }

            #endregion


            #region Check for broken references to objects in file - like an Entity may reference an object in a file but it may have been removed

            if (GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(changedFile) != null)
            {
                // This is a file that is part of the project, so let's see if any named objects are missing references
                CheckForBrokenReferencesToObjectsInFile(changedFile.FullPath);
            }

            #endregion

            // This could be an externally built file:

            ProjectManager.UpdateExternallyBuiltFile(changedFile.FullPath);

            if (handled && FileWatchManager.IsPrintingDiagnosticOutput)
            {
                PluginManager.ReceiveOutput("Handled changed file: " + changedFile);

            }

            if (shouldSave)
            {
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }

            return handled;
        }

        private static bool GetIfIsGlueProjectOrElementFile(string changedFile, string projectFileName)
        {
            var standardizedGlux = FileManager.RemoveExtension(FileManager.Standardize(projectFileName).ToLower()) + ".glux";
            var standardizedGluj = FileManager.RemoveExtension(FileManager.Standardize(projectFileName).ToLower()) + ".gluj";
            var partialGlux = FileManager.RemoveExtension(FileManager.Standardize(projectFileName).ToLower()) + @"\..*\.generated\.glux";
            var partialGluxRegex = new Regex(partialGlux);
            var isGlueProjectFile = String.Equals(changedFile, standardizedGlux, StringComparison.OrdinalIgnoreCase) || partialGluxRegex.IsMatch(changedFile.ToLowerInvariant()) ||
                                    String.Equals(changedFile, standardizedGluj, StringComparison.OrdinalIgnoreCase);
            var isElementFile = false;
            if(!isGlueProjectFile)
            {
                var extension = FileManager.GetExtension(changedFile);

                if(extension is GlueProjectSave.ScreenExtension or GlueProjectSave.EntityExtension)
                {
                    var projectDirectory = FileManager.GetDirectory(projectFileName);

                    var isRelativeToProject = FileManager.IsRelativeTo(changedFile, projectDirectory);

                    isElementFile = isRelativeToProject;
                    // is it relative to the project?
                }
            }

            return isGlueProjectFile || isElementFile;
        }

        private static bool TryHandleProjectFileChanges(string changedFile)
        {
            bool handled = false;

            var project = GlueState.Self.CurrentMainProject;

            if(project != null)
            {
                handled = TryHandleSpecificProjectFileChange(changedFile, project);
            }

            // Can't foreach because TryHandleSpecificProjectFileChange may modify it.
            for (int i = 0; i < ProjectManager.SyncedProjects.Count; i++)
            {
                project = (VisualStudioProject)ProjectManager.SyncedProjects[i];
                if (handled)
                {
                    break;
                }
                handled = TryHandleSpecificProjectFileChange(changedFile, project);
            }

            

            return handled;
        }

        private static bool TryHandleSpecificProjectFileChange(string changedFile, ProjectBase project)
        {
            var standardizedProject = project.FullFileName;
            var standardizedContentProject = project.ContentProject?.FullFileName;
            bool handled = false;

            if (standardizedProject == changedFile)
            {
                if (project == GlueState.Self.CurrentMainProject)
                {
                    if(!ProjectManager.WantsToCloseProject)
                    {
                        //TaskManager.Self.OnUiThread(()=>
                        //{
                        //    return GlueCommands.Self.LoadProjectAsync(changedFile);
                        //});
                        // Whenever files flush, there are times when there are multiple files. We want to add or move to end so the other files have a chance to load:
                        TaskManager.Self.Add(
                            () => GlueCommands.Self.LoadProjectAsync(changedFile),
                            "Reloading Project due to changed file", 
                            TaskExecutionPreference.AddOrMoveToEnd, 
                            doOnUiThread: false);
                    }
                }
                else
                {
                    // Just reload the synced project
                    if (GlueState.Self.SyncedProjects.Contains(project))
                    {
                        GlueState.Self.SyncedProjects.Remove(project);
                    }

                    ProjectLoader.AddSyncedProjectToProjectManager(project.FullFileName.FullPath);
                }
                handled = true;
            }
            else if (standardizedContentProject != null && standardizedContentProject == changedFile)
            {

                if (project == ProjectManager.ContentProject)
                {
                    TaskManager.Self.OnUiThread(()=>ProjectLoader.Self.LoadProject(GlueState.Self.CurrentMainProject.FullFileName.FullPath));
                }
                else
                {
                    TaskManager.Self.OnUiThread(() =>
                    {
                        // Reload the synced content project
                        project.ContentProject.Unload();
                        project.LoadContentProject();

                    });
                }
                handled = true;
            }


            if (handled)
            {
                PluginManager.ReceiveOutput("Handled changed project file for project: " + changedFile);
            }

            return handled;
        }

        private static async Task ReloadGlux()
        {
            object selectedObject = null;
            PluginManager.ReceiveOutput("Reloading FlatRedBall Project");

            var parentElement = GlueState.Self.CurrentNamedObjectSave?.GetContainer();

            GlueProjectSave newGlueProjectSave = null;
            bool wasHandled = false;
            ComparisonResult compareResult = null;

            try
            {
                // March 1, 2020 - this can fail on int comparison so...we'll just tolerate it and do a full reload:
                compareResult = ProjectManager.GlueProjectSave.ReloadUsingComparison(GlueState.Self.GlueProjectFileName.FullPath, out newGlueProjectSave);
            }
            catch
            {
                // write out put?
            }

            if (compareResult != null)
            {
                var plan = BuildProjectDiffPlan(
                    compareResult.Differences.Select(d => d.PropertyName),
                    ProjectManager.GlueProjectSave,
                    newGlueProjectSave);

                // Apply whatever collapsed to a single element/file BEFORE checking the outcome.
                // When the plan ends in FullReloadRequired, this is only the prefix of replacements
                // that were resolved before the diff that forced the full reload - matching the
                // original inline loop, which mutated/regenerated each element as it went and only
                // then bailed on the first unresolvable diff. Those swaps get overwritten by the
                // full reload that follows anyway, so this is redundant work, not a correctness
                // issue - but it's the existing, pinned behavior, so it's preserved as-is here.
                foreach (var replacement in plan.ElementsToReplace)
                {
                    if (replacement.OldElement is ScreenSave)
                    {
                        ProjectManager.GlueProjectSave.Screens[replacement.Index] = (ScreenSave)replacement.ReplacementElement;
                    }
                    else // element is EntitySave
                    {
                        ProjectManager.GlueProjectSave.Entities[replacement.Index] = (EntitySave)replacement.ReplacementElement;
                    }

                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(replacement.OldElement);

                    // Gotta regen this and update the UI and refresh the PropertyGrid if it's selected
                    GlueCommands.Self.UpdateCommands.Update(replacement.ReplacementElement);

                    // Jan 2, 2023
                    // Not sure why
                    // we generate the
                    // old one, it should
                    // be the new one because
                    // the old one is no longer
                    // part of the GlueProjectSave
                    // so finding references during
                    // codegen will not work correctly.
                    //GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(replacement.OldElement);
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(replacement.ReplacementElement);
                }

                foreach (var replacement in plan.GlobalFilesToReplace)
                {
                    ProjectManager.GlueProjectSave.GlobalFiles[replacement.Index] = replacement.ReplacementFile;

                    GlueCommands.Self.RefreshCommands.RefreshGlobalContent();

                    GlueCommands.Self.GenerateCodeCommands.GenerateGlobalContentCode();
                }

                foreach (var propertyName in plan.TopLevelPropertiesChanged)
                {
                    // Add a case here for each entry added to DiffableTopLevelProperties.
                    switch (propertyName)
                    {
                        case nameof(GlueProjectSave.StartUpScreen):
                            var oldStartUpScreen = ProjectManager.GlueProjectSave.Screens
                                .FirstOrDefault(item => item.Name == ProjectManager.GlueProjectSave.StartUpScreen);

                            ProjectManager.GlueProjectSave.StartUpScreen = newGlueProjectSave.StartUpScreen;

                            GlueCommands.Self.GenerateCodeCommands.GenerateStartupScreenCode();

                            var newStartUpScreen = ProjectManager.GlueProjectSave.Screens
                                .FirstOrDefault(item => item.Name == ProjectManager.GlueProjectSave.StartUpScreen);
                            if (oldStartUpScreen != null)
                            {
                                GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(oldStartUpScreen);
                            }
                            if (newStartUpScreen != null)
                            {
                                GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(newStartUpScreen);
                            }

                            PluginManager.ReactToChangedStartupScreen();
                            break;
                    }
                }

                wasHandled = plan.Outcome != ProjectDiffOutcome.FullReloadRequired;
            }
            if (!wasHandled)
            {
                await ProjectLoader.Self.LoadProject(GlueState.Self.CurrentMainProject.FullFileName.FullPath);
            }
            

            // Now that everything is done we want to re-select the same object (if we can)
            if (parentElement != null)
            {
                var newElement = ObjectFinder.Self.GetElement(parentElement.Name);

                if (newElement != null)
                {
                    if(selectedObject != null && selectedObject is NamedObjectSave)
                    {
                        GlueCommands.Self.DoOnUiThread(() =>
                        {
                            NamedObjectSave newNos = newElement.GetNamedObject(((NamedObjectSave)selectedObject).InstanceName);

                            // forces a refresh:
                            GlueState.Self.CurrentNamedObjectSave = null;
                            GlueState.Self.CurrentNamedObjectSave = newNos;
                        });
                    }
                }                
            }
        }

        /// <summary>
        /// Top-level <see cref="GlueProjectSave"/> properties that are safe to apply directly onto the
        /// live project instead of forcing a full reload. Each entry here needs a matching case in
        /// <see cref="ReloadGlux"/>'s apply step to copy the value and trigger whatever codegen/refresh
        /// that property change requires. Kept as an explicit whitelist rather than reflecting over every
        /// top-level property - most project-level properties (e.g. <see cref="GlueProjectSave.CustomGameClass"/>,
        /// <see cref="GlueProjectSave.SuppressBaseTypeGeneration"/>) affect generated code in ways that
        /// haven't been individually verified safe to apply without a full reload.
        /// </summary>
        static readonly HashSet<string> DiffableTopLevelProperties = new HashSet<string>
        {
            nameof(GlueProjectSave.StartUpScreen)
        };

        /// <summary>
        /// Classifies a set of CompareNetObjects difference property paths (e.g. "Screens[3].SomeProperty")
        /// against the currently-loaded project and a freshly-reloaded copy. A difference collapses to a
        /// single Screen/Entity/GlobalFile replacement when its path falls entirely within one
        /// Screens[i]/Entities[i]/GlobalFiles[i] entry, or to a <see cref="ProjectDiffPlan.TopLevelPropertiesChanged"/>
        /// entry when it's an exact match for a name in <see cref="DiffableTopLevelProperties"/>. Any other
        /// difference - an unrecognized project-level property, or a list Count change caused by an
        /// element being added/removed/reordered - forces <see cref="ProjectDiffOutcome.FullReloadRequired"/>.
        /// Pure/static: no Glue statics are touched, which is what makes this seam unit-testable independent
        /// of the rest of <see cref="ReloadGlux"/>.
        /// </summary>
        internal static ProjectDiffPlan BuildProjectDiffPlan(
            IEnumerable<string> differencePropertyNames,
            GlueProjectSave oldProjectSave,
            GlueProjectSave newProjectSave)
        {
            var plan = new ProjectDiffPlan();
            var elementsAlreadyRefreshed = new List<string>();
            bool hasAnyDifference = false;

            foreach (var propertyName in differencePropertyNames)
            {
                hasAnyDifference = true;

                var oldElement = GetElementFromObjectString(propertyName, oldProjectSave, out int indexInOld);
                GlueElement replacementElement = GetElementFromObjectString(propertyName, newProjectSave, out int indexInNew);

                ReferencedFileSave oldFile = null;
                ReferencedFileSave replacementFile = null;
                int fileIndexInOld = -1;
                int fileIndexInNew = -1;
                if (oldElement == null && replacementElement == null)
                {
                    oldFile = GetFileFromObjectString(propertyName, oldProjectSave, out fileIndexInOld);
                    replacementFile = GetFileFromObjectString(propertyName, newProjectSave, out fileIndexInNew);
                }

                if (oldElement != null && replacementElement != null && indexInNew == indexInOld)
                {
                    if (!elementsAlreadyRefreshed.Contains(oldElement.Name))
                    {
                        elementsAlreadyRefreshed.Add(oldElement.Name);
                        plan.ElementsToReplace.Add(new ElementDiffReplacement
                        {
                            Index = indexInOld,
                            OldElement = oldElement,
                            ReplacementElement = replacementElement
                        });
                    }
                }
                else if (oldFile != null && replacementFile != null && fileIndexInOld == fileIndexInNew)
                {
                    plan.GlobalFilesToReplace.Add(new GlobalFileDiffReplacement
                    {
                        Index = fileIndexInOld,
                        OldFile = oldFile,
                        ReplacementFile = replacementFile
                    });
                }
                else if (DiffableTopLevelProperties.Contains(propertyName))
                {
                    if (!plan.TopLevelPropertiesChanged.Contains(propertyName))
                    {
                        plan.TopLevelPropertiesChanged.Add(propertyName);
                    }
                }
                else
                {
                    plan.Outcome = ProjectDiffOutcome.FullReloadRequired;
                    return plan;
                }
            }

            plan.Outcome = hasAnyDifference ? ProjectDiffOutcome.Partial : ProjectDiffOutcome.NoDifferences;
            return plan;
        }

        private static GlueElement GetElementFromObjectString(string element, GlueProjectSave glueProjectSave, out int index)
        {
            Regex regex = new Regex(@"(Screens)\[[0-9]+\]");

            Match match = regex.Match(element);
            if (match != Match.Empty && match.Groups.Count > 1 && match.Groups[1].Value == "Screens")
            {
                string matchRegex = @"Screens\[([0-9]+)\]";

                string indexAsString = Regex.Match(element, matchRegex).Groups[1].Value;
                index = int.Parse(indexAsString);

                return glueProjectSave.Screens[index];
            }
            //string screenOrEntity = 

            regex = new Regex(@"(Entities)\[[0-9]+\]");
            match = regex.Match(element);
            if (match != Match.Empty && match.Groups.Count > 1 && match.Groups[1].Value == "Entities")
            {
                string matchRegex = @"Entities\[([0-9]+)\]";

                string indexAsString = Regex.Match(element, matchRegex).Groups[1].Value;
                index = int.Parse(indexAsString);

                return glueProjectSave.Entities[index];
            }
            index = -1;

            return null;
        }

        private static ReferencedFileSave GetFileFromObjectString(string stringPattern, GlueProjectSave glueProjectSave, out int index)
        {

            var regex = new Regex(@"(GlobalFiles)\[[0-9]+\]");
            var match = regex.Match(stringPattern);
            if(match != Match.Empty && match.Groups.Count > 1)
            {
                string indexMatch = @"GlobalFiles\[([0-9]+)\]";
                string indexAsString = Regex.Match(stringPattern, indexMatch).Groups[1].Value;
                index = int.Parse(indexAsString);
                return glueProjectSave.GlobalFiles[index];

            }
            index = -1;

            return null;
        }

        private static void ReactToChangedCodeFile(string codeFileName)
        {
            
            string absoluteName = GlueCommands.Self.GetAbsoluteFileName(codeFileName, false);

            if(FileManager.FileExists(absoluteName))
            {
                bool isGenerated = absoluteName.Contains(".Generated.");

                if (!isGenerated)
                {
                    PluginManager.ReactToChangedCodeFile(new FilePath(absoluteName));
                }
            }
        }

        private static void CheckForBrokenReferencesToObjectsInFile(string changedFile)
        {
            bool shouldSave = false;

            string relativeToContent = FileManager.MakeRelative(changedFile, FileManager.RelativeDirectory);


            for (int i = 0; i < ProjectManager.GlueProjectSave.Entities.Count; i++)
            {
                bool shouldGenerateEntityCode = false;

                EntitySave entity = ProjectManager.GlueProjectSave.Entities[i];

                for (int j = 0; j < entity.NamedObjects.Count; j++)
                {
                    NamedObjectSave namedObjectSave = entity.NamedObjects[j];

                    if (namedObjectSave.SourceType == SourceType.File &&
                        namedObjectSave.SourceFile == relativeToContent)
                    {
                        // verify that the referenced object still exists
                        string objectToFind = namedObjectSave.SourceName;



                        if (!string.IsNullOrEmpty(objectToFind) && objectToFind != "<NONE>")
                        {
                            List<string> namedObjects = new List<string>();
                            
                            ContentParser.GetNamedObjectsIn(namedObjectSave.SourceFile, namedObjects);
                            // FINISH THIS!!!!
                            if (!namedObjects.Contains(objectToFind))
                            {
                                System.Windows.Forms.MessageBox.Show(
                                    string.Format(
                                    "The object {0} references an object {1} in the file {2}.  This object no longer exists, so the object {0} will have its reference set to NONE.",
                                    namedObjectSave.FieldName, objectToFind, namedObjectSave.SourceFile));

                                namedObjectSave.SourceName = "<NONE>";
                                shouldGenerateEntityCode = true;
                                shouldSave = true;

                                if (entity == GlueState.Self.CurrentEntitySave)
                                {
                                    MainGlueWindow.Self.PropertyGrid.Refresh();
                                }
                            }
                        }
                    }
                }

                if (shouldGenerateEntityCode)
                {
                    // is this okay to save and ignore the output?
                    _=CodeWriter.GenerateCode(entity);
                }
            }

            if (shouldSave)
            {
                GluxCommands.Self.SaveProjectAndElements();
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }


        }
    }

    internal enum ProjectDiffOutcome
    {
        NoDifferences,
        Partial,
        FullReloadRequired
    }

    internal class ElementDiffReplacement
    {
        public int Index;
        public GlueElement OldElement;
        public GlueElement ReplacementElement;
    }

    internal class GlobalFileDiffReplacement
    {
        public int Index;
        public ReferencedFileSave OldFile;
        public ReferencedFileSave ReplacementFile;
    }

    internal class ProjectDiffPlan
    {
        public ProjectDiffOutcome Outcome;
        public List<ElementDiffReplacement> ElementsToReplace { get; } = new List<ElementDiffReplacement>();
        public List<GlobalFileDiffReplacement> GlobalFilesToReplace { get; } = new List<GlobalFileDiffReplacement>();
        public List<string> TopLevelPropertiesChanged { get; } = new List<string>();
    }
}
