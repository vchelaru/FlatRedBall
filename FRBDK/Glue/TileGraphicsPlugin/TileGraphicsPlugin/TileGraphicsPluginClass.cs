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

        TiledObjectTypeCreator tiledObjectTypeCreator;
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
            // 1.0.11.4
            // - Shapes now have their Z value set according to the object layer index, which may be used to position entities properly.
            // 1.0.11.5
            // - Fixed a bug where the Name property for tiled sprites was being ignored.
            // 1.1.0 
            // - Introduces a MUCH faster loading system - approximately 4x as fast on large maps (680x1200)
            // 1.1.1.0
            // - Fixed offset issues with instantiating entities from objects
            // - Added support for rotated entities 
            // 1.1.1.1
            // - Fixed animations not setting the right and bottom texture coordinates according to offset values
            // 1.1.1.2
            // - Fixed attempting to set the EntityToCreate property on new entities in TileEntityInstantiator
            // - Name property is set even if property is lower-case "name"
            // 1.1.1.3
            // - Added method to add collision in a TileShapeCollection from a single layer in the MapDrawableBatch
            // 1.1.1.4
            // - Fixed crash thich can occur sometimes when clicking on a TMX file due to a collection changed 
            // 1.1.1.5
            // - Fixed possible threading issue if the project attempts to save on project startup
            // 1.1.1.6
            // - Updated tile entity instantiator so that it supports the latest changes to the factory pattern to support X and Y values
            // 1.1.1.7
            // - Added support for overriding the current TextureFilter when rendering a MapDrawableBatch
            // - Entities are now offset according to their layer's position - useful for instantiating entities inside of another entity
            // 1.2.0
            // - Added/improved xml Type generated file creation/updating
            // - Made the IDs alittle clearer - no more +1 in code 
            // 1.2.1
            // - Added TileShapeCollection.AddCollisionFromTilesWithProperty
            // 1.2.2
            // - Added TileShapeCollection.SetColor
            // 1.2.2.1
            // - Fixed possible crash with an empty object layer
            // 1.2.2.2
            // - Added TileShapeCollection CollideAgainst Polygon
            // 1.2.3
            // - Default properties from the TileSet now get assigned on created entities for object layers
            // 1.2.3.1
            // - Fixed polygon conversion not doing conversions using culture invariant values - fixes games running in Finnish
            // - Fixed tile entity instantiation not doing float conversions using culture invariant values - also Finnish fix.
            // 1.3.0
            // - Added TileShapeCollection + CollisionManager integration.
            // 1.3.1
            // - Removed requirement for XNA HiDef so old machines and VMs will run this plugin better, but the edit window won't show
            // 1.3.2
            //  - Added support for adding rectangles on rotated tiles
            // 1.3.3
            //  - Updated the TileShapeCollection CollisionRelationship classes to return bool on DoCollision to match the base implementation
            // 1.4.0
            //  - Collision from shapes on tileset tiles will now check the name of the shape - if it has no name then it's added to a TileShapeCollection
            //    with the name matching the layer. If the shape does have a name then it is added to a TileShapeCollection with the same name as the shape.
            //    This allows games to easily organize their collision with no code and no custom properties.
            // 1.4.1
            //  - ShapeCollections in LayeredTileMap are now cleared out, so if they're made visible in custom code
            //    they don't have to be made invisible manually.
            // 1.5.0
            //  - TileEntityInstantiator can now assign properties from CSVs if the CSV dictionary is registered
            get { return new Version(1, 5, 0, 0); }
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
            tiledObjectTypeCreator = new TiledObjectTypeCreator();

            this.TryHandleCopyFile += HandleCopyFile;

            this.ReactToLoadedGluxEarly += HandleGluxLoadEarly;

            this.ReactToLoadedGlux += () =>
            {
                HandleGluxLoad();
                tiledObjectTypeCreator.RefreshFile();
            };



            this.AdjustDisplayedReferencedFile += HandleAdjustDisplayedReferencedFile;

            this.ReactToItemSelectHandler += HandleItemSelect;
            this.ReactToFileChangeHandler += HandleFileChange;
            this.ReactToTreeViewRightClickHandler += RightClickManager.Self.HandleTreeViewRightClick;

            this.FillWithReferencedFiles += FileReferenceManager.Self.HandleGetFilesReferencedBy;
            this.CanFileReferenceContent += HandleCanFileReferenceContent;

            TilesetController.Self.EntityAssociationsChanged +=
                EntityListManager.Self.OnEntityAssociationsChanged;

            TilesetController.Self.GetTsxDirectoryRelativeToTmx = () => "../Tilesets/";

            this.ReactToChangedPropertyHandler += (changedMember, oldalue) =>
            {
                if(GlueState.CurrentCustomVariable != null)
                {
                    if (changedMember == nameof(CustomVariable.Name))
                    {
                        tiledObjectTypeCreator.RefreshFile();
                    }

                }
                else if(GlueState.CurrentEntitySave != null)
                {
                    if (changedMember == nameof(EntitySave.CreatedByOtherEntities))
                    {
                        tiledObjectTypeCreator.RefreshFile();
                    }
                }
            };

            this.ReactToElementVariableChange += (element, variable) =>
            {
                if ((element as EntitySave)?.CreatedByOtherEntities == true)
                {
                    tiledObjectTypeCreator.RefreshFile();
                }
            };

            this.ReactToVariableAdded += (newVariable) =>
            {
                var element = EditorObjects.IoC.Container.Get<IGlueState>().CurrentElement;
                if ((element as EntitySave)?.CreatedByOtherEntities == true)
                {
                    tiledObjectTypeCreator.RefreshFile();
                }
            };

            this.ReactToVariableRemoved += (removedVariable) =>
            {
                var element = EditorObjects.IoC.Container.Get<IGlueState>().CurrentElement;
                if ((element as EntitySave)?.CreatedByOtherEntities == true)
                {
                    tiledObjectTypeCreator.RefreshFile();
                }
            };
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
            if(mControl != null)
            {
                if(this.PluginTab == null)
                {
                    this.AddToTab(PluginManager.CenterTab, mControl, "TMX");
                }
                else if(this.PluginTab.Parent == null)
                {
                    base.AddTab();
                }

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
            mControl?.LoadFile(fullFileName);
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
                    mControl?.LoadFile(fileName);
                }
                else
                {
                    changesToIgnore--;
                }
            }
        }

        private void InitializeTab()
        {
            try
            {
                mControl = new TmxEditor.TmxEditorControl();
            }
            catch(System.NotSupportedException exc)
            {
                GlueCommands.PrintError("Could not find a graphics device that supports XNA hi-def. Attempting to load Tiled plugin without the control tab");
            }

            if(mControl != null)
            {
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
            if(mControl != null)
            {
                mControl.Entities = (GlueState.CurrentGlueProject.Entities.Select(e => e.Name).ToList());
            }
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
            if(mControl != null)
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
            FlatRedBall.Glue.Managers.TaskManager.Self.AddSync( CodeItemAdderManager.Self.UpdateCodeInProjectPresence,
                "Adding Tiled .cs files to the project");

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
            mControl?.UpdateTilesetDisplay();
        }

        #endregion

    }
}
