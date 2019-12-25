using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Parsing;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.NewFiles
{
    [Export(typeof(PluginBase))]
    public class NewFilePlugin : EmbeddedPlugin
    {
        #region Fields/Properties

        public static string BuiltInFileTemplateFolder
        {
            get
            {
                string assemblyLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";
                string newFileDirectory = assemblyLocation + "Content/FilesForAddNewFile/";

                return newFileDirectory;
            }
        }

        public static string CustomFileTemplateFolder
        {
            get
            {
                return FileManager.UserApplicationDataForThisApplication + "FilesForAddNewFile/";

            }
        }

        #endregion


        public override void StartUp()
        {
            System.IO.Directory.CreateDirectory(CustomFileTemplateFolder);

            this.AddNewFileOptionsHandler += AddNewFileOptions;
            this.CreateNewFileHandler += CreateNewFile;
            this.ReactToNewFileHandler += ReactToNewFile;
        }

        void AddNewFileOptions(CustomizableNewFileWindow newFileWindow)
        {
            var listOfFiles = GetAvailableFilesForNewFile();

            List<string> filesNotInAtiList = listOfFiles.Select(item=> FileManager.GetExtension(item)).Distinct().ToList();


            foreach (var ati in AvailableAssetTypes.Self.AllAssetTypes.Where(item => (item.Extension != null && item.HideFromNewFileWindow == false)))
            {
                while (filesNotInAtiList.Contains(ati.Extension))
                {
                    filesNotInAtiList.Remove(ati.Extension);
                }
            }

            // If there's any new options here, let's create new ATIs for it
            foreach (var extension in filesNotInAtiList)
            {
                CreateNoCodeGenerationAtiFor(extension);
            }


            foreach(var ati in AvailableAssetTypes.Self.AllAssetTypes.Where(item=>(item.Extension != null && item.HideFromNewFileWindow == false)))
            {
                bool added = false;
                if (!string.IsNullOrEmpty(ati.Extension) && !string.IsNullOrEmpty(ati.QualifiedSaveTypeName))
                {
                    newFileWindow.AddOption(ati);
                    added = true;
                }

                // special case .txt
                if (!added && ati.Extension == "txt")
                {
                    newFileWindow.AddOption(ati);
                    added = true;
                }

                if (!added && GetNewFileTemplateForExtension(listOfFiles, ati.Extension, false) != null)
                {
                    newFileWindow.AddOption(ati);

                }
            }

            
        }

        private void CreateNoCodeGenerationAtiFor(string extension)
        {
            AssetTypeInfo ati = new AssetTypeInfo();
            ati.Extension = extension;
            ati.FriendlyName = extension + " (" + extension + ")";

            AvailableAssetTypes.Self.AddAssetType(ati);
        }

        private static List<string> GetAvailableFilesForNewFile()
        {

            List<string> listOfFiles = new List<string>();
            listOfFiles.AddRange(System.IO.Directory.GetFiles(BuiltInFileTemplateFolder));

            listOfFiles.AddRange(System.IO.Directory.GetFiles(CustomFileTemplateFolder));

            return listOfFiles;
        }

        private string GetNewFileTemplateForExtension(IEnumerable<string> listOfFiles, string extension, bool haveUserPick = true)
        {
            var enumeration = listOfFiles.Where(file=>FileManager.GetExtension(file) == extension);
            if(enumeration.Count() != 0)
            {
                if (enumeration.Count() > 1 && haveUserPick)
                {
                    ComboBoxMessageBox cbmb = new ComboBoxMessageBox();
                    cbmb.Message = "Which template would you like to use?";
                    foreach (var option in enumeration)
                    {
                        cbmb.Add(option, FileManager.RemovePath(option));
                    }

                    var result = cbmb.ShowDialog();

                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        return cbmb.SelectedItem as string;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return enumeration.FirstOrDefault();
                }
            }
            return null;
        }

        private void ReactToNewFile(SaveClasses.ReferencedFileSave newFile)
        {

        }



        private bool CreateNewFile(AssetTypeInfo assetTypeInfo, object extraData, string directory, string name, out string resultingName)
        {
            resultingName = null;
            if (assetTypeInfo != null)
            {
                string createdFile = GetCreatedFileTarget(assetTypeInfo, directory, name);


                SaveNewFileAtLocation(assetTypeInfo, createdFile);

                bool make2D = (bool)extraData;

                AssetTypeInfoExtensionMethodsGlue.CreateCompanionSettingsFile(createdFile, make2D);
                resultingName = createdFile;

                return true;
            }
            return false;

        }

        private void SaveNewFileAtLocation(AssetTypeInfo assetTypeInfo, string createdFile)
        {
            string availableFile;

            if (assetTypeInfo.Extension == "csv")
            {
                string contents =

"\"Name (string, required)\",Health (float)" + Environment.NewLine + "Monster,100";

                FileManager.SaveText(contents, createdFile);
            }
            else if (assetTypeInfo.Extension == "txt")
            {
                const string contents = "Glue is such a great tool!";

                FileManager.SaveText(contents, createdFile);
            }
            // If the save type isn't null, then it's a type that is understood by the Glue, so we can
            // instantiate and save off the instance
            // Why don't we try a template first?
            else if (TryGetTemplateFileForAti(assetTypeInfo, out availableFile))
            {
                CreateNewFileFromAvailableFileTemplate(createdFile, availableFile);
            }
            else if (assetTypeInfo.SaveType != null)
            {
                object saveInstance = Activator.CreateInstance(assetTypeInfo.SaveType);
                FileManager.XmlSerialize(assetTypeInfo.SaveType, saveInstance, createdFile);
            }
            else if(assetTypeInfo.QualifiedSaveTypeName != null)
            {
                var type = TypeManager.GetTypeFromString(assetTypeInfo.QualifiedSaveTypeName);

                if(type != null)
                {
                    object saveInstance = Activator.CreateInstance(type);
                    FileManager.XmlSerialize(assetTypeInfo.SaveType, saveInstance, createdFile);
                }
            }
            // Unknown type, so save an empty file.
            else
            {
                FileManager.SaveText(null, createdFile);
            }
        }

        private bool TryGetTemplateFileForAti(AssetTypeInfo assetTypeInfo, out string availableFile)
        {
            availableFile = GetNewFileTemplateForExtension(GetAvailableFilesForNewFile(), assetTypeInfo.Extension);

            return availableFile != null;
        }

        private static void CreateNewFileFromAvailableFileTemplate(string newFile, string templateFile)
        {
            try
            {
                // We're going to use the FileHelper to copy the file over in case there are dependencies.
                FileHelper.RecursivelyCopyContentTo(templateFile, FileManager.GetDirectory(templateFile),
                    FileManager.GetDirectory(newFile),
                    FileManager.RemovePath(newFile));

                //System.IO.Directory.CreateDirectory(FileManager.GetDirectory(newFile));
                //System.IO.File.Copy(templateFile, newFile, true);

            }
            catch
            {
                // do nothing?
                int m = 3;
            }
        }

        private static string GetCreatedFileTarget(AssetTypeInfo assetTypeInfo, string directory, string name)
        {
            string createdFile = "";

            if (!directory.EndsWith("\\") && !directory.EndsWith("/"))
            {
                directory = directory + "/";
            }

            if (FileManager.IsRelative(directory))
            {
                createdFile = FileManager.RelativeDirectory + directory + name + "." + assetTypeInfo.Extension;

                if (ObjectFinder.Self.GlueProject != null)
                {
                    // I think we should always use the content directory whether we have a content project or not:
                    createdFile = GlueState.Self.ContentDirectory + directory + name + "." + assetTypeInfo.Extension;
                }

            }
            else
            {
                createdFile = directory + name + "." + assetTypeInfo.Extension;
            }
            return createdFile;
        }
    }
}
