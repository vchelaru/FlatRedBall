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
using System.Diagnostics;
using FlatRedBall.Glue.Errors;
using GumPluginCore.Managers;
using FlatRedBall.Glue.Controls;
using GumPluginCore.ViewModels;

namespace GumPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        #region Fields

        GumControl control;
        GumViewModel viewModel;
        GumToolbarViewModel toolbarViewModel;
        GumxPropertiesManager propertiesManager;

        ToolStripMenuItem addGumProjectMenuItem;

        GlobalContentCodeGenerator globalContentCodeGenerator;
        GumToolbar gumToolbar;

        bool raiseViewModelEvents = true;

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
            // 0.9.0.0
            // - Added new Width Unit and Height Unit for depending on other dimension
            // - Fonts with spaces now generate with '_'instead of ' ' 
            // 0.9.1
            // - Huge performance improvements from using the generated default state when instantiating screens/components
            //   rather than the reflection-based state
            // - Names with dashes now properly instantiate a member rather than using the modified name when searching and
            //   obtaining a null reference
            // - Parent can now be set through states
            // - Children Layout can now be set through states
            // - Removed more reflection from setting a StateSave on a screen/component.
            // - Other micro-optimizations to make state setting faster.
            // 0.9.1.1
            // - Fixed setting custom fonts not considering the relative directory
            // 0.9.1.2
            // - Fixed order of CustomInitialize with default state - custom init comes last
            // 0.9.1.3
            // - Fixed list items not updating their parent due to an earlier fix.
            // 0.9.1.4
            // - Exporting again to make sure I have all the latest changes
            // 0.9.2.0
            // - Added new TextBox behavior save
            // - Added new Button behavior save
            // - Started on ScrollBar behavior save
            // - Fixed a bug where children of a stack layout woulnd't update if added at runtime
            // 0.9.2.1
            // - Fixed components which inherit from nineslice not properly setting up necessary files in Glue project.
            // 0.9.2.2
            // - Fixed bug - derived Screens could not access Gum layers defined in base through Glue layers.
            // - Fixed bug - Screens without Gum screens now use define and associate layers using IDBs.
            // 0.9.2.3
            // - State interpolation is now removed if Glue is set to not embed code.
            // 0.9.2.4
            // - GUE's no longer raise events if disabled, enabling the GuiManager to make 
            //   GUEs with HasEvents = false be "input transparent"
            // 0.9.3
            // - Huge change - plugin can now automatically assign .Forms controls to Gum runtimes based on their behaviors
            // - Added Checkbox behavior
            // - Fixed bugs when adding state categories to standard objects.
            // 0.9.3.1
            // - Improved TweenerManager to not add a removed event, potentially saving lots of memory allocations.
            // 0.9.3.2
            // - Huge reduction in every-frame memory allocation by pooling render state info lists
            // 0.9.3.3
            // - Lots more reduction in every-frame memory allocation by making Text objects only update when width has changed, removing LINQ calls
            // 0.9.4.0
            // - Updated plugin to use the latest error reporting in case an XML file can't be parsed
            // 1.0.0.0
            // - HUGE update - Gum plugin can now create FlatRedBall.Forms default controls and inject them in the project.
            // - Added Gum icon to add new project, or to open project instead of having to search for the .gumx
            // - Changed defaults when creating Gum projects to auto-add to Glue screens
            // - Simplified adding .Forms down to a single button
            // 1.0.0.2
            // - Moved all embedded Gum objects to a folder structure with a .gumx so it can be edited easily.
            // 1.1
            // - Added support for UserControl
            // 1.1.1 
            // - Text no longer trims its trailing spaces, allowing spaces to grow auto-sized Text objects
            // 1.2 
            // - Added support for text objects with automatically generated fonts to specify if they use font smoothing.
            // 1.2.1
            // - If an object has text-based variables (like Font), but it is not a text object, the object will no longer
            //   report a referenced file. This fixes a bug that can happen when an old Text object gets converted to a different
            //   type like a component
            // 1.3.0
            // - Introduced TreeView and TreeViewItem support
            // - Improved layout performance in a few situations, especially list boxes and tree views in .Forms
            // - Fixed a number of crashes which can occur when a standard file (like Circle) is missing            
            // 1.4.0
            // - Introduced support for subfolders and matching namespaces for components
            // 1.4.1
            // - The Gum toolbar disappears if the Gum plugin is shut down
            // 1.4.2
            // - Added FloatRectangle as an embedded file for legacy projects
            // 1.5.0
            // - Added new PasswordBox control
            // 1.5.1
            // - Plugin only reloads project if the file that changed is the main Gum project. This is needed
            //   for the new level editor that can make a secondary gum project.
            // 1.5.2
            // - Fixed code generation including ContainedType property on Container object.
            // 1.6.0
            // - Added support for Polygon
            // 1.6.0.1
            // - Fixed codegen issue with states from components in folders.
            // 1.6.0.2
            // - Fixed a bug where drag+dropping an object on Events would not show
            //   children instance events if the parent container was inheriting its ExposeChildrenEvents value
            // 1.6.1.0
            // - Added Color property to generated standard elements.
            // 1.6.2.0
            // - Fixed double-loading of Gum screens in a derived Glue screen.
            // - Added GumIdb.Self for global access to GumIdb
            // 1.6.2.1
            // - More fixes when tracking files on instances that have been removed from their respective screen or component
            // 1.6.2.2
            // - Fixed the plugin reporting errors for referenced files when those files are tied to deleted instances.
            // 2.0
            // - Screens no longer generate IDBs - they are just GraphicalUiElements
            // 2.0.1
            // - Fixed freeze/behavior generation failing because a directory was not being created
            // 2.0.2
            // - Add forms button now indicates that it will refresh forms controls.
            // - Adding/refreshing forms gum components will now ask you if you want to replace them
            // 2.0.3
            // - Fixed bug in code generation when inheriting one component from another component that is in a folder.
            // 2.0.4
            // - Added missing Polygon standard
            // 2.0.5
            // - Gum plugin will now ask to make itself 
            // 2.0.6
            // - Fixed compile error generated if state begins with a number like "1st". Now it prefixes an underscore
            // 2.0.7
            // - Fixed adding Gum objects to Layers not working now that Screens no longer inherit from GumIdb
            // 2.0.8
            // - Custom code for screns is no longer indented one tab
            // 2.1
            // - Removed resolution setup for specific screens because the new Glue does it automatically
            // 2.2
            // - Added support for text box multiline
            // - Added support for multiple categories per gum component
            // 2.2.1
            // - Improved performance by  making the "Removing unreferenced files for Gum project" action add or move to end, eliminating
            //   unnecessary processing
            // 2.2.2
            // - Fixed Circle.Radius setting width and height incorrectly
            // 2.2.3
            // - Fixed possible crash when reading midding event export JSON files
            // 2.2.4
            // - FlatRedBall XNA can now have .forms, but a warning message is displayed
            // 2.2.5
            // - Fixed missing semicolon at the end of generated code
            // 2.3
            // - Added new classes for creating/updating shapes from Gum
            // 2.4
            // - Added Gum animation speed
            // 2.4.1
            // - Fixed a variety of bugs where files weren't generated/saved
            get { return new Version(2, 4, 1); }
        }

        #endregion

        #region Methods

        public override void StartUp()
        {
            propertiesManager = new GumxPropertiesManager();

            AssetTypeInfoManager.Self.AddCommonAtis();

            addGumProjectMenuItem = this.AddMenuItemTo("Add New Gum Project", HandleAddNewGumProjectMenuItemClicked, "Content");
            //var bmp = new Bitmap(WindowsFormsApplication1.Properties.Resources.myimage);
            addGumProjectMenuItem.Image = new Bitmap(GumPluginCore.Resource1.GumIcon);

            AssignEvents();

            CreateToolbar();

            CodeGeneratorManager.Self.CreateElementComponentCodeGenerators();

            globalContentCodeGenerator = new GlobalContentCodeGenerator();
            FlatRedBall.Glue.Parsing.CodeWriter.GlobalContentCodeGenerators.Add(globalContentCodeGenerator);

            Gum.Managers.StandardElementsManager.Self.Initialize();

            //EditorObjects.IoC.Container.Get<IErrorContainer>().
            var error = new GumPluginCore.ErrorReporting.ErrorReporter();

            EditorObjects.IoC.Container.Get<List<IErrorReporter>>().Add(error);

        }

        private void AssignEvents()
        {
            this.AddEventsForObject += EventsManager.Self.HandleAddEventsForObject;

            this.AdjustDisplayedEntity += GumCollidableManager.HandleDisplayedEntity;

            this.CanFileReferenceContent += FileReferenceTracker.CanTrackDependenciesOn;

            this.FillWithReferencedFiles += FileReferenceTracker.Self.HandleFillWithReferencedFiles;

            this.GetFilesNeededOnDiskBy += FileReferenceTracker.Self.HandleGetFilesNeededOnDiskBy;

            this.ReactToFileChangeHandler += FileChangeManager.Self.HandleFileChange;

            this.ReactToLoadedGlux += HandleGluxLoad;

            this.ReactToLoadedGluxEarly += HandleGluxLoadEarly;

            this.ReactToNewFileHandler += HandleNewFile;

            this.ReactToTreeViewRightClickHandler += RightClickManager.Self.HandleTreeViewRightClick;

            this.ReactToUnloadedGlux += HandleUnloadedGlux;

            this.TryAddContainedObjects += ContainedObjectsManager.Self.HandleTryAddContainedObjects;

            this.ReactToItemSelectHandler += HandleItemSelected;

            this.NewScreenCreated += HandleNewScreen;

            this.GetEventSignatureArgs += HandleGetEventSignatureArgs;

            this.GetUsedTypes = HandleGetUsedTypes;

            this.GetAvailableAssetTypes = HandleGetAvailableAssetTypes;

            this.ReactToFileRemoved += HandleFileRemoved;
        }

        private void CreateToolbar()
        {
            toolbarViewModel = new GumToolbarViewModel();

            gumToolbar = new GumToolbar();
            gumToolbar.DataContext = toolbarViewModel;
            gumToolbar.GumButtonClicked += HandleToolbarButtonClick;
            base.AddToToolBar(gumToolbar, "Standard");
        }

        private void HandleFileRemoved(IElement container, ReferencedFileSave file)
        {
            if(file.Name.EndsWith(".gumx"))
            {
                // gum project was removed, so mark it as removed:
                AppState.Self.GumProjectSave = null;
            }

            toolbarViewModel.HasGumProject =
                AppState.Self.GumProjectSave != null;
        }

        private void HandleToolbarButtonClick(object sender, EventArgs e)
        {
            var alreadyHasGumProject = AppState.Self.GumProjectSave != null;

            if(alreadyHasGumProject == false)
            {
                HandleAddNewGumProjectMenuItemClicked(null, null);
            }
            else
            {
                // open the Gum file:
                var fileName = AppState.Self.GumProjectSave.FullFileName;

                var startInfo = new ProcessStartInfo();
                startInfo.FileName = fileName;
                startInfo.UseShellExecute = true;

                try
                {
                    System.Diagnostics.Process.Start(startInfo);
                }
                catch(Win32Exception winException)
                {
                    var message = "Could not open the Gum project - have you set up the Gum tool to be associated with the .gumx file format in Windows Explorer?";

                    GlueCommands.Self.DialogCommands.ShowMessageBox(message);
                }
                catch
                {
                    GlueCommands.Self.DialogCommands.ShowMessageBox("Unknown error attempting to open Gum") ;
                }
            }


            toolbarViewModel.HasGumProject = AppState.Self.GumProjectSave != null;
        }

        private List<AssetTypeInfo> HandleGetAvailableAssetTypes(ReferencedFileSave referencedFileSave)
        {
            var element = CodeGeneratorManager.GetElementFrom(referencedFileSave);

            if(element != null)
            {

                var foundItem = AssetTypeInfoManager.Self.AssetTypesForThisProject
                    .Where(item => item.QualifiedRuntimeTypeName.QualifiedType ==
                        GueDerivingClassCodeGenerator.Self.GetQualifiedRuntimeTypeFor(element));

                if(foundItem.Any())
                {
                    return foundItem.ToList();
                }
            }

            return null;
        }

        private List<Type> HandleGetUsedTypes()
        {
            List<Type> toReturn = new List<Type>();

            toReturn.AddRange(typeof(ChildrenLayout)
                .Assembly.GetTypes().Where(item => item.IsEnum));
            toReturn.AddRange(typeof(RenderingLibrary.Graphics.HorizontalAlignment)
                .Assembly.GetTypes().Where(item => item.IsEnum));
            toReturn.AddRange(typeof(Gum.Converters.GeneralUnitType)
                .Assembly.GetTypes().Where(item => item.IsEnum));
            return toReturn;
        }

        private void HandleGetEventSignatureArgs(NamedObjectSave namedObject, EventResponseSave eventResponseSave, out string type, out string args)
        {
            EventCodeGenerator.Self.HandleGetEventSignatureArgs(namedObject, eventResponseSave, out type, out args);
        }

        private void HandleNewScreen(FlatRedBall.Glue.SaveClasses.ScreenSave newScreen)
        {
            bool createGumScreen = propertiesManager.GetShouldAutoCreateGumScreens();

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

                    AppCommands.Self.AddScreenToGumProject(gumScreen);

                    AppCommands.Self.SaveGumx(saveAllElements: false);

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
                raiseViewModelEvents = false;
                viewModel.SetFrom(AppState.Self.GumProjectSave, selectedTreeNode.Tag as ReferencedFileSave);
                raiseViewModelEvents = true;
                control.ManuallyRefreshRadioButtons();
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
            if(raiseViewModelEvents)
            {
                propertiesManager.HandlePropertyChanged(e.PropertyName);
            }
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

            AppState.Self.GumProjectSave = null;

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
                    var gumRfs = GumProjectManager.Self.GetRfsForGumProject();
                    var behavior = GetBehavior(gumRfs);

                    // only do this if the property reactor is reacting to changes - if it's not, then we're still
                    // setting up the new file:
                    if(this.propertiesManager.IsReactingToProperyChanges)
                    {
                        EmbeddedResourceManager.Self.UpdateCodeInProjectPresence(behavior);
                        GlueCommands.Self.ProjectCommands.SaveProjectsTask();
                    }
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

        private static FileAdditionBehavior GetBehavior(ReferencedFileSave gumRfs)
        {
            var behavior = FileAdditionBehavior.EmbedCodeFiles;
            if (gumRfs != null)
            {
                behavior = (FileAdditionBehavior)gumRfs.Properties.GetValue<int>(nameof(FileAdditionBehavior));
            }

            return behavior;
        }

        private void HandleAddNewGumProjectMenuItemClicked(object sender, EventArgs e)
        {
            CreateGumProject();
        }

        public void CreateGumProject()
        {
            propertiesManager.IsReactingToProperyChanges = false;

            var added = GumProjectManager.Self.TryAddNewGumProject();

            if (added)
            {
                var gumRfs = GumProjectManager.Self.GetRfsForGumProject();

                var behavior = GetBehavior(gumRfs);

                EmbeddedResourceManager.Self.UpdateCodeInProjectPresence(behavior);

                // show the tab for the new file:
                this.FocusTab();

                TaskManager.Self.Add(
                    () =>
                    {
                        // When we first add the RFS to Glue, the RFS tries to refresh its file cache.
                        // But since the .glux hasn't yet been assigned as the currently-loaded project, 
                        // the Gum plugin doesn't track its references and returns an empty list. That empty
                        // list return is then cached, and future calls will always treat the .gumx as having 
                        // no referenced files. Now that we've assigned the custom project, clear the cache so
                        // it can properly be set up.
                        GlueCommands.Self.FileCommands.ClearFileCache(
                            GlueCommands.Self.GetAbsoluteFileName(gumRfs));
                        GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(gumRfs);

                    },
                    "Adding Gum referenced files to project");

                GlueCommands.Self.GluxCommands.SaveGluxTask();

                GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(
                    "Would you like to mark the Gum plugin as a required plugin for this project? " +
                    "This can help others who open this project",
                    yesAction: HandleMakePluginRequiredYes);
            }



            propertiesManager.IsReactingToProperyChanges = true;

            toolbarViewModel.HasGumProject = AppState.Self.GumProjectSave != null;
        }

        private void HandleMakePluginRequiredYes()
        {
            var changed = GlueCommands.Self.GluxCommands.SetPluginRequirement(this, true);

            if(changed)
            {
                GlueCommands.Self.GluxCommands.SaveGluxTask();
             }
        }

        private void HandleGluxLoadEarly()
        {
            GumProjectManager.Self.ReloadGumProject();

            propertiesManager.UpdateUseAtlases();

            // These are needed for code gen
            AssetTypeInfoManager.Self.RefreshProjectSpecificAtis();

            EventsManager.Self.RefreshEvents();

            toolbarViewModel.HasGumProject = AppState.Self.GumProjectSave != null;
        }

        private void HandleGluxLoad()
        {
            var gumRfs = GumProjectManager.Self.GetRfsForGumProject();

            toolbarViewModel.HasGumProject = gumRfs != null;

            if(gumRfs != null)
            {

                var behavior = GetBehavior(gumRfs);

                // todo: Removing a file should cause this to get called, but I don't think Gum lets us subscribe to that yet.
                TaskManager.Self.AddSync(() =>
                {
                    EmbeddedResourceManager.Self.UpdateCodeInProjectPresence(behavior);

                    CodeGeneratorManager.Self.GenerateDerivedGueRuntimes();

                    CodeGeneratorManager.Self.GenerateAllBehaviors();

                    FileReferenceTracker.Self.RemoveUnreferencedFilesFromVsProject();

                    UpdateMenuItemVisibility();

                    // the UpdatecodeInProjectPresence may add new files, so save:
                    GlueCommands.Self.ProjectCommands.SaveProjectsTask();

                }, "Gum plugin reacting to glux load");
            }
        }

        private void UpdateMenuItemVisibility()
        {
            ReferencedFileSave gumRfs = null;
            if (GlueState.Self.CurrentGlueProject != null)
            {
                gumRfs = GumProjectManager.Self.GetRfsForGumProject();
            }

            GlueCommands.Self.DoOnUiThread(() =>
           {
               addGumProjectMenuItem.Visible = GlueState.Self.CurrentGlueProject != null && gumRfs == null;

           });

        }

        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason)
        {
            Glue.MainGlueWindow.Self.Invoke((MethodInvoker)RemoveAllMenuItems);

            CodeGeneratorManager.Self.RemoveCodeGenerators();

            FlatRedBall.Glue.Parsing.CodeWriter.GlobalContentCodeGenerators.Remove(globalContentCodeGenerator);

            if(gumToolbar != null)
            {
                base.RemoveFromToolbar(gumToolbar, "Standard");
            }

            return true;
        }

        #endregion
    }
}
