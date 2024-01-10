using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using EditorObjects.Parsing;
using Ionic.Zip;
using System.Diagnostics;
using System.Windows.Forms;
using FlatRedBall.Utilities;
using System.IO;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using L = Localization;

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
                    GlueGui.ShowMessageBox(String.Format(L.Texts.ErrorDeleteDirectoryFailed, e));
                    succeeded = false;
                }
            }

            Directory.CreateDirectory(directoryToExportTo);

            // If a single Entity is exported it must be self-contained
            // to be exported (it can't reference files outside of its relative
            // directory.  If a group of Entities are exported they may share files.
            // Therefore, we will populate the filesReferencedByElements List to suppress
            // warnings about files not referenced when they are part of the group.
            var filesReferencedByElements = (
                from IElement element in elementGroup 
                from rfs in element.ReferencedFiles 
                select GlueCommands.Self.GetAbsoluteFileName(rfs) 
                into absoluteFile 
                select FileManager.Standardize(absoluteFile, null, false)).ToList();

            var filesToEmbed = new List<string>();

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
                fileDialog.Filter = $"{L.Texts.GlueGroup} (*.ggpz)|*.ggpz";
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

                string message = string.Format(L.Texts.QuestionFilesOutsideContent, screenOrEntity);


                var result = MessageBox.Show(message, L.Texts.QuestionExport, MessageBoxButtons.YesNo);

                shouldContinue = result == DialogResult.Yes;

                copyExternalFiles = true;
            }

            if (shouldContinue)
            {
                var folderBrowserDialog = new FolderBrowserDialog();
                folderBrowserDialog.Description = L.Texts.FolderSelectExportedFile;
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
        /// <param name="copyExternalFiles"></param>
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

            GetExtensionsForExport(element, out var extension, out var zipExtension);

            string absoluteXml = directory + FileManager.RemovePath(element.Name) + "." + extension;
            string absoluteZip = directory + FileManager.RemovePath(element.Name) + "." + zipExtension;
            DialogResult dialogResult = DialogResult.Yes;

            if (automaticOverwrite == false && FileManager.FileExists(absoluteZip))
            {
                dialogResult = MessageBox.Show(
                    String.Format(L.Texts.FileXExistsOverwrite, absoluteXml),
                    L.Texts.QuestionOverwrite, MessageBoxButtons.YesNo);

            }

            string reasonWhyElementCantBeExported = GetReasonWhyElementCantBeExported(element, filesAlreadyAccountedFor, copyExternalFiles);


            if (!string.IsNullOrEmpty(reasonWhyElementCantBeExported))
            {
                MessageBox.Show($"{L.Texts.ExportCant}\n\n{reasonWhyElementCantBeExported}");
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

            var codeFiles = CodeWriter.GetAllCodeFilesFor(element);

            for (int i = codeFiles.Count - 1; i > -1; i--)
            {
                if (codeFiles[i].Standardized.Contains(".generated."))
                {
                    codeFiles.RemoveAt(i);
                }
            }

            var allFiles = new List<string>();

            var contentDirectory = GlueCommands.Self.GetAbsoluteFileName(element.Name, true);
            foreach (var rfs in element.ReferencedFiles)
            {
                string absoluteRfsName = GlueCommands.Self.GetAbsoluteFileName(rfs);


                allFiles.Add(absoluteRfsName);
                var extraToAdd = FileReferenceManager.Self.GetFilesReferencedBy(absoluteRfsName, TopLevelOrRecursive.Recursive);
                allFiles.AddRange(extraToAdd.Select(item => item.Standardized));
            }

            // Don't preserve case here.  The code below
            // for RemoveDuplicates is case-sensitive, so we
            // want all entries to be lower-case.
            bool wasPreservingCase = FileManager.PreserveCase;
            FileManager.PreserveCase = false;
            for (var i = 0; i < allFiles.Count; i++)
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

            using var zip = new ZipFile();
            
            foreach (var codeFile in codeFiles)
            {
                zip.AddFile(codeFile.FullPath, "");
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

                var isExternal = relativeDirectory.StartsWith("../");

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
                var isCustomClassUsed = customClass.CsvFilesUsingThis.Select(element.GetReferencedFileSave).Any(foundCsvRfs => foundCsvRfs != null);

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

            switch (element)
            {
                case EntitySave:
                    extension = "entx";
                    zipExtension = "entz";
                    break;
                case ScreenSave:
                    extension = "scrx";
                    zipExtension = "scrz";
                    break;
            }
        }



        private static void AdjustToPullFromDirectory(IElement element)
        {
            mOldFileNames.Clear();
            mOldNamedObjectFileNames.Clear();

            mOldElementName = element.Name;

            string oldContentRelativeDirectoryAbsolute = GlueCommands.Self.GetAbsoluteFileName(element.Name, true);

            element.Name = element switch
            {
                EntitySave => "Entities\\" + FileManager.RemovePath(element.Name),
                ScreenSave => "Screens\\" + FileManager.RemovePath(element.Name),
                _ => element.Name
            };

            var newContentRelativeDirectory = element.Name + "\\";

            foreach (ReferencedFileSave rfs in element.ReferencedFiles)
            {
                string absoluteRfsPath = GlueCommands.Self.GetAbsoluteFileName(rfs.Name, true);

                if (FileManager.IsRelativeTo(absoluteRfsPath, oldContentRelativeDirectoryAbsolute))
                {
                    mOldFileNames.Add(rfs, rfs.Name);
                    rfs.SetNameNoCall(
                        (newContentRelativeDirectory + FileManager.RemovePath(rfs.Name)).Replace("\\", "/"));
                }
            }

            AdjustNosSourceFiles(oldContentRelativeDirectoryAbsolute, newContentRelativeDirectory, element.NamedObjects);
        }

        private static void AdjustNosSourceFiles(string oldContentRelativeDirectoryAbsolute, string newContentRelativeDirectory, List<NamedObjectSave> nosList)
        {
            foreach (var nos in nosList)
            {
                if (!string.IsNullOrEmpty(nos.SourceFile) && FileManager.IsRelativeTo(GlueCommands.Self.GetAbsoluteFileName(nos.SourceFile, true), oldContentRelativeDirectoryAbsolute))
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
        static string GetReasonWhyElementCantBeExported(IElement element, List<string> filesAlreadyIncluded, bool copyExternalFiles = false)
        {
            string toReturn = null;

            if (!copyExternalFiles)
            {
                var filesNotIncluded = GetExternalFiles(element, filesAlreadyIncluded);

                if (filesNotIncluded.Count > 0)
                {
                    var contentDirectory = GlueCommands.Self.GetAbsoluteFileName(element.Name, true);
                    toReturn = string.Format(L.Texts.FileXNotRelativeToContentFolder, filesNotIncluded[0], contentDirectory);
                }
            }

            return toReturn;
        }

        private static List<string> GetExternalFiles(IElement element, List<string> filesAlreadyIncluded)
        {
            string contentDirectory = GlueCommands.Self.GetAbsoluteFileName(element.Name, true);
            var filesNotIncluded = new List<string>();

            foreach (ReferencedFileSave rfs in element.ReferencedFiles)
            {
                string absolutePath = GlueCommands.Self.GetAbsoluteFileName(rfs.Name, true);

                absolutePath = FileManager.Standardize(absolutePath, null, false);

                bool referencedOutside = false;

                if (!FileManager.IsRelativeTo(absolutePath, contentDirectory))
                {
                    if (filesAlreadyIncluded != null && filesAlreadyIncluded.Contains(absolutePath, StringComparer.OrdinalIgnoreCase))
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
                        if (!FileManager.IsRelativeTo(fileName.FullPath, contentDirectory) && (filesAlreadyIncluded == null 
                                || !filesAlreadyIncluded.Contains(absolutePath, StringComparer.OrdinalIgnoreCase)))
                        {
                            filesNotIncluded.Add(absolutePath);
                        }
                    }
                }
            }

            return filesNotIncluded;
        }
    }
}
