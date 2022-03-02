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
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueFormsCore.Managers;
using FlatRedBall.Glue.Utilities;

namespace FlatRedBall.Glue.SaveClasses
{
    public static class IElementHelperGlue
    {
        /// <summary>
        /// Renames the argument elementToRename. The value (name) should not include
        /// the "Screens\" or "Entities\" prefix.
        /// </summary>
        /// <param name="elementToRename">The element to rename.</param>
        /// <param name="value">The desired name without the type prefix.</param>
        public static void RenameElement(this GlueElement elementToRename, string value)
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

                string oldNameFull = elementToRename.Name;
                string newNameFull = oldNameFull.Substring(0, oldNameFull.Length - elementToRename.ClassName.Length) + value;

                DialogResult result = ChangeClassNamesInCodeAndFileName(elementToRename, oldNameFull, newNameFull);

                if(result == DialogResult.Yes)
                {
                    // Set the name first because that's going
                    // to be used by code that follows to modify
                    // inheritance.
                    elementToRename.Name = newNameFull;

                    HashSet<GlueElement> elementsToRegenerate = new HashSet<GlueElement>();

                    if (elementToRename is EntitySave)
                    {
                        // Change any Entities that depend on this
                        for (int i = 0; i < ProjectManager.GlueProjectSave.Entities.Count; i++)
                        {
                            var entitySave = ProjectManager.GlueProjectSave.Entities[i];
                            if (entitySave.BaseElement == oldNameFull)
                            {
                                entitySave.BaseEntity = newNameFull;
                            }
                        }

                        // Change any NamedObjects that use this as their type (whether in Entity, or as a generic class)
                        List<NamedObjectSave> namedObjects = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(oldNameFull);

                        foreach (NamedObjectSave nos in namedObjects)
                        {
                            elementsToRegenerate.Add(ObjectFinder.Self.GetElementContaining(nos));
                            if (nos.SourceType == SourceType.Entity && nos.SourceClassType == oldNameFull)
                            {
                                nos.SourceClassType = newNameFull;
                                nos.UpdateCustomProperties();
                            }
                            else if (nos.SourceType == SourceType.FlatRedBallType && nos.SourceClassGenericType == oldNameFull)
                            {
                                nos.SourceClassGenericType = newNameFull;
                            }
                            else if(nos.IsCollisionRelationship())
                            {
                                PluginManager.CallPluginMethod(
                                    "Collision Plugin",
                                    "FixNamedObjectCollisionType",
                                    new object[] { nos });
                            }
                        }
                    }
                    else
                    {
                        // Change any Screens that depend on this
                        for (int i = 0; i < ProjectManager.GlueProjectSave.Screens.Count; i++)
                        {
                            var screenSave = ProjectManager.GlueProjectSave.Screens[i];
                            if (screenSave.BaseScreen == oldNameFull)
                            {
                                screenSave.BaseScreen = newNameFull;
                            }
                        }

                        if (ProjectManager.StartUpScreen == oldNameFull)
                        {
                            ProjectManager.StartUpScreen = newNameFull;

                        }


                        // Don't do anything with NamedObjects and Screens since they can't (currently) be named objects

                    }

                    foreach(var element in elementsToRegenerate)
                    {
                        GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
                    }

                    GlueCommands.Self.GenerateCodeCommands.GenerateGame1();

                    GlueCommands.Self.ProjectCommands.SaveProjects();
                    
                    GlueState.Self.CurrentGlueProject.Entities.SortByName();
                    GlueState.Self.CurrentGlueProject.Screens.SortByName();

                    GlueCommands.Self.GluxCommands.SaveGlux();


                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(elementToRename);

                    PluginManager.ReactToElementRenamed(elementToRename, oldNameFull);
                }
            }
        }

        private static DialogResult ChangeClassNamesInCodeAndFileName(IElement elementToRename, string oldName, string newName)
        {
            var validFiles = CodeWriter.GetAllCodeFilesFor(elementToRename);

            string oldStrippedName = FileManager.RemovePath(oldName);
            string newStrippedName = FileManager.RemovePath(newName);


            bool wasAnythingFound = false;
            List<Tuple<string, string>> oldNewAbsoluteFiles = new List<Tuple<string, string>>();

            foreach (var file in validFiles)
            {
                string newFile = file.FullPath.Replace(oldName.Replace("\\", "/"), newName.Replace("\\", "/"));

                // replace it if it's a factory:
                if(newFile.Contains("/Factories/"))
                {
                    newFile = newFile.Replace($"/Factories/{oldStrippedName}Factory.Generated.cs", $"/Factories/{newStrippedName}Factory.Generated.cs");
                }

                oldNewAbsoluteFiles.Add(new Tuple<string, string>(file.FullPath, newFile));

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
                        RefactorManager.Self.RenameClassInCode(
                            FileManager.RemovePath(oldName),
                            newStrippedName,
                            ref fileContents);

                        FileManager.SaveText(fileContents, absoluteNewFile);

                        string relativeOld = FileManager.MakeRelative(absoluteOldFile);
                        string relativeNew = FileManager.MakeRelative(absoluteNewFile);

                        ProjectManager.ProjectBase.RenameItem(relativeOld, relativeNew);

                        foreach(VisualStudioProject syncedProject in GlueState.Self.SyncedProjects)
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
