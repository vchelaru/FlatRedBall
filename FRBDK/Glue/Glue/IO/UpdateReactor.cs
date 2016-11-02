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

namespace FlatRedBall.Glue.IO
{
    public static class UpdateReactor
    {
        static object mUpdateFileLock = new object();
        public static bool UpdateFile(string changedFile)
        {
            bool shouldSave = false;
            bool handled = false;
                
            lock (mUpdateFileLock)
            {
                
                handled = TryHandleProjectFileChanges(changedFile);

                var standardizedGlux = FileManager.RemoveExtension(FileManager.Standardize(ProjectManager.ProjectBase.FullFileName).ToLower()) + ".glux";
                var partialGlux = FileManager.RemoveExtension(FileManager.Standardize(ProjectManager.ProjectBase.FullFileName).ToLower()) + @"\..*\.generated\.glux";
                var partialGluxRegex = new Regex(partialGlux);
                if(!handled && ((changedFile.ToLower() == standardizedGlux) || partialGluxRegex.IsMatch(changedFile.ToLower())))
                {
                    TaskManager.Self.OnUiThread( ReloadGlux);
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
                    ReferencedFileSave rfs = ObjectFinder.Self.GetReferencedFileSaveFromFile(changedFile);



                    if (rfs != null && (extension == "csv" ||
                        rfs.TreatAsCsv))
                    {
                        try
                        {
                            CsvCodeGenerator.GenerateAndSaveDataClass(rfs, rfs.CsvDelimiter);

                            shouldSave = true;
                        }
                        catch(Exception e)
                        {
                            MessageBox.Show("Error saving Class from CSV " + rfs.Name);
                        }
                    }
                }


                #endregion

                #region If it's a file that references other content we may need to update the project

                if (FileHelper.DoesFileReferenceContent(changedFile))
                {
                    ReferencedFileSave rfs = ObjectFinder.Self.GetReferencedFileSaveFromFile(changedFile);


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

                        handled |= ProjectManager.UpdateFileMembershipInProject(rfs);
                        shouldSave = true;

                        MainGlueWindow.Self.Invoke((MethodInvoker)delegate
                        {
                            if (rfs.GetContainerType() == ContainerType.Entity)
                            {
                                if (EditorLogic.CurrentEntityTreeNode != null)
                                {
                                    if (EditorLogic.CurrentEntitySave == rfs.GetContainer())
                                    {
                                        PluginManager.RefreshCurrentElement();
                                    }
                                }
                            }
                            else if (rfs.GetContainerType() == ContainerType.Screen)
                            {
                                if (EditorLogic.CurrentScreenTreeNode != null)
                                {
                                    if (EditorLogic.CurrentScreenSave == rfs.GetContainer())
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

                            shouldSave |= ProjectManager.UpdateFileMembershipInProject(
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
                    ElementViewWindow.Invoke((MethodInvoker)delegate
                    {
                        // It's a directory, so let's just rebuild our directory TreeNodes
                        ElementViewWindow.AddDirectoryNodes();
                    });
                }

                #endregion


                #region Check for broken references to objects in file - like an Entity may reference an object in a file but it may have been removed

                if (ObjectFinder.Self.GetReferencedFileSaveFromFile(changedFile) != null)
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
                    ProjectManager.SaveProjects();
                }

            }

            return handled;
        }

        private static bool TryHandleProjectFileChanges(string changedFile)
        {
            bool handled = false;
            
            handled = TryHandleSpecificProjectFileChange(changedFile, ProjectManager.ProjectBase);

            // Can't foreach because TryHandleSpecificProjectFileChange may modify it.
            for (int i = 0; i < ProjectManager.SyncedProjects.Count; i++)
            {
                var project = ProjectManager.SyncedProjects[i];
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

                    TaskManager.Self.OnUiThread(()=>ProjectLoader.Self.LoadProject(ProjectManager.ProjectBase.FullFileName));
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
                    // Reload the synced content project
                }
                handled = true;
            }


            if (handled)
            {
                PluginManager.ReceiveOutput("Handled changed project file for project: " + changedFile);
            }

            return handled;
        }

        private static void ReloadGlux()
        {
            object selectedObject = null;
            IElement parentElement = null;
            PluginManager.ReceiveOutput("Reloading .glux");
            if (GlueState.Self.CurrentTreeNode != null)
            {
                selectedObject = GlueState.Self.CurrentTreeNode.Tag;
                if (selectedObject is NamedObjectSave)
                {
                    parentElement = ((NamedObjectSave)selectedObject).GetContainer();
                }
            }

            bool usingQuickReload = true;

            if (usingQuickReload)
            {
                GlueProjectSave newGlueProjectSave;
                CompareObjects comparisonObject = ProjectManager.GlueProjectSave.ReloadUsingComparison(ProjectManager.GlueProjectFileName, out newGlueProjectSave);

                if (comparisonObject != null && comparisonObject.Differences.Count != 0)
                {
                    // See if only a Screen or Entity changed.  If so, do a simple set of that and be done with it


                    bool wasHandled = true;

                    List<string> elementsAlreadyRefreshed = new List<string>();

                    //if (comparisonObject.Differences.Count == 1)
					foreach(string compDifference in comparisonObject.Differences)
                    {
						ComparisonDifference comparison = comparisonObject.GetComparisonDifference(compDifference);
                            //comparisonObject.GetComparisonDifference(comparisonObject.Differences[0]);
                        int indexInOld;
                        int indexInNew;
                        IElement element = GetElementFromObjectString(comparison.FullObject1, ProjectManager.GlueProjectSave, out indexInOld);
                        IElement replacement = GetElementFromObjectString(comparison.FullObject1, newGlueProjectSave, out indexInNew);

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


                                treeNode.SaveObjectAsElement = replacement;


                                // Gotta regen this and update the UI and refresh the PropertyGrid if it's selected
                                GlueCommands.Self.UpdateCommands.Update(replacement);

                            }
						}
						else
						{
							wasHandled = false;
							break;
						}
                    }
                    if (!wasHandled)
                    {
                        ProjectLoader.Self.LoadProject(ProjectManager.ProjectBase.FullFileName);
                    }
                }
            }
            else
            {
                ProjectLoader.Self.LoadProject(ProjectManager.ProjectBase.FullFileName);
            }
            PluginManager.RefreshGlux();
            

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

                                 ElementViewWindow.SelectedNode = GlueState.Self.Find.NamedObjectTreeNode(newNos);
                             }));


                    }

                }                
            }
        }

        private static IElement GetElementFromObjectString(string element, GlueProjectSave glueProjectSave, out int index)
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
                    // See if there is an element
                    string name = FileManager.MakeRelative(absoluteName);

                    string directory = FileManager.GetDirectory(name, RelativeType.Relative);

                    string nameWithoutDirectory = FileManager.RemovePath(absoluteName);

                    int indexOfDot = nameWithoutDirectory.IndexOf('.');
                    nameWithoutDirectory = nameWithoutDirectory.Substring(0, indexOfDot);

                    string elementName = directory + nameWithoutDirectory;

                    IElement element = ObjectFinder.Self.GetIElement(elementName);

                    if (element != null)
                    {
                        GlueState.Self.Find.ElementTreeNode(element).UpdateReferencedTreeNodes(performSave:false);

                        if (element == EditorLogic.CurrentElement && EditorLogic.CurrentEventResponseSave != null &&
                            MainGlueWindow.Self.CodeEditor.ContainsFocus == false)
                        {
                            // Update the script window if this is a script file.
                            MainGlueWindow.Self.CodeEditor.UpdateDisplayToCurrentObject();
                        }

                    }
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

                                if (entity == EditorLogic.CurrentEntitySave)
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
                ProjectManager.SaveProjects();
            }


        }
    }
}
