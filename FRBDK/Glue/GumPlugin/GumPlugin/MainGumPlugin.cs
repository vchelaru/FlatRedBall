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
using GumPlugin.Managers;
using FlatRedBall.Glue.Controls;
using GumPlugin.ViewModels;
using GumPlugin.DataGeneration;
using FlatRedBall.Glue.FormHelpers;
using System.Threading.Tasks;
using HQ.Util.Unmanaged;
using System.IO;
using GumPlugin.CodeGeneration;

namespace GumPlugin;

[Export(typeof(PluginBase))]
public class MainGumPlugin : PluginBase
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

    PluginTab tab;

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
        // We don't actively maintain this version anymore. Sometimes it is incremented for debugging
        get { return new Version(2, 4, 2); }
    }

    #endregion

    #region Initialize/Assing Events

    public override void StartUp()
    {
        propertiesManager = new GumxPropertiesManager();

        AssetTypeInfoManager.Self.AddCommonAtis();

        addGumProjectMenuItem = this.AddMenuItemTo(Localization.Texts.ProjectNewGum, Localization.MenuIds.ProjectNewGumId, HandleAddNewGumProjectMenuItemClicked, Localization.MenuIds.ContentId);
        //var bmp = new Bitmap(WindowsFormsApplication1.Properties.Resources.myimage);
        addGumProjectMenuItem.Image = new Bitmap(GumPlugin.Resource1.GumIcon);

        AssignEvents();

        CreateToolbar();

        CodeGeneratorManager.Self.CreateCodeGenerators(this);

        globalContentCodeGenerator = new GlobalContentCodeGenerator();
        FlatRedBall.Glue.Parsing.CodeWriter.GlobalContentCodeGenerators.Add(globalContentCodeGenerator);

        Gum.Managers.StandardElementsManager.Self.Initialize();
        SkiaStandardElementsManager.AddSkiaStandards();


        //EditorObjects.IoC.Container.Get<IErrorContainer>().
        var error = new GumPlugin.ErrorReporting.ErrorReporter();

        AddErrorReporter(error);

        viewModel = new GumViewModel();
        GumPluginCommands.Self.GumViewModel = viewModel;
        viewModel.PropertyChanged += HandleViewModelPropertyChanged;
    }

    private void AssignEvents()
    {
        this.AddEventsForObject += EventsManager.Self.HandleAddEventsForObject;

        this.AdjustDisplayedEntity += GumCollidableManager.HandleDisplayedEntity;

        #region File-related

        this.CanFileReferenceContent += FileReferenceTracker.CanTrackDependenciesOn;

        this.FillWithReferencedFiles += FileReferenceTracker.Self.HandleFillWithReferencedFiles;

        this.ReactToFileChange += FileChangeManager.Self.HandleFileChange;

        this.ReactToNewFileHandler += HandleNewFile;

        this.ReactToFileRemoved += HandleFileRemoved;

        #endregion

        #region Glux-related

        this.ReactToLoadedGluxEarly += HandleGluxLoadEarly;

        this.ReactToLoadedGlux += HandleGluxLoad;

        this.ReactToUnloadedGlux += HandleUnloadedGlux;

        #endregion

        #region Tree node/selection

        this.ReactToTreeViewRightClickHandler += RightClickManager.Self.HandleTreeViewRightClick;

        this.ReactToItemSelectHandler += HandleItemSelected;

        this.TryHandleTreeNodeDoubleClicked += HandleTreeNodeDoubleClicked;

        #endregion

        #region Add/Remove Glue Screen

        this.NewScreenCreated += HandleNewScreen;

        this.ReactToScreenRemoved += HandleScreenRemoved;

        #endregion

        this.TryAddContainedObjects += ContainedObjectsManager.Self.HandleTryAddContainedObjects;

        this.GetEventSignatureArgs += HandleGetEventSignatureArgs;

        this.GetUsedTypes = HandleGetUsedTypes;

        this.GetAvailableAssetTypes = HandleGetAvailableAssetTypes;

        this.ResolutionChanged += HandleResolutionChanged;

        this.ScaleGumChanged += HandleScaleGumChanged;
    }


    #endregion

    #region Methods

    private void HandleResolutionChanged()
    {
        if (viewModel?.IsMatchGameResolutionInGumChecked == true)
        {
            GumPluginCommands.Self.UpdateGumToGlueResolution();
        }
    }

    private void HandleScaleGumChanged()
    {
        if (viewModel?.IsMatchGameResolutionInGumChecked == true)
        {
            GumPluginCommands.Self.UpdateGumToGlueResolution();
        }
    }

    private bool HandleTreeNodeDoubleClicked(ITreeNode arg)
    {
        var nos = arg.Tag as NamedObjectSave;

        if (nos != null && nos.SourceType == SourceType.FlatRedBallType &&
            !string.IsNullOrEmpty(nos.SourceClassType))
        {
            var ati = nos.GetAssetTypeInfo();

            var isGumAti = AssetTypeInfoManager.Self.AssetTypesForThisProject
                .Any(item => item.QualifiedRuntimeTypeName.QualifiedType == ati?.QualifiedRuntimeTypeName.QualifiedType);

            if (isGumAti)
            {
                var qualified = ati.QualifiedRuntimeTypeName.QualifiedType;
                // open it!
                var endsInRuntime = qualified.EndsWith("Runtime");

                if (endsInRuntime)
                {
                    var withoutRuntime = qualified.Substring(0, qualified.Length - "Runtime".Length);
                    var lastPeriod = withoutRuntime.LastIndexOf(".");
                    var strippedName = withoutRuntime.Substring(lastPeriod + 1);

                    var matchingComponent = AppState.Self.GumProjectSave.Components.FirstOrDefault(
                        item => item.Name == strippedName || item.Name.EndsWith("/" + strippedName));

                    if (matchingComponent != null)
                    {
                        string fileName = AppState.Self.GumProjectFolder +
                            "Components/" + matchingComponent.Name + "." + GumProjectSave.ComponentExtension;

                        var startInfo = new ProcessStartInfo();
                        startInfo.FileName = "\"" + fileName + "\"";
                        startInfo.UseShellExecute = true;

                        System.Diagnostics.Process.Start(startInfo);
                    }
                }

                return true;
            }
        }

        return false;
    }

    private void CreateToolbar()
    {
        toolbarViewModel = new GumToolbarViewModel();

        gumToolbar = new GumToolbar();
        gumToolbar.DataContext = toolbarViewModel;
        gumToolbar.GumButtonClicked += HandleToolbarButtonClick;
    }

    public bool HasGum() => AppState.Self.GumProjectSave != null;

    private void HandleFileRemoved(IElement container, ReferencedFileSave file)
    {
        if (file.Name.EndsWith(".gumx"))
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

        if (alreadyHasGumProject == false)
        {
            HandleAddNewGumProjectMenuItemClicked(null, null);
        }
        else
        {
            GlueCommands.Self.FileCommands.OpenFileInDefaultProgram(AppState.Self.GumProjectSave.FullFileName);
        }

        toolbarViewModel.HasGumProject = AppState.Self.GumProjectSave != null;
    }

    private List<AssetTypeInfo> HandleGetAvailableAssetTypes(ReferencedFileSave referencedFileSave)
    {
        var element = CodeGeneratorManager.GetElementFrom(referencedFileSave);

        if (element != null)
        {

            var foundItem = AssetTypeInfoManager.Self.AssetTypesForThisProject
                .Where(item => item.QualifiedRuntimeTypeName.QualifiedType ==
                    GueDerivingClassCodeGenerator.Self.GetQualifiedRuntimeTypeFor(element, prefixGlobal:false));

            if (foundItem.Any())
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

    private async Task HandleNewScreen(FlatRedBall.Glue.SaveClasses.ScreenSave newFrbScreen)
    {
        await TaskManager.Self.AddAsync(async () =>
        {
            var createGumScreen = propertiesManager.GetShouldAutoCreateGumScreens();

            if (createGumScreen && AppState.Self.GumProjectSave != null)
            {
                await GumPluginCommands.Self.AddScreenForGlueScreen(newFrbScreen);
            }

        }, String.Format(Localization.Texts.GumPluginHandleNewFrbScreen , newFrbScreen));
    }


    private void HandleScreenRemoved(FlatRedBall.Glue.SaveClasses.ScreenSave glueScreen, List<string> listToFillWithAdditionalFilesToRemove)
    {
        if (AppState.Self.GumProjectSave != null)
        {
            var gumScreenName = GumPluginCommands.Self.GetGumScreenNameFor(glueScreen);

            var gumScreen = AppState.Self.GumProjectSave.Screens.FirstOrDefault(item => item.Name == gumScreenName);

            if (gumScreen != null)
            {
                var result = GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(
                    String.Format(Localization.Texts.GumConfirmDeleteScreen,gumScreen.Name, glueScreen));
                if(result == System.Windows.MessageBoxResult.Yes)
                {
                    // don't await this, we want this method to immediately add to the lists
                    _=GumPluginCommands.Self.RemoveScreen(gumScreen);

                    listToFillWithAdditionalFilesToRemove.Add(CodeGeneratorManager.Self.CustomRuntimeCodeLocationFor(gumScreen).FullPath);
                    listToFillWithAdditionalFilesToRemove.Add(CodeGeneratorManager.Self.GeneratedRuntimeCodeLocationFor(gumScreen).FullPath);

                    // If there are forms screens, remove those too:
                    var formsCustomFilePath = CodeGeneratorManager.Self.CustomFormsCodeLocationFor(gumScreen);

                    if(formsCustomFilePath.Exists())
                    {
                        listToFillWithAdditionalFilesToRemove.Add(formsCustomFilePath.FullPath);
                    }

                    var formsGeneratedFilePath = CodeGeneratorManager.Self.GeneratedFormsCodeLocationFor(gumScreen);
                    if(formsGeneratedFilePath.Exists())
                    {
                        listToFillWithAdditionalFilesToRemove.Add(formsGeneratedFilePath.FullPath);
                    }
                }

            }


        }
    }

    private void HandleItemSelected(ITreeNode selectedTreeNode)
    {
        bool shouldShowTab = GetIfShouldShowTab(selectedTreeNode);

        if (shouldShowTab)
        {
            if (control == null)
            {
                CreateGumControl();
            }
            tab.Show();
            raiseViewModelEvents = false;
            viewModel.SetFrom(AppState.Self.GumProjectSave, selectedTreeNode.Tag as ReferencedFileSave);
            raiseViewModelEvents = true;
            control.ManuallyRefreshRadioButtons();
        }
        else
        {
            tab?.Hide();
        }
    }

    private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        // Maybe we'll need this at some point:
        //var referencedFileSave = GlueState.Self.CurrentReferencedFileSave;
        //var absoluteFileName = GlueCommands.Self.GetAbsoluteFileName(referencedFileSave);
        if (raiseViewModelEvents && !viewModel.IsUpdatingFromGlueObject)
        {
            propertiesManager.HandlePropertyChanged(e.PropertyName);
        }
    }

    private bool GetIfShouldShowTab(ITreeNode selectedTreeNode)
    {
        if (selectedTreeNode?.Tag is ReferencedFileSave rfs)
        {
            return FileManager.GetExtension(rfs.Name) == "gumx";
        }

        return false;
    }

    private void HandleUnloadedGlux()
    {
        base.RemoveFromToolbar(gumToolbar, Localization.Texts.Tools);

        AssetTypeInfoManager.Self.UnloadProjectSpecificAtis();

        AppState.Self.GumProjectSave = null;

        UpdateMenuItemVisibility();
    }

    private void HandleNewFile(ReferencedFileSave newFile, AssetTypeInfo assetTypeInfo)
    {
        string extension = FileManager.GetExtension(newFile.Name);

        if (extension == GumProjectSave.ProjectExtension)
        {
            bool isInGlobalContent = GlueState.Self.CurrentGlueProject.GlobalFiles.Contains(newFile);
            if (!isInGlobalContent)
            {
                MessageBox.Show(Localization.Texts.GumProjectFileCanOnlyAddedToGlobal);

                var container = FlatRedBall.Glue.Elements.ObjectFinder.Self.GetElementContaining(newFile);

                if (container != null)
                {
                    container.ReferencedFiles.Remove(newFile);
                }
            }
            else
            {
                GumProjectManager.Self.ReloadGumProject();

                // in global content, so proceed with logic

                var gumRfs = GumProjectManager.Self.GetRfsForGumProject();

                // Gum RFS should be first so that it loads before any global content Gum screens:
                if(gumRfs != null)
                {
                    var project = GlueState.Self.CurrentGlueProject;
                    var globalFiles = project.GlobalFiles;

                    TaskManager.Self.AddAsync(() =>
                    {
                        if (globalFiles[0] != gumRfs)
                        {
                            globalFiles.Remove(gumRfs);
                            globalFiles.Insert(0, gumRfs);
                        }
                    }, Localization.Texts.GumReordering);
                }

                // only do this if the property reactor is reacting to changes - if it's not, then we're still
                // setting up the new file:
                if (this.propertiesManager.IsReactingToProperyChanges)
                {
                    var behavior = GetBehavior(gumRfs);
                    EmbeddedResourceManager.Self.UpdateCodeInProjectPresence(behavior);
                    GlueCommands.Self.ProjectCommands.SaveProjects();
                }

                GumProjectManager.SetGumxReferencedFileSaveDefaults(gumRfs);
            }

        }
        // If it's a component then assign the specific type:
        else if (extension == GumProjectSave.ComponentExtension)
        {
            string componentName = FileManager.RemovePath(FileManager.RemoveExtension(newFile.Name));

            var componentTypeWithRuntime = componentName + "Runtime";

            var ati = AssetTypeInfoManager.Self.GetAtisForDerivedGues().FirstOrDefault(item => item.RuntimeTypeName == componentTypeWithRuntime);

            if (ati != null)
            {
                newFile.RuntimeType = ati.QualifiedRuntimeTypeName.QualifiedType;
            }
        }
        else if (extension == GumProjectSave.ScreenExtension)
        {
            string screenName = FileManager.RemovePath(FileManager.RemoveExtension(newFile.Name));

            var screenTypeWithRuntime = screenName + "Runtime";

            var ati = AssetTypeInfoManager.Self.GetAtisForDerivedGues().FirstOrDefault(item => item.RuntimeTypeName == screenTypeWithRuntime);

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
            // At some point Glue no longer shows this UI. Therefore, new Gum projects cannot have this value modified
            // If we are on a newer version, then don't embed files:
            if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.GumGueHasGetAnimation)
            {
                behavior = FileAdditionBehavior.IncludeNoFiles;
            }
            else
            {
                behavior = (FileAdditionBehavior)gumRfs.Properties.GetValue<int>(nameof(FileAdditionBehavior));
            }
        }

        return behavior;
    }

    private void HandleAddNewGumProjectMenuItemClicked(object sender, EventArgs e)
    {
        AskToCreateGumProject();
    }

    public async Task CreateGumProjectWithForms(bool askToOverwrite)
    {
        await CreateGumProjectInternal(shouldAlsoAddForms: true, askToOverwrite: askToOverwrite);
    }

    public async Task CreateGumProjectNoForms(bool askToOverwrite)
    {
        await CreateGumProjectInternal(shouldAlsoAddForms: false, askToOverwrite: askToOverwrite);
    }

    public async void AskToCreateGumProject()
    {
        var mbmb = new MultiButtonMessageBoxWpf();
        mbmb.AddButton(Localization.Texts.GumIncludeRecommendedFormsControls, true);
        mbmb.AddButton(Localization.Texts.GumNoForms, false);
        mbmb.MessageText = Localization.Texts.GumAddFRBForms;
        var showDialogResult = mbmb.ShowDialog();

        if (showDialogResult == true)
        {
            var shouldAlsoAddForms = (bool)mbmb.ClickedResult;
            await CreateGumProjectInternal(shouldAlsoAddForms, askToOverwrite:true);
        }
    }

    private async Task CreateGumProjectInternal(bool shouldAlsoAddForms, bool askToOverwrite)
    {
        var assembly = typeof(FormsControlAdder).Assembly;
        var shouldSave = true;
        if (askToOverwrite)
        {
            shouldSave = FormsControlAdder.AskToSaveIfOverwriting(assembly);
        }

        if (GlueState.Self.CurrentGlueProject == null)
        {
            MessageBox.Show(Localization.Texts.GumErrorCreateFRBFirst);
            shouldSave = false;
        }

        else if (GumProjectManager.Self.GetIsGumProjectAlreadyInGlueProject())
        {
            MessageBox.Show(Localization.Texts.GumProjectAlreadyExists);
            shouldSave = false;
        }

        if (shouldSave)
        {

            if (control == null)
            {
                GlueCommands.Self.DoOnUiThread(CreateGumControl);
            }
            await TaskManager.Self.AddAsync(async () =>
            {
                propertiesManager.IsReactingToProperyChanges = false;
                GumProjectManager.Self.AddNewGumProject();

                var gumRfs = GumProjectManager.Self.GetRfsForGumProject();

                var behavior = GetBehavior(gumRfs);
                EmbeddedResourceManager.Self.UpdateCodeInProjectPresence(behavior);

                // show the tab for the new file:
                tab.Focus();

                // When we first add the RFS to Glue, the RFS tries to refresh its file cache.
                // But since the .glux hasn't yet been assigned as the currently-loaded project, 
                // the Gum plugin doesn't track its references and returns an empty list. That empty
                // list return is then cached, and future calls will always treat the .gumx as having 
                // no referenced files. Now that we've assigned the custom project, clear the cache so
                // it can properly be set up.
                GlueCommands.Self.FileCommands.ClearFileCache(GlueCommands.Self.GetAbsoluteFilePath(gumRfs));
                GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(gumRfs);



                if (shouldAlsoAddForms == true)
                {
                    // add forms:

                    //viewModel.IncludeFormsInComponents = true;
                    gumRfs.SetProperty(nameof(GumViewModel.IncludeFormsInComponents), true);
                    //viewModel.IncludeComponentToFormsAssociation = true;
                    gumRfs.SetProperty(nameof(GumViewModel.IncludeComponentToFormsAssociation), true);

                    await FormsControlAdder.SaveElements(assembly);
                    await FormsControlAdder.SaveBehaviors(assembly);

                    await HandleBuildMissingFonts();

                }
                GlueCommands.Self.GluxCommands.SaveProjectAndElements();

                await CodeGeneratorManager.Self.GenerateDerivedGueRuntimesAsync(forceReload:true);

                propertiesManager.IsReactingToProperyChanges = true;

                toolbarViewModel.HasGumProject = AppState.Self.GumProjectSave != null;
            }, Localization.Texts.GumProjectCreating);
        }
    }

    private void CreateGumControl()
    {
        control = new GumControl();
        GumPluginCommands.Self.GumControl = control;
        control.DataContext = viewModel;

        control.RebuildFontsClicked += async () => await HandleBuildMissingFonts();

        tab = this.CreateTab(control, Localization.Texts.GumProperties);
    }

    public async Task HandleBuildMissingFonts()
    {
        // --rebuildfonts "C:\Users\Victor\Documents\TestProject2\TestProject2\Content\GumProject\GumProject.gumx"
        var gumFileName = AppState.Self.GumProjectSave.FullFileName;

        string executable = GetGumExecutableLocation();

        if (string.IsNullOrEmpty(executable))
        {
            var message = 
                "Could not find file association for Gum files and could not find Gum relative to the FlatRedBall Editor." +
                "Associations need to be set before attempting to rebuild font files. Alternatively you can have Gum located at" +
                "one of the following locations: \n\n" +
                "As part of FRBDK (prebuilt):\n\t" +
                GumPrebuiltLocation + "\n\n" +
                "Built from source at default checkout locations: \n\t" +
                GumFromSourceLocation                
                ;

            GlueCommands.Self.DialogCommands.ShowMessageBox(message);
        }
        else
        {
            await TaskManager.Self.AddAsync(() =>
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo();

                // Even though this is "rebuildfonts" it actually doesn't "rebuild". It's just a "build"
                startInfo.Arguments = $@"--rebuildfonts ""{gumFileName}""";
                startInfo.FileName = executable;
                startInfo.UseShellExecute = false;

                GlueCommands.Self.PrintOutput($"{startInfo.FileName} {startInfo.Arguments}");

                var process = System.Diagnostics.Process.Start(startInfo);

                process.WaitForExit();
            },
            Localization.Texts.RefreshingFontCache);

            var gumRfs = GumProjectManager.Self.GetRfsForGumProject();
            if(gumRfs != null)
            {
                GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(gumRfs);
            }
        }
    }

    static FilePath GumPrebuiltLocation =>
        GlueState.Self.GlueExeDirectory + "../Gum/Data/Gum.exe";

    static FilePath GumFromSourceLocation =>
        GlueState.Self.GlueExeDirectory + "../../../../../../Gum/Gum/bin/Debug/Data/Gum.exe";

    public static string GetGumExecutableLocation()
    {
        var executable = WindowsFileAssociation.GetExecFileAssociatedToExtension("gumx");
        if (!File.Exists(executable))
            executable = "";

        // see if the prebuilt location exists:
        if (string.IsNullOrEmpty(executable))
        {
            FilePath prebuiltLocation = GumPrebuiltLocation;
            if (prebuiltLocation.Exists())
            {
                executable = prebuiltLocation.FullPath;
            }

        }
        try
        {
            //find corresponding gum if in ide
            if (string.IsNullOrEmpty(executable))
            {
                // \FlatRedBall\FRBDK\Glue\Glue\bin\x86\Debug to \Gum\Gum\bin\Debug\Data
                FilePath prebuiltLocation = GumFromSourceLocation;
                if (prebuiltLocation.Exists())
                {
                    executable = prebuiltLocation.FullPath;
                }

            }
        }
        catch (Exception)
        {
            // no biggie, this could be in a location that doesn't exist...
        }

        return executable;
    }

    private void HandleMakePluginRequiredYes()
    {
        var changed = GlueCommands.Self.GluxCommands.SetPluginRequirement(this, true);

        if (changed)
        {
            GlueCommands.Self.GluxCommands.SaveProjectAndElements();
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

        GumPluginCommands.Self.RefreshGumViewModel();

        StateCodeGenerator.Self.RefreshVariableNamesToSkipBasedOnGlueVersion();

        StandardsCodeGenerator.Self.RefreshVariableNamesToSkipForProperties();
    }

    private async void HandleGluxLoad()
    {
        base.AddToToolBar(gumToolbar, Localization.Texts.Tools);

        var gumRfs = GumProjectManager.Self.GetRfsForGumProject();

        toolbarViewModel.HasGumProject = gumRfs != null;

        if (gumRfs != null)
        {
            FileChangeManager.Self.RegisterAdditionalContentTypes();

            var behavior = GetBehavior(gumRfs);

            // todo: Removing a file should cause this to get called, but I don't think Gum lets us subscribe to that yet.
            await TaskManager.Self.AddAsync(async () =>
            {
                EmbeddedResourceManager.Self.UpdateCodeInProjectPresence(behavior);

                await CodeGeneratorManager.Self.GenerateDerivedGueRuntimesAsync();

                CodeGeneratorManager.Self.GenerateAllBehaviors();

                FileReferenceTracker.Self.RemoveUnreferencedFilesFromVsProject();

                UpdateMenuItemVisibility();

                // the UpdatecodeInProjectPresence may add new files, so save:
                GlueCommands.Self.ProjectCommands.SaveProjects();

            }, Localization.Texts.GumPluginGluxLoad);

            await UpdateGumParentProject();

            GumPluginCommands.Self.UpdateGumToGlueResolution();

            TaskManager.Self.Add(() => HandleBuildMissingFonts(), "Building missing Gum fonts");
        }
    }

    private async Task UpdateGumParentProject()
    {
        var gumProject = AppState.Self.GumProjectSave;

        if (gumProject != null)
        {
            var needsToSetRoot = string.IsNullOrWhiteSpace(gumProject.ParentProjectRoot);
            // Victor Chelaru Jan 8, 2022
            // This uses the content directory as the root. Should it use the entire game folder? Or just the 
            // content folder? I'm not sure.
            var expectedGumProjectParentRoot = new FilePath(GlueState.Self.ContentDirectory).RelativeTo(AppState.Self.GumProjectFolder);
            
            if (gumProject.ParentProjectRoot != expectedGumProjectParentRoot)
            {
                gumProject.ParentProjectRoot = expectedGumProjectParentRoot;

                await GumPluginCommands.Self.SaveGumxAsync();
            }
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

        if (gumToolbar != null)
        {
            base.RemoveFromToolbar(gumToolbar, Localization.Texts.Standard);
        }

        return true;
    }

    public bool IsAssetTypeInfoGum(AssetTypeInfo ati)
    {
        return ati != null &&
            AssetTypeInfoManager.Self.IsAssetTypeInfoGum(ati);
    }

    #endregion
}
