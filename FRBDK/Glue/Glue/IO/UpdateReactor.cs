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

namespace FlatRedBall.Glue.IO
{
    public static class UpdateReactor
    {
        public const string ReloadingProjectDescription = "Reloading Project";
        //static object mUpdateFileLock = new object();
        public static bool UpdateFile(string changedFile)
        {
            bool shouldSave = false;
            bool handled = false;
                
            //lock (mUpdateFileLock)
            {
                string projectFileName = ProjectManager.ProjectBase?.FullFileName;
                if(ProjectManager.ProjectBase != null)
                {
                    handled = TryHandleProjectFileChanges(changedFile);

                    var standardizedGlux = FileManager.RemoveExtension(FileManager.Standardize(projectFileName).ToLower()) + ".glux";
                    var standardizedGluj = FileManager.RemoveExtension(FileManager.Standardize(projectFileName).ToLower()) + ".gluj";
                    var partialGlux = FileManager.RemoveExtension(FileManager.Standardize(projectFileName).ToLower()) + @"\..*\.generated\.glux";
                    var partialGluxRegex = new Regex(partialGlux);
                    if(!handled && (
                        changedFile.ToLower() == standardizedGlux || partialGluxRegex.IsMatch(changedFile.ToLower()) || changedFile.ToLower() == standardizedGluj
                        )
                    )
                    {
                        if(!ProjectManager.WantsToClose)
                        {
                            ReloadGlux();
                        }
                        handled = true;
                    }

                    if (ProjectManager.IsContent(changedFile))
                    {
                        PluginManager.ReactToChangedFile(changedFile);
                    }

                    #region If it's a CSV, then re-generate the code for the objects

                    string extension = FileManager.GetExtension(changedFile);

                    if (extension == "csv" ||
                        extension == "txt")
                    {
                        ReferencedFileSave rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(changedFile);

                        bool shouldGenerate = rfs != null &&
                            (extension == "csv" || rfs.TreatAsCsv) &&
                            rfs.IsDatabaseForLocalizing == false;

                        if (shouldGenerate)
                        {
                            try
                            {
                                CsvCodeGenerator.GenerateAndSaveDataClass(rfs, rfs.CsvDelimiter);

                                shouldSave = true;
                            }
                            catch (Exception e)
                            {
                                GlueCommands.Self.PrintError("Error saving Class from CSV " + rfs.Name +
                                    "\n" + e.ToString());
                            }
                        }
                    }


                    #endregion

                    #region If it's a file that references other content we may need to update the project

                    if (FileHelper.DoesFileReferenceContent(changedFile))
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

                            if (ProjectManager.ContentProject.IsFilePartOfProject(changedFile))
                            {
                                string relativePath = ProjectManager.MakeRelativeContent(changedFile);

                                shouldSave |= GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(
                                    ProjectManager.ProjectBase, relativePath, false, false);
                                handled |= shouldSave;

                            }
                            
                        }
                    }

                    #endregion

                    #region If it's a .cs file, we should see if we've added a new .cs file, and if so refresh the Element for it
                    if (extension == "cs")
                    {
                        TaskManager.Self.OnUiThread(()=>ReactToChangedCodeFile(changedFile));

                    }


                    #endregion

                    #region Maybe it's a directory that was added or removed

                    if (FileManager.GetExtension(changedFile) == "")
                    {
                        MainGlueWindow.Self.Invoke((MethodInvoker)delegate
                        {
                            try
                            {
                                // It's a directory, so let's just rebuild our directory TreeNodes
                                ElementViewWindow.AddDirectoryNodes();
                            }
                            catch(System.IO.IOException)
                            {
                                // this could be because something else is accessing the directory, so sleep, try again
                                System.Threading.Thread.Sleep(100);
                                ElementViewWindow.AddDirectoryNodes();
                            }
                        });
                    }

                    #endregion


                    #region Check for broken references to objects in file - like an Entity may reference an object in a file but it may have been removed

                    if (GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(changedFile) != null)
                    {
                        // This is a file that is part of the project, so let's see if any named objects are missing references
                        CheckForBrokenReferencesToObjectsInFile(changedFile);
                    }

                    #endregion

                    // This could be an externally built file:

                    ProjectManager.UpdateExternallyBuiltFile(changedFile);

                    if (handled)
                    {
                        PluginManager.ReceiveOutput("Handled changed file: " + changedFile);

                    }

                    if (shouldSave)
                    {
                        GlueCommands.Self.ProjectCommands.SaveProjects();
                    }
                }

            }

            return handled;
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
            string standardizedProject = FileManager.Standardize(project.FullFileName).ToLower();
            string standardizedContentProject = null;
            bool handled = false;

            if (project.ContentProject != null)
            {
                standardizedContentProject = FileManager.Standardize(project.ContentProject.FullFileName).ToLower();
            }



            if (standardizedProject == changedFile.ToLower())
            {
                if (project == ProjectManager.ProjectBase)
                {
                    TaskManager.Self.OnUiThread(async ()=>
                    {
                        await GlueCommands.Self.LoadProjectAsync(changedFile);
                    });
                }
                else
                {
                    // Just reload the synced project
                    if (ProjectManager.SyncedProjects.Contains(project))
                    {
                        ProjectManager.RemoveSyncedProject(project);
                    }

                    ProjectLoader.AddSyncedProjectToProjectManager(project.FullFileName);
                }
                handled = true;
            }
            else if (!string.IsNullOrEmpty(standardizedContentProject) &&
                standardizedContentProject == changedFile.ToLower())
            {

                if (project == ProjectManager.ContentProject)
                {
                    TaskManager.Self.OnUiThread(()=>ProjectLoader.Self.LoadProject(ProjectManager.ProjectBase.FullFileName));
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

        private static async void ReloadGlux()
        {
            object selectedObject = null;
            IElement parentElement = null;
            PluginManager.ReceiveOutput("Reloading Glue Project");

            TaskManager.Self.OnUiThread(() =>
            {
                if (GlueState.Self.CurrentTreeNode != null)
                {
                    selectedObject = GlueState.Self.CurrentTreeNode.Tag;
                    if (selectedObject is NamedObjectSave)
                    {
                        parentElement = ((NamedObjectSave)selectedObject).GetContainer();
                    }
                }
            });

            bool usingQuickReload = true;

            if (usingQuickReload)
            {
                GlueProjectSave newGlueProjectSave = null;
                bool wasHandled = false;
                ComparisonResult compareResult = null;

                // March 1, 2020 - this can fail on int comparison so...we'll just tolerate it and do a full reload:
                try
                {
                    compareResult = ProjectManager.GlueProjectSave.ReloadUsingComparison(GlueState.Self.GlueProjectFileName, out newGlueProjectSave);
                }
                catch
                {
                    // write out put?
                }

                if (compareResult != null && compareResult.Differences.Count != 0)
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
                        var element = GetElementFromObjectString(comparison.PropertyName, ProjectManager.GlueProjectSave, out indexInOld);
                        GlueElement replacement = GetElementFromObjectString(comparison.PropertyName, newGlueProjectSave, out indexInNew);

                        if (element != null && replacement != null && indexInNew == indexInOld)
						{
                            if (!elementsAlreadyRefreshed.Contains(element.Name))
                            {
                                elementsAlreadyRefreshed.Add(element.Name);
                                if (element is ScreenSave)
                                {
                                    ProjectManager.GlueProjectSave.Screens[indexInOld] = newGlueProjectSave.Screens[indexInNew];
                                }
                                else // element is EntitySave
                                {
                                    ProjectManager.GlueProjectSave.Entities[indexInOld] = newGlueProjectSave.Entities[indexInNew];
                                }

                                var treeNode = GlueState.Self.Find.ElementTreeNode(element);


                                treeNode.SaveObject = replacement;


                                // Gotta regen this and update the UI and refresh the PropertyGrid if it's selected
                                GlueCommands.Self.UpdateCommands.Update(replacement);

                                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
                            }
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
                    await ProjectLoader.Self.LoadProject(ProjectManager.ProjectBase.FullFileName);
                }
            }
            else
            {
                await ProjectLoader.Self.LoadProject(ProjectManager.ProjectBase.FullFileName);
            }
            

            // Now that everything is done we want to re-select the same object (if we can)
            if (parentElement != null)
            {
                IElement newElement = ObjectFinder.Self.GetIElement(parentElement.Name);

                if (newElement != null)
                {
                    if(selectedObject != null && selectedObject is NamedObjectSave)
                    {

                        MainGlueWindow.Self.BeginInvoke(
                         new EventHandler(delegate 
                             {
                                 NamedObjectSave newNos = newElement.GetNamedObject(((NamedObjectSave)selectedObject).InstanceName);

                                 // forces a refresh:
                                 ElementViewWindow.SelectedNode = null;
                                 ElementViewWindow.SelectedNode = GlueState.Self.Find.NamedObjectTreeNode(newNos);

                             }));


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

        private static void ReactToChangedCodeFile(string codeFileName)
        {
            
            string absoluteName = ProjectManager.MakeAbsolute(codeFileName);

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
