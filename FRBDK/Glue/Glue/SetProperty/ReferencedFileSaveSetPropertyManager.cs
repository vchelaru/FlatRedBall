using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.ContentPipeline;
using System.Windows.Forms;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using FlatRedBall.Glue.Controls;
using Glue;
using System.IO;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.ProjectExclusionPlugin;

namespace FlatRedBall.Glue.SetVariable
{
    public class ReferencedFileSaveSetPropertyManager
    {
        internal void ReactToChangedReferencedFile(string changedMember, object oldValue, ref bool updateTreeView)
        {
            ReferencedFileSave rfs = GlueState.Self.CurrentReferencedFileSave;
            var element = GlueState.Self.CurrentElement;

            #region Opens With

            if (changedMember == "OpensWith")
            {
                if (rfs.OpensWith == "New Application...")
                {
                    string newApplication = EditorData.FileAssociationSettings.SetApplicationForExtension(null, "New Application...");

                    if (!string.IsNullOrEmpty(newApplication))
                    {
                        rfs.OpensWith = newApplication;
                    }
                    else
                    {
                        rfs.OpensWith = "<DEFAULT>";
                    }
                }
            }

            #endregion

            #region Name

            else if (changedMember == "Name")
            {
                if ((string)oldValue != rfs.Name && ProjectManager.GlueProjectSave != null)
                {
                    if ((string)oldValue != null)
                    {
                        ReactToRenamedReferencedFile((string)oldValue, rfs.Name, rfs, GlueState.Self.CurrentElement);
                    }
                }
            }

            #endregion

            #region LoadedAtRuntime

            if (changedMember == "LoadedAtRuntime")
            {

                updateTreeView = false;
            }

            #endregion

            #region Loaded only when referenced

            else if (changedMember == "LoadedOnlyWhenReferenced")
            {
                updateTreeView = false;
                if (rfs.LoadedOnlyWhenReferenced)
                {
                    // We need to make this public, or else it won't work on WP7 and Silverlight.
                    // Update - The preferred method to get access to this stuff by string is either
                    // GetMember or GetStaticMember, so there's no reason to force this stuff as public
                    // when LoadedWhenReferenced is set to true.
                    //rfs.HasPublicProperty = true;
                }
            }

            #endregion

            #region Has public property

            else if (changedMember == "HasPublicProperty")
            {
                updateTreeView = false;
                // GetMember and GetStaticMember
                // make it so we no longer require
                // the member to be public. 
                //if (rfs.LoadedOnlyWhenReferenced && !rfs.HasPublicProperty)
                //{
                //    System.Windows.Forms.MessageBox.Show("This file must have a public property if it " +
                //        "is \"Loaded Only When Referenced\" so that it can be accessed through reflection " +
                //        "on non-PC platforms.");

                //    rfs.HasPublicProperty = true;
                //}

                //if (rfs.ContainerType == ContainerType.None && rfs.HasPublicProperty == false)
                //{
                //    System.Windows.Forms.MessageBox.Show("Global content must be public so custom code can access it.");
                //    rfs.HasPublicProperty = true;
                //}
            }

            #endregion

            #region IsSharedStatic

            else if (changedMember == "IsSharedStatic")
            {
                updateTreeView = false;
                // If this is made IsSharedStatic, that means that the file will not be added to managers
                // We should see if any named objects reference this and notify the user
                List<NamedObjectSave> namedObjects = GlueState.Self.CurrentElement.NamedObjects;

                foreach (NamedObjectSave namedObject in namedObjects)
                {
                    if (namedObject.SourceType == SourceType.File && namedObject.SourceFile == rfs.Name && namedObject.AddToManagers == false)
                    {
                        DialogResult result = MessageBox.Show("The object " + namedObject.InstanceName + " references this file.  " +
                            "Shared files are not added to the engine, but since the object has its AddToManagers also set to false " +
                            "the content in this file will not be added to managers.  Would you like to set the object's AddToManagers to " +
                            "true?", "Add to managers?", MessageBoxButtons.YesNo);

                        if (result == DialogResult.Yes)
                        {
                            namedObject.AddToManagers = true;
                        }
                    }
                }
            }

            #endregion

            #region IsDatabaseForLocalizing

            else if (changedMember == nameof(ReferencedFileSave.IsDatabaseForLocalizing))
            {
                updateTreeView = false;
                bool oldValueAsBool = (bool)oldValue;
                bool newValue = rfs.IsDatabaseForLocalizing;

                if(newValue)
                {
                    TaskManager.Self.Add(() => RemoveCodeForCsv(rfs), "Removing old CSV");

                }
                else
                {
                    CsvCodeGenerator.GenerateAndSaveDataClass(rfs, rfs.CsvDelimiter);
                }

                // Let's revert the change just to see if 
                // things have changed project-wide, and if
                // have to tell the user to re-generate all code.
                rfs.IsDatabaseForLocalizing = oldValueAsBool;
                ObjectFinder.Self.GlueProject.UpdateIfTranslationIsUsed();
                bool oldProjectLocalization = ObjectFinder.Self.GlueProject.UsesTranslation;

                rfs.IsDatabaseForLocalizing = newValue;
                ObjectFinder.Self.GlueProject.UpdateIfTranslationIsUsed();
                bool newProjectLocalization = ObjectFinder.Self.GlueProject.UsesTranslation;



                if (oldProjectLocalization != newProjectLocalization)
                {
                    MessageBox.Show("Because of the change to the \"Is Database For Localizing\" the generated code for the entire project is likely out of date." +
                        "We recommend closing and re-opening the project in Glue to cause a full regeneration.", "Generated code is out of date");
                }
            }

            #endregion

            #region UseContentPipeline

            else if (changedMember == "UseContentPipeline")
            {
                ContentPipelineHelper.ReactToUseContentPipelineChange(rfs);

                // Make sure that
                // all other RFS's
                // that use this file
                // get changed too.
                List<ReferencedFileSave> matchingRfses = ObjectFinder.Self.GetMatchingReferencedFiles(rfs);

                foreach (ReferencedFileSave rfsToUpdate in matchingRfses)
                {
                    rfsToUpdate.UseContentPipeline = rfs.UseContentPipeline;
                    // No need to
                    // call this method
                    // because there's only
                    // one file in the project
                    // even though there's multiple
                    // ReferencedFileSaves, and this
                    // method just modifies the content
                    // project.
                    //ContentPipelineHelper.ReactToUseContentPipelineChange(rfsToUpdate);

                    var container = rfsToUpdate.GetContainer();

                    if (container != null)
                    {
                        //CodeWriter.GenerateCode(container);
                        GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(container);
                        //CodeWriter.GenerateCode(container);
                    }
                    else
                    {
                        GlueCommands.Self.GenerateCodeCommands.GenerateGlobalContentCode();
                    }
                }


                updateTreeView = false;
            }

            #endregion

            #region TextureFormat

            //else if (changedMember == "TextureFormat")
            //{
            //    ContentPipelineHelper.UpdateTextureFormatFor(rfs);

            //    // See the UseContentPipeline section for comments on what this
            //    // code does.
            //    List<ReferencedFileSave> matchingRfses = ObjectFinder.Self.GetMatchingReferencedFiles(rfs);
            //    foreach (ReferencedFileSave rfsToUpdate in matchingRfses)
            //    {
            //        rfsToUpdate.TextureFormat = rfs.TextureFormat;
            //        IElement container = rfsToUpdate.GetContainer();

            //        if (container != null)
            //        {
            //            CodeWriter.GenerateCode(container);
            //        }
            //        else
            //        {
            //            GlueCommands.Self.GenerateCodeCommands.GenerateGlobalContentCode();
            //        }
            //    }


            //    updateTreeView = false;
            //}

            #endregion

            #region IncludeDirectoryRelativeToContainer

            else if (changedMember == "IncludeDirectoryRelativeToContainer")
            {
                if (rfs.GetContainerType() == ContainerType.None)
                {
                    // This RFS
                    // is in GlobalContent
                    // so we need to find all
                    // RFS's that use this RFS
                    // and regenerate them
                    var elements = ObjectFinder.Self.GetAllElementsReferencingFile(rfs.Name);

                    foreach (var item in elements)
                    {
                        CodeWriter.GenerateCode(item);
                    }
                }
                else
                {

                }
            }

            #endregion

            #region AdditionalArguments

            else if (changedMember == "AdditionalArguments")
            {
                rfs.PerformExternalBuild(runAsync:true);

            }

            #endregion

            #region CreatesDictionary

            else if (changedMember == nameof(ReferencedFileSave.CreatesDictionary))
            {
                // This could change things like the constants added to the code file, so let's generate the code now.
                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentCsvCode();
            }

            #endregion

            #region ProjectsToExcludeFrom

            else if(changedMember == nameof(ReferencedFileSave.ProjectsToExcludeFrom))
            {
                // handled by ProjectExclusionPlugin
            }

            #endregion

            PluginManager.ReactToReferencedFileChangedValue(changedMember, oldValue);
        }

        public static void ReactToRenamedReferencedFile(string oldName, string newName, ReferencedFileSave rfs, IElement container)
        {
            string oldDirectory = FileManager.GetDirectory(oldName);
            string newDirectory = FileManager.GetDirectory(newName);

            // it's a RFS so it's gotta be content
            // Note - MakeAbsolute will do its best
            // to determine if a file is content. However,
            // a rename may change the extension to something 
            // unrecognizable. In this case we still want to have 
            // it be content
            bool forceAsContent = true;
            var oldFilePath = new FilePath(GlueCommands.Self.GetAbsoluteFileName(oldName, forceAsContent));
            var newFilePath = new FilePath(GlueCommands.Self.GetAbsoluteFileName(newName, forceAsContent));

            string instanceName = FileManager.RemovePath(FileManager.RemoveExtension(newName));
            string whyIsntValid;
            if (oldDirectory != newDirectory)
            {
                MessageBox.Show("The old file was located in \n" + oldDirectory + "\n" +
                    "The new file is located in \n" + newDirectory + "\n" +
                    "Currently Glue does not support changing directories.", "Warning");

                rfs.SetNameNoCall(oldName);
            }
            else if (NameVerifier.IsReferencedFileNameValid(instanceName, rfs.GetAssetTypeInfo(), rfs, container, out whyIsntValid) == false)
            {
                MessageBox.Show(whyIsntValid);
                rfs.SetNameNoCall(oldName);

            }
            else
            {
                bool shouldContinue = true;
                bool shouldMove = true;
                CheckForExistingFileOfSameName(oldName, rfs, newFilePath, ref shouldMove, ref shouldContinue);
                if(shouldContinue)
                {
                    ForceReactToRenamedReferencedFileAsync(oldName, newName, rfs, container, shouldMove:shouldMove);
                }
            }
        }

        public static async Task ForceReactToRenamedReferencedFileAsync(string oldName, string newName, ReferencedFileSave rfs, IElement container, bool shouldMove)
        {
            await TaskManager.Self.AddAsync(async () =>
            {
                bool forceAsContent = true;
                var oldFilePath = new FilePath(GlueCommands.Self.GetAbsoluteFileName(oldName, forceAsContent));
                var newFilePath = new FilePath(GlueCommands.Self.GetAbsoluteFileName(newName, forceAsContent));


                if (shouldMove && oldFilePath.Exists())
                {
                    File.Move(oldFilePath.FullPath, newFilePath.FullPath);
                }

                UpdateObjectsUsingFile(container as GlueElement, oldName, rfs);

                await RegenerateCodeAndUpdateUiAccordingToRfsRename(oldName, newName, rfs);

                UpdateBuildItemsForRenamedRfs(oldName, newName);

                AdjustDataFilesIfIsCsv(oldName, rfs);

                GluxCommands.Self.SaveGlux();

                GlueCommands.Self.ProjectCommands.SaveProjects();
            }, $"ForceReactToRenamedReferencedFileAsync {oldName} -> {newName}");
        }

        private static void UpdateObjectsUsingFile(GlueElement element, string oldName, ReferencedFileSave rfs)
        {
            string newName = rfs.Name;
            string oldNameUnqualified = FileManager.RemoveExtension(FileManager.RemovePath(oldName));
            string newNameUnqualified = FileManager.RemoveExtension(FileManager.RemovePath(newName));
            if (element != null)
            {
                AdjustAccordingToRenamedRfs(element, oldName, rfs, newName, oldNameUnqualified, newNameUnqualified);
            }
            foreach (var derived in ObjectFinder.Self.GetAllElementsThatInheritFrom(element))
            {
                AdjustAccordingToRenamedRfs(derived, oldName, rfs, newName, oldNameUnqualified, newNameUnqualified);
            }
        }

        private static void AdjustAccordingToRenamedRfs(GlueElement element, string oldName, ReferencedFileSave rfs, string newName, string oldNameUnqualified, string newNameUnqualified)
        {
            foreach (NamedObjectSave nos in element.GetAllNamedObjectsRecurisvely())
            {
                UpdateObjectToRenamedFile(oldName, newName, nos, rfs);
            }

            foreach (CustomVariable cv in element.CustomVariables.Where(item =>  item.DefaultValue is string && ((string)item.DefaultValue) == oldNameUnqualified))
            {
                cv.DefaultValue = newNameUnqualified;
            }
            foreach (var state in element.AllStates)
            {
                foreach (var variable in state.InstructionSaves.Where(item => item.Value is string && ((string)item.Value) == oldNameUnqualified))
                {
                    variable.Value = newNameUnqualified;
                }

            }
        }

        private static void UpdateObjectToRenamedFile(string oldName, string newName, NamedObjectSave nos, ReferencedFileSave rfs)
        {
            if (nos.SourceType == SourceType.Entity && nos.SourceFile == oldName)
            {
                nos.SourceFile = newName;
            }

            string oldNameUnqualified = FileManager.RemoveExtension( FileManager.RemovePath(oldName) );
            string newNameUnqualified = FileManager.RemoveExtension(FileManager.RemovePath(newName));
            // see if any variables use this
            foreach (var customVariable in nos.InstructionSaves)
            {
                // December 29, 2021
                // This code used to only
                // compare against unqualified
                // RuntimeTypes. I believe this is
                // because older Glue would always use
                // unqualified names, but newer Glue uses
                // qualified names in most places. We'll check
                // for both in case there are any older projects.
                string rfsType = rfs.RuntimeType;
                string rfsTypeUnqualified = rfsType;
                if (rfsType != null && rfsType.Contains('.'))
                {
                    rfsTypeUnqualified = FileManager.GetExtension(rfsType);
                }

                var matches =
                    customVariable.Value is string && (customVariable.Value as string) == oldNameUnqualified && 
                        (string.Equals(customVariable.Type, rfsType, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(customVariable.Type, rfsTypeUnqualified, StringComparison.OrdinalIgnoreCase));
                if (matches)
                {
                    customVariable.Value = newNameUnqualified;
                    Plugins.PluginManager.ReceiveOutput("Changed " + nos.InstanceName + "." + customVariable.Member + " from " + oldNameUnqualified + " to " + newNameUnqualified);
                }

            }
        }

        public static void AdjustDataFilesIfIsCsv(string oldName, ReferencedFileSave rfs)
        {
            // We'll remove the old file from the project, delete it, then have the RFS regenerate/add the new one to the project

            //////////////Early Out///////////////////

            if (!rfs.IsCsvOrTreatedAsCsv || UsesAlterntaiveClass(rfs))
            {
                return;
            }
            ////////////End Early Out/////////////////
            RemoveCodeForCsv(rfs, oldName);

            CsvCodeGenerator.GenerateAndSaveDataClass(rfs, rfs.CsvDelimiter);
        }

        private static void RemoveCodeForCsv(ReferencedFileSave rfs, string alternativeName = null, bool saveProject = true)
        {
            if(alternativeName == null)
            {
                alternativeName = rfs.Name;
            }
            string className = rfs.GetTypeForCsvFile(alternativeName);

            // the class name will be fully qualified, but we don't want that, we want just the end:
            if(className.Contains("."))
            {
                // provides the name after the dot:
                className = FileManager.GetExtension(className);
            }

            string whatToRemove = "DataTypes/" + className + ".Generated.cs";


            if (ProjectManager.ProjectBase.RemoveItem(whatToRemove) && saveProject)
            {
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }
            string fileToDelete = whatToRemove;
            fileToDelete = GlueCommands.Self.GetAbsoluteFileName(fileToDelete, false);
            if (System.IO.File.Exists(fileToDelete))
            {
                try
                {
                    FileHelper.MoveToRecycleBin(fileToDelete);
                }
                catch (Exception e)
                {
                    GlueGui.ShowMessageBox("Could not delete the file " + fileToDelete + "\n\nThe file is no longer referneced by the project so it is not necessary to delete this file manually.");
                }
            }
        }

        private static bool UsesAlterntaiveClass(ReferencedFileSave rfs)
        {
            CustomClassSave ccs = ObjectFinder.Self.GlueProject.GetCustomClassReferencingFile(rfs.Name);
            return ccs != null;
        }

        private static void UpdateBuildItemsForRenamedRfs(string oldName, string newName)
        {
            if (ProjectManager.ContentProject != null)
            {
                var oldNameWithContentPrefix = "content\\" + oldName.ToLower().Replace("/", "\\");
                var item = ProjectManager.ContentProject.GetItem(oldNameWithContentPrefix);

                // The item could be null if this file is excluded from the project
                if (item != null)
                {
                    if (newName.Replace("/", "\\").StartsWith(@"content\", StringComparison.OrdinalIgnoreCase))
                    {
                        newName = newName.Substring(@"content\".Length);
                    }

                    var newNameWithContentPrefix = "content\\" + newName.ToLowerInvariant().Replace("/", "\\");
                    item.UnevaluatedInclude = newNameWithContentPrefix;

                    string nameWithoutExtensions = FileManager.RemovePath(FileManager.RemoveExtension(newName));

                    item.SetMetadataValue("Name", nameWithoutExtensions);


                    ProjectManager.ContentProject.RenameInDictionary(oldNameWithContentPrefix, newNameWithContentPrefix, item);
                }
            }
        }

        private static async Task RegenerateCodeAndUpdateUiAccordingToRfsRename(string oldName, string newName, ReferencedFileSave fileSave)
        {
            foreach (var element in ProjectManager.GlueProjectSave.AllElements())
            {
                bool wasAnythingChanged = element.ReactToRenamedReferencedFile(oldName, newName);

                if (wasAnythingChanged)
                {
                    await CodeWriter.GenerateCode(element);
                }

                if (element.ReferencedFiles.Contains(fileSave))
                {
                    await CodeWriter.GenerateCode(element);

                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element);
                }
            }
        }

        private static void CheckForExistingFileOfSameName(string oldName, ReferencedFileSave fileSave, FilePath newFilePath, ref bool shouldMove, ref bool shouldContinue)
        {
            if (newFilePath.Exists())
            {
                string message = "The new file name already exists.  What would you like to do?";

                var mbmb = new MultiButtonMessageBoxWpf();

                mbmb.MessageText = message;

                mbmb.AddButton("Use existing file", DialogResult.Yes);
                mbmb.AddButton("Cancel the rename", DialogResult.Cancel);

                mbmb.ShowDialog();
                var result = (DialogResult)mbmb.ClickedResult;

                if (result == DialogResult.Cancel)
                {
                    fileSave.SetNameNoCall(oldName);
                    shouldContinue = false;
                }
                else if (result == DialogResult.Yes)
                {
                    shouldMove = false;
                }
            }
        }
    }
}
