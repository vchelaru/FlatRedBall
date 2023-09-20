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
            if(ProjectManager.ProjectBase == null)
            {
                return handled;
            }
            ////////////End Early Out//////////////////

            bool shouldSave = false;
                
            var projectFileName = ProjectManager.ProjectBase?.FullFileName.FullPath;

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
                        ErrorReporter.ReportError(rfs.Name, error, false);
                    }
                    else
                    {
                        handled = true;
                    }

                    handled |= GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(rfs);
                    shouldSave = true;

                    MainGlueWindow.Self.Invoke((MethodInvoker)delegate
                    {
                        if (rfs.GetContainerType() == ContainerType.Entity)
                        {
                            if (GlueState.Self.CurrentEntitySave != null)
                            {
                                if (GlueState.Self.CurrentEntitySave == rfs.GetContainer())
                                {
                                    PluginManager.RefreshCurrentElement();
                                }
                            }
                        }
                        else if (rfs.GetContainerType() == ContainerType.Screen)
                        {
                            if (GlueState.Self.CurrentScreenSave != null)
                            {
                                if (GlueState.Self.CurrentScreenSave == rfs.GetContainer())
                                {
                                    PluginManager.RefreshCurrentElement();
                                }
                            }
                        }
                    });
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
                            ProjectManager.ProjectBase, changedFilePath, false, false);
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

            if (handled)
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

            var project = ProjectManager.ProjectBase;

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
                if (project == ProjectManager.ProjectBase)
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
                    if (ProjectManager.SyncedProjects.Contains(project))
                    {
                        ProjectManager.RemoveSyncedProject(project);
                    }

                    ProjectLoader.AddSyncedProjectToProjectManager(project.FullFileName.FullPath);
                }
                handled = true;
            }
            else if (standardizedContentProject != null && standardizedContentProject == changedFile)
            {

                if (project == ProjectManager.ContentProject)
                {
                    TaskManager.Self.OnUiThread(()=>ProjectLoader.Self.LoadProject(ProjectManager.ProjectBase.FullFileName.FullPath));
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

            if(compareResult?.Differences.Count == 0)
            {
                // no changes!
                wasHandled = true;
            }
            else if (compareResult != null && compareResult.Differences.Count != 0)
            {
                // See if only a Screen or Entity changed.  If so, do a simple set of that and be done with it
                wasHandled = true;

                List<string> elementsAlreadyRefreshed = new List<string>();

                //if (comparisonObject.Differences.Count == 1)
				foreach(var comparison in compareResult.Differences)
                {
                        //comparisonObject.GetComparisonDifference(comparisonObject.Differences[0]);
                    int indexInOld = 0;
                    int indexInNew = 0;
                    var oldElement = GetElementFromObjectString(comparison.PropertyName, ProjectManager.GlueProjectSave, out indexInOld);
                    GlueElement replacementElement = GetElementFromObjectString(comparison.PropertyName, newGlueProjectSave, out indexInNew);

                    int fileIndexInOld = -1;
                    int fileIndexInNew = -1;
                    ReferencedFileSave oldFile = null;
                    ReferencedFileSave replacementFile = null;
                    if(oldElement == null && replacementElement == null)
                    {
                        oldFile = GetFileFromObjectString(comparison.PropertyName, ProjectManager.GlueProjectSave, out fileIndexInOld);
                        replacementFile = GetFileFromObjectString(comparison.PropertyName, newGlueProjectSave, out fileIndexInNew);
                        //replacement = GetNamedObjectSaveFromObjectString(comparison.PropertyName, newGlueProjectSave, out indexInNew);
                    }
                    if (oldElement != null && replacementElement != null && indexInNew == indexInOld)
					{
                        if (!elementsAlreadyRefreshed.Contains(oldElement.Name))
                        {
                            elementsAlreadyRefreshed.Add(oldElement.Name);
                            if (oldElement is ScreenSave)
                            {
                                ProjectManager.GlueProjectSave.Screens[indexInOld] = newGlueProjectSave.Screens[indexInNew];
                            }
                            else // element is EntitySave
                            {
                                ProjectManager.GlueProjectSave.Entities[indexInOld] = newGlueProjectSave.Entities[indexInNew];
                            }


                            GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(oldElement);


                            // Gotta regen this and update the UI and refresh the PropertyGrid if it's selected
                            GlueCommands.Self.UpdateCommands.Update(replacementElement);

                            // Jan 2, 2023
                            // Not sure why
                            // we generate the 
                            // old one, it should
                            // be the new one because
                            // the old one is no longer
                            // part of the GlueProjectSave
                            // so finding references during
                            // codegen will not work correctly.
                            //GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(oldElement);
                            GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(replacementElement);
                        }
					}
                    else if(oldFile != null && replacementFile != null && fileIndexInOld == fileIndexInNew)
                    {
                        ProjectManager.GlueProjectSave.GlobalFiles[fileIndexInOld] = newGlueProjectSave.GlobalFiles[fileIndexInNew];

                        GlueCommands.Self.RefreshCommands.RefreshGlobalContent();

                        GlueCommands.Self.GenerateCodeCommands.GenerateGlobalContentCode();
                    }
					else
					{
						wasHandled = false;
						break;
					}
                }
            }
            if (!wasHandled)
            {
                await ProjectLoader.Self.LoadProject(ProjectManager.ProjectBase.FullFileName.FullPath);
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
                    CodeWriter.GenerateCode(entity);
                }
            }

            if (shouldSave)
            {
                GluxCommands.Self.SaveGlux();
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }


        }
    }
}
