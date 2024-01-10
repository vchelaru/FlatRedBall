using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.SetVariable;
using L = Localization;

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


            AddTemporaryAtis(newFileWindow);
        }

        AssetTypeInfo localizationDatabaseAti;
        AssetTypeInfo LocalizationDatabaseAti
        {
            get
            {
                if(localizationDatabaseAti == null)
                {

                    var csvAti = AvailableAssetTypes.Self.GetAssetTypeFromExtension("csv");
                    localizationDatabaseAti = FileManager.CloneObject(csvAti);
                    localizationDatabaseAti.FriendlyName = "Localization Database (.csv)";

                }
                return localizationDatabaseAti;
            }
        }

        /// <summary>
        /// Adds ATIs to the NewFileWindow which should only exist during the creation of the new file and should not be available to the rest of Glue.
        /// </summary>
        /// <param name="newFileWindow">The new file window instance</param>
        private void AddTemporaryAtis(CustomizableNewFileWindow newFileWindow)
        {
            newFileWindow.AddOption(LocalizationDatabaseAti);
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

            var customFiles = System.IO.Directory.GetFiles(CustomFileTemplateFolder);
            listOfFiles.AddRange(customFiles);

            return listOfFiles;
        }

        private string GetNewFileTemplateForExtension(IEnumerable<string> listOfFiles, string extension, bool haveUserPick = true)
        {
            var enumeration = listOfFiles.Where(file=>FileManager.GetExtension(file) == extension).ToList();
            // If nothing was found, return null.
            if (!enumeration.Any()) return null;

            // if there is only 1 option, or the user isn't allowed to pick, pick the only first item.
            if (enumeration.Count == 1 || !haveUserPick) return enumeration.First();
            
            // Now that we have determined there are multiple options and the user is allowed to pick,
            // open a window with a combobox where the user can pick options.
            var cbmb = new ComboBoxMessageBox(L.Texts.WhichTemplateUse);
            foreach (var option in enumeration)
            {
                cbmb.Add(option, FileManager.RemovePath(option));
            }

            var result = cbmb.ShowDialog();

            if (result.HasValue && result.Value)
            {
                return cbmb.SelectedItem as string;
            }

            return null;

        }

        private void ReactToNewFile(SaveClasses.ReferencedFileSave newFile, AssetTypeInfo ati)
        {
            if(ati == LocalizationDatabaseAti)
            {
                newFile.IsDatabaseForLocalizing = true;

                EditorObjects.IoC.Container.Get<SetPropertyManager>().ReactToPropertyChanged(
                    nameof(newFile.IsDatabaseForLocalizing), false, nameof(newFile.IsDatabaseForLocalizing), null);
            }
        }



        private bool CreateNewFile(AssetTypeInfo assetTypeInfo, object extraData, string directory, string name, out string resultingName)
        {
            resultingName = null;
            if (assetTypeInfo != null)
            {
                string createdFile = GetCreatedFileTarget(assetTypeInfo, directory, name);
                SaveNewFileAtLocation(assetTypeInfo, createdFile);
                resultingName = createdFile;
                return true;
            }
            return false;

        }

        private void SaveNewFileAtLocation(AssetTypeInfo assetTypeInfo, string createdFile)
        {
            string availableFile;

            if(assetTypeInfo == LocalizationDatabaseAti)
            {
                string contents =
@"""StringId (string, required)"",English (string),Spanish (string),German (string)
T_Hello,Hello,Hola,Hallo
T_StartGame,Start Game,Comenzar el Juego,Spiel Starten
T_Exit,Exit,Salida,Ausfahrt
T_HighScore,High Score,Puntuación Alta,Hohe Punktzahl
T_Paused,Paused,En Pausa,Pausiert
";

                FileManager.SaveText(contents, createdFile);
            }
            else if (assetTypeInfo.Extension == "csv")
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
                    FileManager.XmlSerialize(type, saveInstance, createdFile);
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
