using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.IO;
using System.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins;

namespace FlatRedBall.Glue.SaveClasses
{
    public static class IElementHelperGlue
    {
        public static void RenameElement(this IElement elementToRename, string value)
        {
            bool isValid = true;
            string whyItIsntValid;
            if (elementToRename is ScreenSave)
            {
                isValid = NameVerifier.IsScreenNameValid(value, elementToRename as ScreenSave, out whyItIsntValid);
            }
            else
            {
                isValid = NameVerifier.IsEntityNameValid(value, elementToRename as EntitySave, out whyItIsntValid);

            }

            if (!isValid)
            {
                MessageBox.Show(whyItIsntValid);
            }
            else
            {

                string oldName = elementToRename.Name;
                string newName = oldName.Substring(0, oldName.Length - elementToRename.ClassName.Length) + value;

                DialogResult result = ChangeClassNamesInCodeAndFileName(elementToRename, value, oldName, newName);

                if(result == DialogResult.Yes)
                {
                    // Set the name first because that's going
                    // to be used by code that follows to modify
                    // inheritance.
                    elementToRename.Name = newName;


                    if (elementToRename is EntitySave)
                    {
                        // Change any Entities that depend on this
                        for (int i = 0; i < ProjectManager.GlueProjectSave.Entities.Count; i++)
                        {
                            EntitySave entitySave = ProjectManager.GlueProjectSave.Entities[i];
                            if (entitySave.BaseEntity == oldName)
                            {
                                entitySave.BaseEntity = newName;
                            }
                        }

                        // Change any NamedObjects that use this as their type (whether in Entity, or as a generic class)
                        List<NamedObjectSave> namedObjects = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(oldName);

                        foreach (NamedObjectSave nos in namedObjects)
                        {
                            if (nos.SourceType == SourceType.Entity && nos.SourceClassType == oldName)
                            {
                                nos.SourceClassType = newName;
                                nos.UpdateCustomProperties();
                            }
                            else if (nos.SourceType == SourceType.FlatRedBallType && nos.SourceClassGenericType == oldName)
                            {
                                nos.SourceClassGenericType = newName;
                            }
                        }
                    }
                    else
                    {
                        // Change any Screens that depend on this
                        for (int i = 0; i < ProjectManager.GlueProjectSave.Screens.Count; i++)
                        {
                            ScreenSave screenSave = ProjectManager.GlueProjectSave.Screens[i];
                            if (screenSave.BaseScreen == oldName)
                            {
                                screenSave.BaseScreen = newName;
                            }
                        }

                        if (ProjectManager.StartUpScreen == oldName)
                        {
                            ProjectManager.StartUpScreen = newName;

                        }


                        // Don't do anything with NamedObjects and Screens since they can't (currently) be named objects

                    }

                    GlueCommands.Self.ProjectCommands.SaveProjectsTask();
                    GlueCommands.Self.GluxCommands.SaveGluxTask();


                    TreeNode treeNode = GlueState.Self.Find.ElementTreeNode(elementToRename);
                    if (treeNode is ScreenTreeNode)
                    {
                        ((ScreenTreeNode)treeNode).RefreshTreeNodes();
                    }
                    else if (treeNode is EntityTreeNode)
                    {
                        ((EntityTreeNode)treeNode).RefreshTreeNodes();
                    }

                    if (elementToRename is EntitySave)
                    {
                        ProjectManager.SortAndUpdateUI(elementToRename as EntitySave);
                    }

                    else if (elementToRename is ScreenSave)
                    {
                        ProjectManager.SortAndUpdateUI(elementToRename as ScreenSave);
                    }

                    PluginManager.ReactToElementRenamed(elementToRename, oldName);
                }
            }
        }

        private static DialogResult ChangeClassNamesInCodeAndFileName(IElement elementToRename, string value, string oldName, string newName)
        {
            List<string> validFiles = CodeWriter.GetAllCodeFilesFor(elementToRename);

            string oldStrippedName = FileManager.RemovePath(oldName);
            string newStrippedName = FileManager.RemovePath(newName);


            bool wasAnythingFound = false;
            List<Tuple<string, string>> oldNewAbsoluteFiles = new List<Tuple<string, string>>();

            foreach (string file in validFiles)
            {
                string newFile = file.Replace(oldName.Replace("\\", "/"), newName.Replace("\\", "/"));

                // replace it if it's a factory:
                if(newFile.Contains("/Factories/"))
                {
                    newFile = newFile.Replace($"/Factories/{oldStrippedName}Factory.Generated.cs", $"/Factories/{newStrippedName}Factory.Generated.cs");
                }

                oldNewAbsoluteFiles.Add(new Tuple<string, string>(file, newFile));

                if (File.Exists(newFile))
                {
                    wasAnythingFound = true;
                }

            }
            DialogResult result = DialogResult.Yes;

            if (wasAnythingFound)
            {
                result = MessageBox.Show("This rename would result in existing files being overwritten. \n\nOverwrite?", "Overwrite",
                    MessageBoxButtons.YesNo);
            }

            if (result == DialogResult.Yes)
            {
                foreach(var pair in oldNewAbsoluteFiles)
                {
                    string absoluteOldFile = pair.Item1;
                    string absoluteNewFile = pair.Item2;

                    bool isCapitalizationOnlyChange = absoluteOldFile.Equals(absoluteNewFile, StringComparison.InvariantCultureIgnoreCase);

                    if (isCapitalizationOnlyChange == false && File.Exists(absoluteNewFile))
                    {
                        FileHelper.DeleteFile(absoluteNewFile);
                    }

                    // The old files may not exist
                    // for a variety of reasons (Glue
                    // error, user manually removed the file,
                    // etc).
                    if (File.Exists(absoluteOldFile))
                    {
                        File.Move(absoluteOldFile, absoluteNewFile);
                    }

                    if (File.Exists(absoluteNewFile))
                    {
                        // Change the class name in the non-generated .cs
                        string fileContents = FileManager.FromFileText(absoluteNewFile);
                        // We call RemovePath because the name is going to be "Namespace/ClassName" and we want
                        // to find just "ClassName".
                        fileContents = fileContents.Replace("partial class " + FileManager.RemovePath(oldName),
                            "partial class " + value);
                        FileManager.SaveText(fileContents, absoluteNewFile);

                        string relativeOld = FileManager.MakeRelative(absoluteOldFile);
                        string relativeNew = FileManager.MakeRelative(absoluteNewFile);

                        ProjectManager.ProjectBase.RenameItem(relativeOld, relativeNew);

                        foreach(var syncedProject in GlueState.Self.SyncedProjects)
                        {
                            string syncedRelativeOld = FileManager.MakeRelative(absoluteOldFile, syncedProject.Directory);
                            string syncedRelativeNew = FileManager.MakeRelative(absoluteNewFile, syncedProject.Directory);
                            syncedProject.RenameItem(syncedRelativeOld, syncedRelativeNew);
                        }
                    }
                }
            }
            return result;
        }



    }
}
