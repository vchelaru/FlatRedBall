using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins;
using System.Reflection;
using FlatRedBall.Glue.Elements;
using System.IO;
using FlatRedBall.IO;
using EditorObjects.SaveClasses;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using System.Windows.Forms;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.VSHelpers;
using TileGraphicsPlugin.Controllers;
using TileGraphicsPlugin.Managers;
using FlatRedBall.Content.Instructions;
using TmxEditor;
using TmxEditor.Controllers;
using TMXGlueLib.DataTypes;
using ProjectManager = FlatRedBall.Glue.ProjectManager;
using TMXGlueLib;
using FlatRedBall.Glue.Parsing;
using TileGraphicsPlugin.CodeGeneration;
using TileGraphicsPlugin.Views;
using TileGraphicsPlugin.ViewModels;
using TmxEditor.Events;


namespace TileGraphicsPlugin
{
    [Export(typeof(PluginBase))]
    public class TileGraphicsPluginClass : PluginBase
    {
        #region Fields

        TilesetXnaRightClickController mTilesetXnaRightClickController;

        string mLastFile;

        TmxEditor.TmxEditorControl mControl;

        
        CommandLineViewModel mCommandLineViewModel;
        #endregion

        #region Properties

        public override Version Version
        {
            // 1.0.8 introduced tile instantiation per layer
            // 1.0.8.1 adds a null check around returning files referenced by a .tmx
            // 1.0.9 adds TileNodeNetworkCreator
            // 1.0.10 adds shape property support
            // 1.0.11 Adds automatic instantiation and collision setting from shapes
            // 1.0.11.1 Fixed bug where shape had a name
            // 1.0.11.2 Fixed rectangle polygon creation offset bug
            // 1.0.11.3 
            // - Fixed crash occurring when the Object layer .object property is null
            // - Upped to .NET 4.5.2
            get { return new Version(1, 0, 11, 3); }
        }


        [Import("GlueProjectSave")]
        public GlueProjectSave GlueProjectSave
        {
            get;
            set;
        }

        [Import("GlueCommands")]
        public IGlueCommands GlueCommands
        {
            get;
            set;
        }
		
		[Import("GlueState")]
		public IGlueState GlueState
		{
		    get;
		    set;
        }

        public override string FriendlyName
        {
            get { return "Tiled Plugin"; }
        }

        static TileGraphicsPluginClass mSelf;
        public static TileGraphicsPluginClass Self
        {
            get { return mSelf; }
        }

       
        #endregion
        
        #region Methods

        public TileGraphicsPluginClass()
        {
            mSelf = this;
        }

        public override void StartUp()
        {
            CodeItemAdderManager.Self.AddFilesToCodeBuildItemAdder();

            InitializeTab();


            AddEvents();

            SaveTemplateTmx();

            AddCodeGenerators();
        }

        private void AddCodeGenerators()
        {
            CodeWriter.CodeGenerators.Add(new LevelCodeGenerator());

        }

        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason reason)
        {
            // Do anything your plugin needs to do to shut down
            // or don't shut down and return false
            return true;
        }

        private static void SaveTemplateTmx()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string whatToSave = "TileGraphicsPlugin.Content.Levels.TiledMap.tmx";
            //C:\Users\Victor\AppData\Roaming\Glue\FilesForAddNewFile\

            string destination = FileManager.UserApplicationData +
                @"Glue\FilesForAddNewFile\TiledMap.tmx";
            try
            {
                FileManager.SaveEmbeddedResource(assembly, whatToSave, destination);
            }
            catch (Exception e)
            {
                PluginManager.ReceiveError("Error trying to save tmx: " + e.ToString());
            }
        }

        private void AddEvents()
        {

            this.TryHandleCopyFile += HandleCopyFile;

            this.ReactToLoadedGluxEarly += HandleGluxLoadEarly;

            this.ReactToLoadedGlux += HandleGluxLoad;
            
            this.AdjustDisplayedReferencedFile += HandleAdjustDisplayedReferencedFile;

            this.ReactToItemSelectHandler += HandleItemSelect;
            this.ReactToFileChangeHandler += HandleFileChange;
            this.ReactToTreeViewRightClickHandler += RightClickManager.Self.HandleTreeViewRightClick;

            this.GetFilesReferencedBy += FileReferenceManager.Self.HandleGetFilesReferencedBy;
            this.CanFileReferenceContent += HandleCanFileReferenceContent;

            TilesetController.Self.EntityAssociationsChanged +=
                EntityListManager.Self.OnEntityAssociationsChanged;

            TilesetController.Self.GetTsxDirectoryRelativeToTmx = () => "../Tilesets/";
        }

        private void HandleGluxLoadEarly()
        {

            // Add the necessary files for performing the builds to the Libraries/tmx folder
            BuildToolSaver.Self.SaveBuildToolsToDisk();

            // Add Builders so that the user has the option to handle these file types
            BuildToolAssociationManager.Self.UpdateBuildToolAssociations();
        }

        public static void ExecuteFinalGlueCommands(EntitySave entity)
        {
            FlatRedBall.Glue.Plugins.ExportedImplementations.GlueCommands.Self.RefreshCommands.RefreshUiForSelectedElement();
            FlatRedBall.Glue.Plugins.ExportedImplementations.GlueCommands.Self.GluxCommands.SaveGlux();
            FlatRedBall.Glue.Plugins.ExportedImplementations.GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(entity);
            FlatRedBall.Glue.Plugins.ExportedImplementations.GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
        }

        private bool HandleCanFileReferenceContent(string fileName)
        {
            var extension = FileManager.GetExtension(fileName);
            return extension == "tilb" || extension == "tsx" || extension == "tmx";

        }

        private bool HandleCopyFile(string sourceFile, string sourceDirectory, string targetFile)
        {
            string extension = FileManager.GetExtension(sourceFile);

            if (extension == "tmx")
            {
                if (System.IO.File.Exists(sourceFile))
                {
                    CopyFileManager.Self.CopyTmx(sourceFile, targetFile);
                }
                return true;

            }
            return false;
        }
        
        private void HandleItemSelect(TreeNode treeNode)
        {
            bool shouldRemove = true;

            ReferencedFileSave rfs = treeNode?.Tag as ReferencedFileSave;

            if (FileReferenceManager.Self.IsTmx(rfs))
            {
                shouldRemove = false;

                FileReferenceManager.Self.UpdateAvailableTsxFiles();

                ReactToRfsSelected(rfs);
            }


            if (shouldRemove)
            {
                RemoveTab();
            }
        }

        private void ReactToRfsSelected(ReferencedFileSave rfs)
        {
            if(this.PluginTab == null)
            {
                this.AddToTab(PluginManager.CenterTab, mControl, "TMX");
            }
            else if(this.PluginTab.Parent == null)
            {
                base.AddTab();
            }

            // These aren't built anymore, so no command line
            //mCommandLineViewModel.ReferencedFileSave = rfs;

            string fileNameToUse = rfs.Name;
            if(FileManager.GetExtension(rfs.SourceFile) == "tmx")
            {
                fileNameToUse = rfs.SourceFile;
            }

            string fullFileName = FlatRedBall.Glue.ProjectManager.MakeAbsolute(fileNameToUse, true);
            mLastFile = fullFileName;
            mControl.LoadFile(fullFileName);
        }

        private void HandleFileChange(string fileName)
        {
            string extension = FileManager.GetExtension(fileName);

            if(extension == "tsx")
            {
                // oh boy, the user changed a shared tile set.  Time to rebuild everything that uses this tileset
                var allReferencedFileSaves = FileReferenceManager.Self.GetReferencedFileSavesReferencingTsx(fileName);

                var toListForDebug = allReferencedFileSaves.ToList();

                // build em!
                foreach(var file in allReferencedFileSaves)
                {
                    file.PerformExternalBuild(runAsync:true);
                }
            }

            // If a png changes, it may be resized. Tiled changes IDs of tiles when a PNG resizes if
            // no external tileset is used, so we want to rebuild the .tmx's.
            if (extension == "png")
            {
                var allReferencedFileSaves = FileReferenceManager.Self.GetReferencedFileSavesReferencingPng(fileName);

                var toListForDebug = allReferencedFileSaves.ToList();

                // build em!
                foreach (var file in allReferencedFileSaves)
                {
                    file.PerformExternalBuild(runAsync: true);
                }
            }

            if (this.PluginTab != null && this.PluginTab.Parent != null && fileName == mLastFile)
            {
                if (changesToIgnore == 0)
                {
                    mControl.LoadFile(fileName);
                }
                else
                {
                    changesToIgnore--;
                }
            }
        }

        private void InitializeTab()
        {
            mControl = new TmxEditor.TmxEditorControl();
            mControl.AnyTileMapChange += HandleUserChangeTmx;
            mControl.LoadEntities += OnLoadEntities;
            var commandLineArgumentsView = new TileGraphicsPlugin.Views.CommandLineArgumentsView();
            mCommandLineViewModel = new CommandLineViewModel();

            mCommandLineViewModel.CommandLineChanged += HandleCommandLinePropertyChanged;

            commandLineArgumentsView.DataContext = mCommandLineViewModel;
            mControl.AddTab("Command Line", commandLineArgumentsView);


            mTilesetXnaRightClickController = new TilesetXnaRightClickController();
            mTilesetXnaRightClickController.Initialize(mControl.TilesetXnaContextMenu);
            mControl.TilesetDisplayRightClick += (o, s) => mTilesetXnaRightClickController.RefreshMenuItems();
        }

        private void HandleCommandLinePropertyChanged()
        {
            var file = mCommandLineViewModel.ReferencedFileSave;
            file.AdditionalArguments =
                mCommandLineViewModel.CommandLineString;

            FlatRedBall.Glue.Managers.TaskManager.Self.AddSync(() =>
            {
                file.PerformExternalBuild();
            },
            "Building file " + file);

            GlueCommands.GluxCommands.SaveGlux();
        }


        private void OnLoadEntities(object sender, EventArgs args)
        {
            mControl.Entities = (GlueState.CurrentGlueProject.Entities.Select(e => e.Name).ToList());
        }

        private void OnClosedByUser(object sender)
        {
            PluginManager.ShutDownPlugin(this);
        }

        int changesToIgnore = 0;
        private void HandleUserChangeTmx(object sender, EventArgs args)
        {
            var asTileMapChangeEventArgs = args as TileMapChangeEventArgs;

            ChangeType changeType = ChangeType.Other;

            if(asTileMapChangeEventArgs != null)
            {
                changeType = asTileMapChangeEventArgs.ChangeType;
            }

            SaveTiledMapSave(changeType);
        }

        public void SaveTiledMapSave(ChangeType changeType)
        {
            string fileName = mLastFile;

            FlatRedBall.Glue.Managers.TaskManager.Self.AddSync(() =>
                {

                    changesToIgnore++;

                    bool saveTsxFiles = changeType == ChangeType.Tileset;

                    mControl.SaveCurrentTileMap(saveTsxFiles);
                },
                "Saving tile map");
        }

        void HandleAdjustDisplayedReferencedFile(ReferencedFileSave rfs, ReferencedFileSavePropertyGridDisplayer displayer)
        {
            if (rfs.IsCsvOrTreatedAsCsv && !string.IsNullOrEmpty(rfs.SourceFile))
            {

            }
        }

        void HandleGluxLoad()
        {
            // Add the .cs files which include the map drawable batch classes
            CodeItemAdderManager.Self.UpdateCodeInProjectPresence();

            // Add the CSV entry so that Glue knows how to load a .scnx into the classes added above
            AssetTypeInfoAdder.Self.UpdateAtiCsvPresence();



            // Make sure the TileMapInfo CustomClassSave is there, and make sure it has all the right properties
            TileMapInfoManager.Self.AddAndModifyTileMapInfoClass();

            // Ensure the TmxEditorControl has all the data it needs
            InitializeTmxEditorControl();


        }

        private void InitializeTmxEditorControl()
        {
            OnLoadEntities(null, null);
        }


        internal void UpdateTilesetDisplay()
        {
            mControl.UpdateTilesetDisplay();
        }

        #endregion

    }
}
