using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.FormHelpers;
using TMXGlueLib;
using FlatRedBall.IO;
using FlatRedBall.Glue.SaveClasses;
using System.Windows.Forms;
using TileGraphicsPlugin.Views;
using TileGraphicsPlugin.ViewModels;
using System.Reflection;
using TmxEditor;
using TmxEditor.Managers;
using FlatRedBall.Glue.ViewModels;

namespace TileGraphicsPlugin.Controllers
{
    public class AddLevelController : FlatRedBall.Glue.Managers.Singleton<AddLevelController>
    {

        public const string UsesTmxLevelFilesVariableName = "UsesTmxLevelFiles";


        public void ShowAddLevelUi()
        {
            AddNewLevelView view = new AddNewLevelView();
            var viewModel = new AddNewLevelViewModel();

            viewModel.MakeNameUnique(GlueState.Self.CurrentScreenSave.ReferencedFiles);
            
            view.DataContext = viewModel;





            AddTmxWithSharedTsxTo(viewModel);

            var dialogResult = view.ShowDialog();


            if(dialogResult.HasValue && dialogResult.Value)
            {
                string whyIsntValid = GetWhyCantAddLevel(viewModel.Name);
                if (string.IsNullOrEmpty(whyIsntValid))
                {
#if DEBUG
                    if(viewModel.CreateShareTilesetWith && viewModel.SelectedSharedFile == null)
                    {
                        throw new Exception("The viewModel indicates that the user selected a shared file, but it'the file is not specified");
                    }
#endif
                    AddLevel(viewModel);
                }
                else
                {
                    MessageBox.Show(whyIsntValid);
                }
            }
        }

        private static void AddTmxWithSharedTsxTo(AddNewLevelViewModel viewModel)
        {
            List<string> filesAlreadyVisited = new List<string>();

            var tmxFiles =
                GlueState.Self.CurrentGlueProject.GetAllReferencedFiles().Where(item => TileGraphicsPlugin.Managers.FileReferenceManager.Self.IsTmx(item));
            foreach (var file in tmxFiles)
            {
                string relativeToContent = file.Name;
                bool isBuiltTmx = !string.IsNullOrEmpty(file.SourceFile) && FileManager.GetExtension(file.SourceFile) == "tmx";
                if(isBuiltTmx)
                {
                    relativeToContent = file.SourceFile;
                }

                string absoluteFileName = GlueCommands.Self.GetAbsoluteFileName(relativeToContent, true);

                string absoluteStandardized = FileManager.Standardize(absoluteFileName).ToLowerInvariant();
                if (!filesAlreadyVisited.Contains(absoluteStandardized) && System.IO.File.Exists(absoluteFileName))
                {
                    filesAlreadyVisited.Add(absoluteStandardized);

                    TiledMapSave tsx = null;

                    try
                    {
                        tsx = TiledMapSave.FromFile(absoluteFileName);
                    }
                    catch (Exception exception)
                    {
                        FlatRedBall.Glue.Plugins.PluginManager.ReceiveError("Error loading TMX when trying to find TSX references: " + exception.Message);
                    }

                    if (tsx != null)
                    {
                        var hasShared = tsx.Tilesets.Any(item => !string.IsNullOrEmpty(item.Source));

                        if (hasShared)
                        {
                            
                            viewModel.AvailableSharedFiles.Add(relativeToContent);
                        }
                    }
                }
            }
        }

        public string GetWhyCantAddLevel(string levelName)
        {
            if(string.IsNullOrEmpty(levelName))
            {
                return "Empty names are not allowed";
            }
            if(GlueState.Self.CurrentScreenSave == null)
            {
                return "No screen is currently selected";
            }

            var screen = GlueState.Self.CurrentScreenSave;

            if(screen.ReferencedFiles.Any(item=> FileManager.RemovePath(item.Name) == levelName + ".tilb"))
            {
                return "There is already a level named " + levelName;
            }

            return null;
        }

        public void AddLevel(AddNewLevelViewModel levelInfo)
        {
            bool shouldContinue = true;

            shouldContinue = ShareOnSourceTsxIfNecessary(levelInfo);

            if (shouldContinue)
            {
                TryAddLevelsFolders();

                CreateFilesOnDisk(levelInfo);

                CreateReferencedFileSaves(levelInfo.Name);

                TryAddSolidCollisions();

                TryAddUsesTmxLevelFilesVariable();

                SaveEverything();

                GenerateCode();
            }
        }

        private bool ShareOnSourceTsxIfNecessary(AddNewLevelViewModel levelInfo)
        {

            bool shouldContinue = true;

            if (levelInfo.CreateShareTilesetWith)
            {
                TiledMapSave tiledMapSave = null;

                string whyIsntValid = null;
                string absoluteFile = GlueCommands.Self.GetAbsoluteFileName(levelInfo.SelectedSharedFile, true);

                if (string.IsNullOrEmpty(levelInfo.SelectedSharedFile))
                {
                    whyIsntValid = "No TMX file selected to pull tilesets from";
                }
                else if (System.IO.File.Exists(absoluteFile) == false)
                {
                    whyIsntValid = "Could not find the file" + levelInfo.SelectedSharedFile;
                }

                if(string.IsNullOrEmpty(whyIsntValid))
                {
                    try
                    {
                        tiledMapSave = TiledMapSave.FromFile(absoluteFile);
                    }
                    catch(Exception e)
                    {
                        whyIsntValid = "Error loading the TMX: " + e.Message;
                    }
                }

                if(!string.IsNullOrEmpty(whyIsntValid))
                {
                    MessageBox.Show(whyIsntValid);
                    shouldContinue = false;
                }
                else
                {
                    string destinationDirectory = GetLevelsFolderAbsoluteForScreen(GlueState.Self.CurrentScreenSave);


                    var nonSharedTilesets = tiledMapSave.Tilesets.Where(item => item.IsShared == false);


                    if(nonSharedTilesets.Count() > 0)
                    {
                        var shouldShareResult = MessageBox.Show(
                            "The file " + levelInfo.SelectedSharedFile + " includes layers which are not shared.  Would you like to share them?");

                        if(shouldShareResult != DialogResult.OK)
                        {
                            shouldContinue = false;
                        }
                    }

                    if (shouldContinue)
                    {
                        bool shouldSave = false;

                        foreach (var tileset in tiledMapSave.Tilesets.Where(item=>item.IsShared == false))
                        {
                            SharedTilesetManager.ConvertToSharedTileset(
                                tileset, tiledMapSave, destinationDirectory);
                            shouldSave = true;
                        }

                        if (shouldSave)
                        {
                            tiledMapSave.Save(tiledMapSave.FileName);
                        }
                    }
                }
            }

            return shouldContinue;
        }

        private void TryAddLevelsFolders()
        {
            var screen = GlueState.Self.CurrentScreenSave;

            if (screen == null)
            {
                throw new InvalidOperationException("Levels can only be added to Screens");
            }

            var treeNode = GlueState.Self.Find.ScreenTreeNode(screen);
            var filesTreeNode = treeNode.FilesTreeNode;

            // See if the Levels directory exists...
            var levelsFolder = filesTreeNode.Nodes.FirstOrDefault(item => item.Text == "Levels");

            // ... and if not, create it
            if (levelsFolder == null)
            {
                GlueCommands.Self.ProjectCommands.AddDirectory("Levels", filesTreeNode);
            }

            // See if the Tilesets directory exists...
            var tilesetsFolder = filesTreeNode.Nodes.FirstOrDefault(item => item.Text == "Tilesets");

            // ... and if not, create it
            if (tilesetsFolder == null)
            {
                GlueCommands.Self.ProjectCommands.AddDirectory("Tilesets", filesTreeNode);
            }
        }

        private void CreateFilesOnDisk(AddNewLevelViewModel levelInfo)
        {
            string levelName = levelInfo.Name;

            var screen = GlueState.Self.CurrentScreenSave;
            var treeNode = GlueState.Self.Find.ScreenTreeNode(screen);
            var filesTreeNode = treeNode.FilesTreeNode;
            var levelsFolder = filesTreeNode.Nodes.FirstOrDefault(item => item.Text == "Levels");
            var tilesetsFolder = filesTreeNode.Nodes.FirstOrDefault(item => item.Text == "Tilesets");
            var thisAssembly = this.GetType().Assembly;

            if (levelInfo.CreateSamplePlatformer)
            {
                CreateSamplePlatformerLevelAndAssociatedFilesOnDisk(levelName, levelsFolder, tilesetsFolder, thisAssembly);
            }
            else if(levelInfo.CreateEmptyLevel)
            {
                CreateEmptyLevelFilesOnDisk(levelName, levelsFolder, levelInfo.IndividualTileWidth, levelInfo.IndividualTileHeight, thisAssembly);
            }
            else if (levelInfo.CreateShareTilesetWith)
            {

                var savedFile = CreateEmptyLevelFilesOnDisk(levelName, levelsFolder, levelInfo.IndividualTileWidth, levelInfo.IndividualTileHeight, thisAssembly);
                string destinationFolder = FileManager.GetDirectory(savedFile);

                TiledMapSave newTms = TiledMapSave.FromFile(savedFile);

                if(string.IsNullOrEmpty(levelInfo.SelectedSharedFile))
                {
                    throw new NullReferenceException("levelInfo.SelectedSharedFile is null and it shouldn't be");
                }

                string toPullFromAbsoluteFileName =
                    GlueCommands.Self.GetAbsoluteFileName(levelInfo.SelectedSharedFile, true);
                string toPullFromFolder = FileManager.GetDirectory(toPullFromAbsoluteFileName);


                TiledMapSave toPullFrom = TiledMapSave.FromFile(toPullFromAbsoluteFileName);

                bool oldValue = Tileset.ShouldLoadValuesFromSource;
                Tileset.ShouldLoadValuesFromSource = false;

                foreach(var tileset in toPullFrom.Tilesets)
                {
                    var clonedTileset = FileManager.CloneObject(tileset);

                    // The tsx we're pulling from may not be in the same
                    // folder as the tsx we're creating, so we need to convert
                    // to absolute then back to relative:
                    string absoluteSource = toPullFromFolder + clonedTileset.Source;
                    string relativeToNewFolder = FileManager.MakeRelative(absoluteSource, destinationFolder);
                    clonedTileset.Source = relativeToNewFolder;
                    newTms.Tilesets.Add(clonedTileset);
                }
                Tileset.ShouldLoadValuesFromSource = oldValue;

                newTms.Save(newTms.FileName);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static void CreateSamplePlatformerLevelAndAssociatedFilesOnDisk(string levelName, TreeNode levelsFolder, TreeNode tilesetsFolder, Assembly thisAssembly)
        {
            // Save the TMX file to disk
            string absoluteLevelsDirectory = GlueCommands.Self.ProjectCommands.MakeAbsolute(
                levelsFolder.GetRelativePath(), true);
            string absoluteTilesetsDirectory = GlueCommands.Self.ProjectCommands.MakeAbsolute(
                tilesetsFolder.GetRelativePath(), true);


            string fullTmxFile = absoluteLevelsDirectory + levelName + ".tmx";
            FileManager.SaveEmbeddedResource(thisAssembly, "TileGraphicsPlugin.Content.Levels.PlatformerTileMap.tmx",
                fullTmxFile);

            // Create the TSX
            // TODO: Right now we don't use a shared tsx file, but we should

            // Add the PNG for the TSX
            string targetPng = absoluteTilesetsDirectory + "PlatformerTiles.png";
            if (!System.IO.File.Exists(targetPng))
            {
                FileManager.SaveEmbeddedResource(thisAssembly, "TileGraphicsPlugin.Content.Tilesets.PlatformerTiles.png",
                                targetPng);
            }
        }

        private static string CreateEmptyLevelFilesOnDisk(string levelName, TreeNode levelsFolder, int tileWidth, int tileHeight, System.Reflection.Assembly thisAssembly)
        {
            // Save the TMX file to disk
            string absoluteTargetDirectory = GlueCommands.Self.ProjectCommands.MakeAbsolute(
                levelsFolder.GetRelativePath(), true);
            string fullTmxFile = absoluteTargetDirectory + levelName + ".tmx";
            FileManager.SaveEmbeddedResource(thisAssembly, "TileGraphicsPlugin.Content.Levels.TiledMap.tmx",
                fullTmxFile);

            // but wait, we need to modify it
            TiledMapSave tms = TiledMapSave.FromFile(fullTmxFile);
            tms.tilewidth = tileWidth;
            tms.tileheight = tileHeight;

            tms.Save(fullTmxFile);

            return fullTmxFile;
        }

        private void CreateReferencedFileSaves(string levelName)
        {
            var screen = GlueState.Self.CurrentScreenSave;
            var treeNode = GlueState.Self.Find.ScreenTreeNode(screen);
            var filesTreeNode = treeNode.FilesTreeNode;
            var levelsFolder = filesTreeNode.Nodes.FirstOrDefault(item => item.Text == "Levels");
            var tilesetFolder = filesTreeNode.Nodes.FirstOrDefault(item => item.Text == "Tilesets");

            AddTilbReferencedFileSave(levelName, levelsFolder);

            AddCsvReferencedFileSave(levelName, levelsFolder: levelsFolder, tilesetFolder: tilesetFolder);
        }

        private static void AddTilbReferencedFileSave(string levelName, TreeNode levelsFolder)
        {
            string absoluteLevelsDirectory = GlueCommands.Self.ProjectCommands.MakeAbsolute(
                levelsFolder.GetRelativePath(), forceAsContent:true);

            string fullTmxFile = absoluteLevelsDirectory + levelName + ".tmx";

            GlueState.Self.CurrentTreeNode = levelsFolder;

            var builderToolAssociation = 
                FlatRedBall.Glue.Managers.BuildToolAssociationManager.Self.GetBuilderToolAssociationForExtensions("tmx", "tilb");
            // Add the RFS for this TMX
            string commandLineArguments = "copyimages=false";

            var currentElement = GlueState.Self.CurrentElement;
            var directory = levelsFolder.GetRelativePath();

            var newRfs = GlueCommands.Self.GluxCommands.AddSingleFileTo(fullTmxFile, levelName, 
                commandLineArguments,
                builderToolAssociation,
                true,
                null, currentElement, directory);

            if (newRfs == null)
            {
                MessageBox.Show("Error trying to add new file to Glue project");
            }
            else
            {
                newRfs.LoadedOnlyWhenReferenced = true;
            }
        }


        private void AddCsvReferencedFileSave(string levelName, TreeNode levelsFolder, TreeNode tilesetFolder)
        {
            string absoluteLevelsDirectory = GlueCommands.Self.ProjectCommands.MakeAbsolute(
                levelsFolder.GetRelativePath(), forceAsContent: true);
            string absoluteTilesetsFolder = GlueCommands.Self.ProjectCommands.MakeAbsolute(
                tilesetFolder.GetRelativePath(), forceAsContent:true);

            string fullCsvFile = absoluteTilesetsFolder + levelName + "Info.csv";
            string fullTmxFile = absoluteLevelsDirectory + levelName + ".tmx";

            GlueState.Self.CurrentTreeNode = tilesetFolder;
            var element = GlueState.Self.CurrentElement;

            var btaManager = FlatRedBall.Glue.Managers.BuildToolAssociationManager.Self;

            var builderToolAssociation = btaManager.GetBuilderToolAssociationForExtensions("tmx", "csv");

            var newRfs = GlueCommands.Self.GluxCommands.AddSingleFileTo(fullTmxFile, levelName + "Info", "",
                builderToolAssociation,
                true,
                null, element, tilesetFolder.GetRelativePath());

            newRfs.LoadedOnlyWhenReferenced = true;


            var glueProject = GlueState.Self.CurrentGlueProject;
            var customClass = glueProject.CustomClasses.FirstOrDefault(item => item.Name == "TileMapInfo");

            if(customClass != null)
            {
                customClass.CsvFilesUsingThis.Add(newRfs.Name);
            }
        }


        private void TryAddSolidCollisions()
        {
            // Only add it if it's not already there - if the user already has made another level then we don't need to do it again
            var currentScreen = GlueState.Self.CurrentScreenSave;

            var alreadyHasSolidCollisions = currentScreen.NamedObjects.Any(item => item.InstanceName == "SolidCollisions" && item.SourceClassType == "TileShapeCollection");

            if (!alreadyHasSolidCollisions)
            {
                AddObjectViewModel viewModel = new AddObjectViewModel
                {
                    SourceType = SourceType.FlatRedBallType,
                    SourceClassType = "TileShapeCollection",
                    ObjectName = "SolidCollisions"
                };



                // SourceType sourceType, 
                // string sourceClassType, 
                // string sourceFile, 
                // string objectName, string sourceNameInFile, string sourceClassGenericType
                // 
                var namedObjectSave = GlueCommands.Self.GluxCommands.AddNewNamedObjectToSelectedElement(
                    //FlatRedBall.Glue.SaveClasses.SourceType.FlatRedBallType,
                    //"TileShapeCollection",
                    //null,
                    //"SolidCollisions",
                    //null,
                    //null);
                    viewModel);
            }
        }

        private void TryAddUsesTmxLevelFilesVariable()
        {
            var screen = GlueState.Self.CurrentScreenSave;

            CustomVariable customVariable =
                screen.CustomVariables.FirstOrDefault(item => item.Name == UsesTmxLevelFilesVariableName);

            if (customVariable == null)
            {
                customVariable = screen.AddCustomVariable("bool", UsesTmxLevelFilesVariableName);
            }

            customVariable.DefaultValue = true;
        }


        private void SaveEverything()
        {
            GlueCommands.Self.ProjectCommands.SaveProjects();
            GlueCommands.Self.GluxCommands.SaveGlux();

            GlueCommands.Self.RefreshCommands.RefreshUiForSelectedElement();
        }


        private void GenerateCode()
        {
            GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
        }

        string GetLevelsFolderAbsoluteForScreen(ScreenSave screen)
        {

            if (screen == null)
            {
                return null;
            }
            else
            {
                string folder = screen.Name + "/Levels/";

                return GlueCommands.Self.GetAbsoluteFileName(folder, true);
            }
        }

    }
}
