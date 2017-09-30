using FlatRedBall.Glue;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.IO;
using Gum.DataTypes;
using GumPlugin.CodeGeneration;
using GumPlugin.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;
using GumPlugin.Controls;
using GumPlugin.ViewModels;
using System.ComponentModel;
using EditorObjects.Parsing;
using Gum.Managers;
using System.Drawing;
using Gum.DataTypes.Behaviors;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Managers;

namespace GumPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        #region Fields

        GumControl control;
        GumViewModel viewModel;
        GumxPropertiesManager propertiesManager;

        ToolStripMenuItem addGumProjectMenuItem;

        #endregion

        #region Properties

        public override string FriendlyName
        {
            get { return "Gum Plugin"; }
        }

        public override Version Version
        {
            // 0.8.0.2:
            // - Added support for computer settings with ',' decimal separator
            // - Fixed layout not considering Text font scale
            // 0.8.0.3
            // - First pass at behaviors
            // 0.8.0.4
            // - Big improvements on auto-sizing components which stack their children.
            // - Added state code generation for Width Units and Height Units
            // 0.8.0.5
            // - All runtime objects - even ones created manually and added to a parent, now
            //   have their CustomInit called
            // - Gum runtimes can now turn on events on all children, whether on the parent or
            //   floating
            // 0.8.0.6 - Exposing variables with spaces no longer generates code with spaces.
            // 0.8.0.7 - Added GumAnimation.IsPlaying() method.
            // 0.8.2.2 - Added support for automatic one-level Click event exposing
            // 0.8.2.3 - Fixed Rotation on Sprites not rotating about their origin
            // 0.8.3.0 
            // - Calling Stop on a looping animation now properly stops it
            // - Line rectangles now render properly in components 
            // 0.8.3.1
            // - If an Instance in a Component/Screen starts with a number, codegen will prefix _ so it compiles in code.
            // - Fixed a bug where the plugin might crash if it handles a file change that isn't part of the .gumx. This could happen
            //   when pulling from source control, or if a user copies a file just to test it locally
            // - Fixed bug where plugin could load wrong Gum file if more than one .gumx file is in the same folder (like a backup file)
            // 0.8.3.2
            // - Fixed absolute file path issue with custom fonts
            // - Core files are always copied instead of only if newer - so starter projects will always get the latest
            // - Fixed fonts with outlines not being referenced in content proj.
            // - Fixed interpolation of states not working properly because the wrong Name was assigned to the new state (spaces were removed)
            // - Added support for "Instant" interpolation type
            // 0.8.3.3 - Fixed threading issue when fixing projects.
            // 0.8.3.4 - Fixed children of containers not becoming invisible when they should
            // 0.8.3.5 - Fixed codegen bug when exposing a state in a component in a folder
            // 0.8.3.6 - Added ThreadStatic to the IsLayoutSuspended static bool so async loading will not interrupt foreground layout calls
            // 0.8.3.7 - Fixed relative file bug when loading BitmapFont files.
            // 0.8.3.8 - Fixed codegen assuming .gumx file is called GumProject.
            // 0.8.3.9 - Fixed path issues on Mac
            // 0.8.4.1 
            //  - Added support for components in Entities to be added to FRB/Gum layers
            //  - Added color values to rectangle and circle
            // 0.8.4.2
            //  - Fixed texture loading (for sprites) on the Mac
            // 0.8.5.0
            //  - Added setting the content manager even if there is no Gum screen, simplifying code-only Gum creation
            // 0.8.5.1
            //  - Fixed float parsing/code gen in languages that use comma for the digit separator.
            // 0.8.5.2
            //  - Fixed crash when trying to delete a Gum IDB
            //  - Fixed GumIDB being added to managers ininitialize rather than in AddToManagers, causing a crash on async loaded screens
            //  - Gum now generates Gum layers for under-all and top layer if any Gum instances are on those layers, instead of generating code that doesn't compile.
            // 0.8.5.3
            //  - Gum layers now set their own name at runtime
            //  - Removed a bit of memory allocation which occurred automatically in the Gum renderer
            // 0.8.6.4
            //  - Fixed rendering library by pulling in latest gum rendering code
            // 0.8.6.5
            //  - Added support to set single pixel texture and single pixel destination rectangle on the renderer.
            // 0.8.7.1
            //  - Updated to latest Gum rendering engine, addressing text rendering when using font scale
            //  - More fixes to loading files using aliases (for content pipeline)
            // 0.8.7.2
            //  - Fixed color on Text objects not rendering properly.
            // 0.8.7.3
            //  - Plugin now performs code generation when adding an existing .gumx file to the project
            // 0.8.7.4
            //  - Fixed text rendering when using an alpha value less than one, caused by premutiplied alpha.
            // 0.8.7.5
            //  - Fixed Gum calling save commands async - it should be sync!
            // 0.8.7.6
            //  - Fixed plugin not adding components in folders to the Glue project because of a path issue
            // 0.8.7.7
            //  - Fixed bug where components attached to a sprite wouldn't get their events raised
            // 0.8.7.8
            //  - Fixed overlapping children resulting in the wrong child getting UI events.
            // 0.8.7.9
            //  - Added GraphicalUiElement.GetAbsoluteWidth and GetAbsoluteHeight
            // 0.8.8.0
            // - Plugin no longer crashes if there is a missing screen file
            // 0.8.8.1
            // - Added TextRuntime.WrappedLines and TextRuntime.BitmapFont
            // 0.8.8.2
            // - Added support for adding an entire screen to a layer.
            // - Added SpriteRuntime.Text property.
            // 0.8.8.3
            // - Fixed viewport issues when ClipsChildren = true on a FRB XNA game
            // - Added property to address monogame GL bug here:  https://github.com/MonoGame/MonoGame/issues/5947
            // 0.8.8.4 
            // - Fixed cursor over on apps running with letterbox/pillarbox.
            // 0.8.8.5
            // - Updated to latest Gum rendering engine, allowing Text to specify pixel perfect or free floating positioning.
            // 0.8.8.6
            // - Fixed bug with Tweener not setting its Running to false when the elapsed passes duration
            get { return new Version(0, 8, 8, 6); }
        }

        #endregion

        #region Methods

        public override void StartUp()
        {
            propertiesManager = new GumxPropertiesManager();

            AssetTypeInfoManager.Self.AddCommonAtis();

            addGumProjectMenuItem = this.AddMenuItemTo("Add New Gum Project", HandleAddNewGumProject, "Content");
            //var bmp = new Bitmap(WindowsFormsApplication1.Properties.Resources.myimage);
            addGumProjectMenuItem.Image = new Bitmap(GumPlugin.Resource1.GumIcon);

            AssignEvents();

            CodeGeneratorManager.Self.CreateElementComponentCodeGenerators();

            Gum.Managers.StandardElementsManager.Self.Initialize();

        }

        private void AssignEvents()
        {
            this.ReactToLoadedGlux += HandleGluxLoad;

            this.ReactToLoadedGluxEarly += HandleGluxLoadEarly;

            this.ReactToUnloadedGlux += HandleUnloadedGlux;

            this.CanFileReferenceContent += FileReferenceTracker.CanTrackDependenciesOn;

            this.GetFilesReferencedBy += FileReferenceTracker.Self.HandleGetFilesReferencedBy;

            this.GetFilesNeededOnDiskBy += FileReferenceTracker.Self.HandleGetFilesNeededOnDiskBy;

            this.TryAddContainedObjects += ContainedObjectsManager.Self.HandleTryAddContainedObjects;

            this.ReactToTreeViewRightClickHandler += RightClickManager.Self.HandleTreeViewRightClick;

            this.ReactToFileChangeHandler += HandleFileChange;

            this.ReactToNewFileHandler += HandleNewFile;

            this.AddEventsForObject += EventsManager.Self.HandleAddEventsForObject;

            this.ReactToItemSelectHandler += HandleItemSelected;

            this.ReactToNewScreenCreated += HandleNewScreen;

            this.GetEventSignatureArgs += HandleGetEventSignatureArgs;
        }

        private void HandleGetEventSignatureArgs(NamedObjectSave namedObject, EventResponseSave eventResponseSave, out string type, out string args)
        {
            EventCodeGenerator.Self.HandleGetEventSignatureArgs(namedObject, eventResponseSave, out type, out args);
        }

        private void HandleNewScreen(FlatRedBall.Glue.SaveClasses.ScreenSave newScreen)
        {
            bool createGumScreen = propertiesManager.GetAutoCreateGumScreens();

            if(createGumScreen && AppState.Self.GumProjectSave != null)
            {
                string gumScreenName = FileManager.RemovePath( newScreen.Name ) + "Gum";

                bool exists = AppState.Self.GumProjectSave.Screens.Any(item => item.Name == gumScreenName);
                if (!exists)
                {
                    Gum.DataTypes.ScreenSave gumScreen = new Gum.DataTypes.ScreenSave();
                    gumScreen.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Screen"));
                    gumScreen.Name = gumScreenName;

                    string gumProjectFileName = GumProjectManager.Self.GetGumProjectFileName();

                    AppCommands.Self.AddScreen(gumScreen);

                    AppCommands.Self.SaveGlux(saveAllElements: false);

                    AppCommands.Self.SaveScreen(gumScreen);

                }
                // Select the screen to add the file to this
                GlueState.Self.CurrentScreenSave = newScreen;

                RightClickManager.Self.AddScreenByName(gumScreenName);
            }
        }

        private void HandleItemSelected(TreeNode selectedTreeNode)
        {
            bool shouldShowTab = GetIfShouldShowTab(selectedTreeNode);

            if(shouldShowTab)
            {
                if (control == null)
                {
                    control = new GumControl();
                    viewModel = new GumViewModel();
                    viewModel.PropertyChanged += HandleViewModelPropertyChanged;
                    control.DataContext = viewModel;

                    this.AddToTab(PluginManager.CenterTab, control, "Gum Properties");
                }
                else
                {
                    AddTab();
                }
                viewModel.SetFrom(AppState.Self.GumProjectSave, selectedTreeNode.Tag as ReferencedFileSave);
            }
            else
            {
                RemoveTab();
            }
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Maybe we'll need this at some point:
            //var referencedFileSave = GlueState.Self.CurrentReferencedFileSave;
            //var absoluteFileName = GlueCommands.Self.GetAbsoluteFileName(referencedFileSave);

            propertiesManager.HandlePropertyChanged(e.PropertyName);
        }

        private bool GetIfShouldShowTab(TreeNode selectedTreeNode)
        {
            if(selectedTreeNode != null && selectedTreeNode.Tag is ReferencedFileSave)
            {
                var rfs = selectedTreeNode.Tag as ReferencedFileSave;

                return FileManager.GetExtension( rfs.Name ) == "gumx";
            }

            return false;
        }

        private void HandleUnloadedGlux()
        {
            AssetTypeInfoManager.Self.UnloadProjectSpecificAtis();

            UpdateMenuItemVisibility();
        }

        private void HandleNewFile(ReferencedFileSave newFile)
        {
            string extension = FileManager.GetExtension(newFile.Name);
            
            if(extension == GumProjectSave.ProjectExtension)
            {
                bool isInGlobalContent = GlueState.Self.CurrentGlueProject.GlobalFiles.Contains(newFile);
                if (!isInGlobalContent)
                {
                    MessageBox.Show("The Gum project file (.gumx) should be added to global content. Not doing so may cause runtime errors.");
                }
                else
                {
                    // in global content, so generate the code files
                    EmbeddedResourceManager.Self.UpdateCodeInProjectPresence();
                }

            }
            // If it's a component then assign the specific type:
            else if(extension == GumProjectSave.ComponentExtension)
            {
                string componentName = FileManager.RemovePath(FileManager.RemoveExtension(newFile.Name));

                var componentType = Gum.Managers.ObjectFinder.Self.GetComponent(componentName);
                var componentTypeWithRuntime = componentName + "Runtime";

                var ati = AssetTypeInfoManager.Self.GetAtisForDerivedGues().FirstOrDefault(item => item.RuntimeTypeName == componentTypeWithRuntime);

                if (ati != null)
                {
                    newFile.RuntimeType = ati.QualifiedRuntimeTypeName.QualifiedType;
                }
            }

            UpdateMenuItemVisibility();
        }

        private void HandleFileChange(string fileName)
        {
            string extension = FileManager.GetExtension(fileName);

            if(Gum.Managers.ObjectFinder.Self.GumProjectSave != null)
            {


                if (extension == GumProjectSave.ComponentExtension ||
                    extension == GumProjectSave.ScreenExtension ||
                    extension == GumProjectSave.StandardExtension ||
                    extension == GumProjectSave.ProjectExtension)
                {
                    // November 1, 2015
                    // Why do we reload the
                    // entire project and not
                    // just the object that changed?
                    GumProjectManager.Self.ReloadProject();

                    // Something could have changed - more components could have been added
                    AssetTypeInfoManager.Self.AddProjectSpecificAtis();

                    if (extension == GumProjectSave.ProjectExtension)
                    {
                        CodeGeneratorManager.Self.GenerateDerivedGueRuntimes();
                    }
                    else
                    {
                        CodeGeneratorManager.Self.GenerateDueToFileChange(fileName);
                    }

                    // Behaviors could have been added, so generate them
                    CodeGeneratorManager.Self.GenerateAllBehaviors();

                    EventsManager.Self.RefreshEvents();

                    TaskManager.Self.AddSync(FileReferenceTracker.Self.RemoveUnreferencedFilesFromVsProject, "Removing unreferenced files for Gum project");
                }
                else if(extension == BehaviorReference.Extension)
                {
                    // todo: make this take just 1 behavior for speed
                    CodeGeneratorManager.Self.GenerateAllBehaviors();
                }
                else if (extension == "ganx")
                {
                    // Animations have changed, so we need to regenerate animation code.
                    // For now we'll generate everything, but we may want to make this faster
                    // and more precise by only generating the selected element:
                    CodeGeneratorManager.Self.GenerateDerivedGueRuntimes();
                }
            }
        }

        private void HandleAddNewGumProject(object sender, EventArgs e)
        {
            var added = GumProjectManager.Self.TryAddNewGumProject();

            if (added)
            {
                EmbeddedResourceManager.Self.UpdateCodeInProjectPresence();
            }
        }

        private void HandleGluxLoadEarly()
        {
            GumProjectManager.Self.ReloadProject();

            propertiesManager.UpdateUseAtlases();

            // These are needed for code gen
            AssetTypeInfoManager.Self.AddProjectSpecificAtis();

            // They may already have been added, which means the ATI for .gumx
            // would not be refreshed. Force it:
            AssetTypeInfoManager.Self.RefreshGumxLoadCode();

            EventsManager.Self.RefreshEvents();
        }

        private void HandleGluxLoad()
        {
            // todo: Removing a file should cause this to get called, but I don't think Gum lets us subscribe to that yet.
            EmbeddedResourceManager.Self.UpdateCodeInProjectPresence();

            CodeGeneratorManager.Self.GenerateDerivedGueRuntimes();

            CodeGeneratorManager.Self.GenerateAllBehaviors();


            TaskManager.Self.AddSync(FileReferenceTracker.Self.RemoveUnreferencedFilesFromVsProject, "Removing unreerenced files for Gum project");

            UpdateMenuItemVisibility();
        }

        private void UpdateMenuItemVisibility()
        {
            ReferencedFileSave gumRfs = null;
            if (GlueState.Self.CurrentGlueProject != null)
            {
                gumRfs = GumProjectManager.Self.GetRfsForGumProject();
            }
            addGumProjectMenuItem.Visible = GlueState.Self.CurrentGlueProject != null && gumRfs == null;

        }

        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason)
        {
            Glue.MainGlueWindow.Self.Invoke((MethodInvoker)RemoveAllMenuItems);

            CodeGeneratorManager.Self.RemoveCodeGenerators();


            return true;
        }

        #endregion
    }
}
