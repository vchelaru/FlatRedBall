using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using EditorObjects.Parsing;
using Ionic.Zip;
using System.Diagnostics;
using System.Windows.Forms;
using FlatRedBall.Utilities;
using FlatRedBall.Glue.Controls;
using System.IO;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.IO
{
    public static class ElementExporter
    {
        #region Fields

        static Dictionary<ReferencedFileSave, string> mOldFileNames = new Dictionary<ReferencedFileSave, string>();
        static Dictionary<NamedObjectSave, string> mOldNamedObjectFileNames = new Dictionary<NamedObjectSave, string>();

        static string mOldElementName;

        #endregion
        public static void ExportGroup(IEnumerable<GlueElement> elementGroup, GlueProjectSave glueProjectSave)
        {
            string directoryToExportTo = FileManager.UserApplicationDataForThisApplication + "ExportTemp\\";
            bool succeeded = true;
            if (Directory.Exists(directoryToExportTo))
            {
                try
                {
                    FileManager.DeleteDirectory(directoryToExportTo);
                }
                catch(Exception e)
                {
                    GlueGui.ShowMessageBox("Failed to delete the target export directory: \n" + e.ToString());
                    succeeded = false;
                }
            }

            Directory.CreateDirectory(directoryToExportTo);

            // If a single Entity is exported it must be self-contained
            // to be exported (it can't reference files outside of its relative
            // directory.  If a group of Entities are exported they may share files.
            // Therefore, we will populate the filesReferencedByElements List to suppress
            // warnings about files not referenced when they are part of the group.
            List<string> filesReferencedByElements = new List<string>();
            foreach (IElement element in elementGroup)
            {
                // We don't want to do this recursively - only the elements that
                // have been selected for export.
                foreach (ReferencedFileSave rfs in element.ReferencedFiles)
                {
                    string absoluteFile = ProjectManager.MakeAbsolute(rfs.Name, true);

                    absoluteFile = FileManager.Standardize(absoluteFile, null, false).ToLower();

                    filesReferencedByElements.Add(absoluteFile);
                }
            }

            List<string> filesToEmbed = new List<string>();

            foreach (var element in elementGroup)
            {
                if(!succeeded)
                {
                    break;
                }
                const bool openDirectory = false;

                string directoryForElement = directoryToExportTo + FileManager.GetDirectory(element.Name, RelativeType.Relative);
                Directory.CreateDirectory(directoryForElement);
                string outputFile = ExportElementToDirectory(element, glueProjectSave, directoryForElement, openDirectory, filesReferencedByElements, true);
                succeeded = !string.IsNullOrEmpty(outputFile);
                if(succeeded)
                {
                    filesToEmbed.Add(outputFile);
                }
            }

            if(succeeded)
            {
                // Create a zip that contains all the .entz and .scrz files
                SaveFileDialog fileDialog = new SaveFileDialog();
                fileDialog.Filter = "Glue Group (*.ggpz)|*.ggpz";
                DialogResult result = fileDialog.ShowDialog();

                if (result != DialogResult.Cancel)
                {
                    FilePath whereToSaveFile = fileDialog.FileName;
                    using (ZipFile zip = new ZipFile())
                    {
                        foreach (string file in filesToEmbed)
                        {
                            string directoryInZip = FileManager.GetDirectory( FileManager.MakeRelative(file, directoryToExportTo), RelativeType.Relative);
                            zip.AddFile(file, directoryInZip);
                        }

                        zip.Save(whereToSaveFile.FullPath);


                    }


                    string locationToShow = "\"" + whereToSaveFile.FullPath + "\"";
                    locationToShow = locationToShow.Replace("/", "\\");
                    Process.Start("explorer.exe", "/select," + locationToShow);
                }
            }
        }
        public static void ExportElement(GlueElement element, GlueProjectSave glueProjectSave)
        {
            bool shouldContinue = true;

            // let's see if we can export this element. If not we should tell the user what to do.
            bool hasExternals = GetExternalFiles(element, null).Count > 0;

            bool copyExternalFiles = false;

            if(hasExternals)
            {
                string screenOrEntity = "screen";
                if(element is EntitySave)
                {
                    screenOrEntity = "entity";
                }

                string message = string.Format(
                    "This {0} contains files outside of its content folder. Glue will export this {0}, but " +
                    "all files will be referenced in the same content folder. This may break some file " + 
                    "references.\n\nDo you want to export?", screenOrEntity);


                var result = MessageBox.Show(message, "Export?", MessageBoxButtons.YesNo);

                shouldContinue = result == DialogResult.Yes;

                copyExternalFiles = true;
            }

            if (shouldContinue)
            {

                System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
                folderBrowserDialog.Description = "Select folder to save exported file:";
                folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer;
                DialogResult result = folderBrowserDialog.ShowDialog();

                if (result != DialogResult.Cancel)
                {

                    string directory = folderBrowserDialog.SelectedPath + "\\";

                    ExportElementToDirectory(element, glueProjectSave, directory, true, null, automaticOverwrite: false, copyExternalFiles: copyExternalFiles);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <param name="directory"></param>
        /// <param name="openDirectory"></param>
        /// <param name="filesAlreadyAccountedFor"></param>
        /// <param name="automaticOverwrite"></param>
        /// <returns>The exported file name.</returns>
        private static string ExportElementToDirectory(GlueElement element, GlueProjectSave glueProjectSave, string directory, bool openDirectory, List<string> filesAlreadyAccountedFor, 
            bool automaticOverwrite = false, bool copyExternalFiles = false)
        {
            // The following steps are needed:
            // 1.  Make a entx or scrx
            // 2.  Create a file list including the entx/scrx, cs, and referenced file saves
            // 3.  Create a zipped file out of it
            // 4.  Delete the XML file
            // 5.  Show an explorer window with the exported file.

            string exportedFile = null;

            string extension;
            string zipExtension;
            GetExtensionsForExport(element, out extension, out zipExtension);

            string absoluteXml = directory + FileManager.RemovePath(element.Name) + "." + extension;
            string absoluteZip = directory + FileManager.RemovePath(element.Name) + "." + zipExtension;
            DialogResult dialogResult = DialogResult.Yes;

            if (automaticOverwrite == false && FileManager.FileExists(absoluteZip))
            {
                dialogResult = MessageBox.Show("The file already exist\n\n" + absoluteXml + "\n\nOverwrite?",
                    "Overwrite?", MessageBoxButtons.YesNo);

            }

            string reasonWhyElementCantBeExported = GetReasonWhyElementCantBeExported(element, filesAlreadyAccountedFor, copyExternalFiles: copyExternalFiles);


            if (!string.IsNullOrEmpty(reasonWhyElementCantBeExported))
            {
                MessageBox.Show("Can't export:\n\n" + reasonWhyElementCantBeExported);
            }
            else if (dialogResult == DialogResult.Yes)
            {
                PerformExport(element, glueProjectSave, openDirectory, absoluteXml, absoluteZip, copyExternalFiles);
                exportedFile = absoluteZip;
            }

            return exportedFile;
        }

        private static void PerformExport(GlueElement element, GlueProjectSave glueProjectSave, bool openDirectory, string absoluteXml, string absoluteZip, bool copyExternals)
        {
            AddCustomClasses(element, glueProjectSave);

            // This changes the name of the Element so that it doesn't
            // include any subdirectories.  The name is set back after 
            // the Element is serialized below in ReturnValuesBeforeModification.
            AdjustToPullFromDirectory(element);

            if (element is EntitySave asEntitySave)
            {
                FileManager.XmlSerialize(asEntitySave, absoluteXml);
            }
            else if (element is ScreenSave asScreenSave)
            {
                FileManager.XmlSerialize(asScreenSave, absoluteXml);
            }

            ReturnValuesBeforeModification(element);

            List<string> codeFiles = CodeWriter.GetAllCodeFilesFor(element);

            for (int i = codeFiles.Count - 1; i > -1; i--)
            {
                if (codeFiles[i].ToLowerInvariant().Contains(".generated."))
                {
                    codeFiles.RemoveAt(i);
                }
            }

            List<string> allFiles = new List<string>();

            string contentDirectory = ProjectManager.MakeAbsolute(element.Name, true);
            foreach (ReferencedFileSave rfs in element.ReferencedFiles)
            {
                string absoluteRfsName = ProjectManager.MakeAbsolute(rfs.Name);


                allFiles.Add(absoluteRfsName);
                var extraToAdd = FileReferenceManager.Self.GetFilesReferencedBy(absoluteRfsName, TopLevelOrRecursive.Recursive);
                allFiles.AddRange(extraToAdd.Select(item => item.Standardized));
            }

            // Don't preserve case here.  The code below
            // for RemoveDuplicates is case-sensitive, so we
            // want all entries to be lower-case.
            bool wasPreservingCase = FileManager.PreserveCase;
            FileManager.PreserveCase = false;
            for (int i = 0; i < allFiles.Count; i++)
            {
                allFiles[i] = FileManager.Standardize(allFiles[i]);
            }
            FileManager.PreserveCase = wasPreservingCase;

            // There may be duplicate entries here.
            // For example, an Entity may include a .scnx
            // which references a .achx, and it may also include
            // the .achx itself.  In that case, the .achx will appear
            // twice.
            StringFunctions.RemoveDuplicates(allFiles);

            using (ZipFile zip = new ZipFile())
            {
                foreach (var codeFile in codeFiles)
                {
                    zip.AddFile(codeFile, "");
                }
                zip.AddFile(absoluteXml, "");

                foreach (string fileToAdd in allFiles)
                {
                    string relativeDirectory = null;

                    relativeDirectory = FileManager.MakeRelative(FileManager.GetDirectory(fileToAdd), contentDirectory);

                    if (relativeDirectory.EndsWith("/"))
                    {
                        relativeDirectory = relativeDirectory.Substring(0, relativeDirectory.Length - 1);
                    }

                    bool isExternal = relativeDirectory.StartsWith("../");

                    if (isExternal)
                    {
                        string externalDirectory = "__external/";

                        string directoryToAddTo = externalDirectory +
                            FileManager.MakeRelative(fileToAdd, GlueState.Self.CurrentMainContentProject.Directory);

                        directoryToAddTo = FileManager.GetDirectory(directoryToAddTo, RelativeType.Relative);

                        zip.AddFile(fileToAdd, directoryToAddTo);

                    }
                    else
                    {
                        zip.AddFile(fileToAdd, relativeDirectory);

                    }
                }

                zip.Save(absoluteZip);
            }

            System.IO.File.Delete(absoluteXml);

            if (openDirectory)
            {
                string locationToShow = "\"" + absoluteZip + "\"";
                locationToShow = locationToShow.Replace("/", "\\");
                Process.Start("explorer.exe", "/select," + locationToShow);
            }

        }

        private static void AddCustomClasses(GlueElement element, GlueProjectSave glueProjectSave)
        {
            element.CustomClassesForExport.Clear();
            // See if there are any custom classes that are used by this:
            foreach (var customClass in glueProjectSave.CustomClasses)
            {
                var isCustomClassUsed = false;
                foreach (var csv in customClass.CsvFilesUsingThis)
                {
                    var foundCsvRfs = element.GetReferencedFileSave(csv);
                    if (foundCsvRfs != null)
                    {
                        isCustomClassUsed = true;
                        break;
                    }
                }

                if (isCustomClassUsed)
                {
                    element.CustomClassesForExport.Add(customClass);
                }
            }
        }

        private static void GetExtensionsForExport(IElement element, out string extension, out string zipExtension)
        {
            extension = "";
            zipExtension = "";


            if (element is EntitySave)
            {
                extension = "entx";
                zipExtension = "entz";
            }
            else if (element is ScreenSave)
            {
                extension = "scrx";
                zipExtension = "scrz";
            }
        }



        private static void AdjustToPullFromDirectory(IElement element)
        {
            mOldFileNames.Clear();
            mOldNamedObjectFileNames.Clear();

            mOldElementName = element.Name;

            string oldContentRelativeDirectory = "Content\\" + element.Name + "\\";
            string oldContentRelativeDirectoryAbsolute = ProjectManager.MakeAbsolute(element.Name, true);
            
            if (element is EntitySave)
            {
                element.Name = "Entities\\" + FileManager.RemovePath(element.Name);

            }
            else if (element is ScreenSave)
            {

                element.Name = "Screens\\" + FileManager.RemovePath(element.Name);
            }

            string newContentRelativeDirectory = element.Name + "\\";

            foreach (ReferencedFileSave rfs in element.ReferencedFiles)
            {
                string absoluteRfsPath = ProjectManager.MakeAbsolute(rfs.Name, true);

                if (FileManager.IsRelativeTo(absoluteRfsPath, oldContentRelativeDirectoryAbsolute))
                {
                    mOldFileNames.Add(rfs, rfs.Name);
                    rfs.SetNameNoCall(
                        (newContentRelativeDirectory + FileManager.RemovePath(rfs.Name)).Replace("\\", "/"));
                }
            }

            List<NamedObjectSave> nosList = element.NamedObjects;

            AdjustNosSourceFiles(oldContentRelativeDirectoryAbsolute, newContentRelativeDirectory, nosList);
        }

        private static void AdjustNosSourceFiles(string oldContentRelativeDirectoryAbsolute, string newContentRelativeDirectory, List<NamedObjectSave> nosList)
        {
            foreach (NamedObjectSave nos in nosList)
            {
                if (!string.IsNullOrEmpty(nos.SourceFile) && FileManager.IsRelativeTo(ProjectManager.MakeAbsolute(nos.SourceFile, true), oldContentRelativeDirectoryAbsolute))
                {
                    mOldNamedObjectFileNames.Add(nos, nos.SourceFile);
                    nos.SourceFile = (newContentRelativeDirectory + FileManager.RemovePath(nos.SourceFile)).Replace("\\", "/");
                }

                AdjustNosSourceFiles(oldContentRelativeDirectoryAbsolute, newContentRelativeDirectory, nos.ContainedObjects);
            }
        }

        private static void ReturnValuesBeforeModification(IElement element)
        {
            element.Name = mOldElementName;

            foreach (KeyValuePair<ReferencedFileSave, string> kvp in mOldFileNames)
            {
                kvp.Key.SetNameNoCall(kvp.Value);
            }


            foreach (KeyValuePair<NamedObjectSave, string> kvp in mOldNamedObjectFileNames)
            {
                kvp.Key.SourceFile = kvp.Value;
            }
        }

        //public static void ShowExportMultipleElementsListBox()
        //{
        //    ListBoxWindow listBoxWindow = new ListBoxWindow();
        //    listBoxWindow.AddButton("Cancel", DialogResult.Cancel);

        //    if (ProjectManager.GlueProjectSave != null)
        //    {
        //        foreach (EntitySave entitySave in ProjectManager.GlueProjectSave.Entities)
        //        {
        //            listBoxWindow.AddItem(entitySave);
        //        }

        //        foreach (ScreenSave screenSave in ProjectManager.GlueProjectSave.Screens)
        //        {
        //            listBoxWindow.AddItem(screenSave);
        //        }
        //        listBoxWindow.ShowCheckBoxes = true;
        //        listBoxWindow.Message = "Select which Screens and Entities you would like to export:";
        //        DialogResult result = listBoxWindow.ShowDialog();

        //        if (result == DialogResult.OK)
        //        {
        //            bool areAnySelected = false;
        //            foreach (TreeNode treeNode in listBoxWindow.CheckedTreeNodes)
        //            {
        //                areAnySelected = true;
        //                break;
        //            }
        //            if (areAnySelected)
        //            {


        //                System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
        //                folderBrowserDialog.Description = "Select folder to export selected Screens/Entities:";
        //                folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer;
        //                DialogResult folderSelectionResults = folderBrowserDialog.ShowDialog();

        //                if (folderSelectionResults != DialogResult.Cancel)
        //                {

        //                    string directory = folderBrowserDialog.SelectedPath + "\\";




        //                    int numberOfElementsExported = 0;

        //                    // If a single Entity is exported it must be self-contained
        //                    // to be exported (it can't reference files outside of its relative
        //                    // directory.  If a group of Entities are exported they may share files.
        //                    // Therefore, we will populate the filesReferencedByElements List to suppress
        //                    // warnings about files not referenced when they are part of the group.
        //                    List<string> filesReferencedByElements = new List<string>();
        //                    foreach (TreeNode treeNode in listBoxWindow.CheckedTreeNodes)
        //                    {
        //                        IElement element = treeNode.Tag as IElement;

        //                        // We don't want to do this recursively - only the elements that
        //                        // have been selected for export.
        //                        foreach (ReferencedFileSave rfs in element.ReferencedFiles)
        //                        {
        //                            string absoluteFile = ProjectManager.MakeAbsolute(rfs.Name, true);

        //                            absoluteFile = FileManager.Standardize(absoluteFile, null, false);

        //                            filesReferencedByElements.Add(absoluteFile);
        //                        }
        //                    }

        //                    foreach (TreeNode treeNode in listBoxWindow.CheckedTreeNodes)
        //                    {
        //                        if (ExportElementToDirectory((IElement)treeNode.Tag, directory, false, filesReferencedByElements))
        //                        {
        //                            numberOfElementsExported++;
        //                        }
        //                    }

        //                    Process.Start(directory);

        //                    MessageBox.Show(string.Format("Exported {0} Entity(s)/Screen(s)", numberOfElementsExported));
        //                }
        //            }
        //            else
        //            {
        //                MessageBox.Show("No Screens/Entities selected");
        //            }
        //        }
        //    }
        //}

        static string GetReasonWhyElementCantBeExported(IElement element, List<string> filesAlreadyIncluded, bool copyExternalFiles = false)
        {
            string toReturn = null;

            if (!copyExternalFiles)
            {
                List<string> filesNotIncluded = GetExternalFiles(element, filesAlreadyIncluded);

                if (filesNotIncluded.Count > 0)
                {
                    string contentDirectory = ProjectManager.MakeAbsolute(element.Name, true);
                    toReturn = "The file\n\n" + filesNotIncluded[0] + "\n\nis not relative to the content folder for the element which is\n\n" + contentDirectory;
                }
            }

            return toReturn;
        }

        private static List<string> GetExternalFiles(IElement element, List<string> filesAlreadyIncluded)
        {
            string contentDirectory = ProjectManager.MakeAbsolute(element.Name, true);

            List<string> filesNotIncluded = new List<string>();

            foreach (ReferencedFileSave rfs in element.ReferencedFiles)
            {
                string absolutePath = ProjectManager.MakeAbsolute(rfs.Name, true);

                absolutePath = FileManager.Standardize(absolutePath, null, false);

                bool referencedOutside = false;

                if (!FileManager.IsRelativeTo(absolutePath, contentDirectory))
                {
                    if (filesAlreadyIncluded != null && filesAlreadyIncluded.Contains(absolutePath.ToLower()))
                    {
                        referencedOutside = false;
                    }
                    else
                    {
                        filesNotIncluded.Add(absolutePath);
                    }
                }

                if (!referencedOutside)
                {
                    var files = FileReferenceManager.Self.GetFilesReferencedBy(absolutePath, TopLevelOrRecursive.Recursive);

                    foreach (var fileName in files)
                    {
                        if (!FileManager.IsRelativeTo(fileName.FullPath, contentDirectory))
                        {
                            if (filesAlreadyIncluded != null && filesAlreadyIncluded.Contains(absolutePath.ToLower()))
                            {
                                referencedOutside = false;
                            }
                            else
                            {
                                filesNotIncluded.Add(absolutePath);
                            }
                        }
                    }
                }
            }

            return filesNotIncluded;
        }
    }
}
