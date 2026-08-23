using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Gum.DataTypes;
using Gum.Managers;
using GumPlugin.CodeGeneration;
using GumPlugin.Controls;
using GumPlugin.Managers;
using GumPlugin.ViewModels;
using GlueFormsCore.ViewModels;
using HQ.Util.Unmanaged;

namespace GumPlugin;

[Export(typeof(PluginBase))]
public class MainGumPlugin : PluginBase
{
    #region Fields

    GumControl control;
    GumViewModel viewModel;
    GumToolbarViewModel toolbarViewModel;
    GumxPropertiesManager propertiesManager;
    NewGumProjectCreationLogic newGumProjectCreationLogic;
    ToolStripMenuItem addGumProjectMenuItem;

    GlobalContentCodeGenerator globalContentCodeGenerator;
    GumToolbar gumToolbar;

    bool raiseViewModelEvents = true;

    PluginTab tab;

    #endregion

    #region Properties

    public override string FriendlyName => "Gum Plugin"; 

    public override Version Version => new Version(2, 4, 2); 

    #endregion

    #region Initialize/Assing Events

    public override void StartUp()
    {
        propertiesManager = new GumxPropertiesManager();

        AssetTypeInfoManager.Self.AddCommonAtis();

        newGumProjectCreationLogic = new NewGumProjectCreationLogic(propertiesManager);

        addGumProjectMenuItem = this.AddMenuItemTo(
            "New Gum Project",
            Localization.MenuIds.ProjectNewGumId,
            async () =>
            {
                await newGumProjectCreationLogic.AskToCreateGumProject();

                UpdateAfterProjectCreation();
            },
            Localization.MenuIds.ContentId);
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

        var textCodeGenerator = new TextCodeGenerator();
        var containerCodeGenerator = new ContainerCodeGenerator(GlueState.Self);
        var nineSliceCodeGenerator = new NineSliceCodeGenerator(GlueState.Self);
        var spriteCodeGenerator = new SpriteCodeGenerator(GlueState.Self);
        var polygonCodeGenerator = new PolygonCodeGenerator(GlueState.Self);

        StandardsCodeGenerator.Self.Initialize(
            spriteCodeGenerator,
            textCodeGenerator,
            containerCodeGenerator,
            nineSliceCodeGenerator,
            polygonCodeGenerator);
        StateCodeGenerator.Self.Initialize(textCodeGenerator, containerCodeGenerator, nineSliceCodeGenerator);
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

        this.FillDeleteOptions += HandleFillDeleteOptions;
        this.ReactToElementRemoved += HandleElementRemoved;

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

    // This function should not be removed, it is referenced by string name by the Quick Actions MainView.
    public Task AskToCreateGumProject() =>
        newGumProjectCreationLogic.AskToCreateGumProject();

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
        toolbarViewModel = new GumToolbarViewModel(newGumProjectCreationLogic);

        gumToolbar = new GumToolbar();
        gumToolbar.DataContext = toolbarViewModel;
        gumToolbar.GumButtonClicked += (_, _) => toolbarViewModel.OnToolbarClicked();
    }

    public bool HasGum() => AppState.Self.GumProjectSave != null;

    private void HandleFileRemoved(GlueElement container, ReferencedFileSave file)
    {
        if (file.Name.EndsWith(".gumx"))
        {
            // gum project was removed, so mark it as removed:
            AppState.Self.GumProjectSave = null;
        }

        toolbarViewModel.HasGumProject =
            AppState.Self.GumProjectSave != null;
    }

    private List<AssetTypeInfo> HandleGetAvailableAssetTypes(ReferencedFileSave referencedFileSave)
    {
        var element = CodeGeneratorManager.GetElementFrom(referencedFileSave);

        if (element != null)
        {

            var foundItem = AssetTypeInfoManager.Self.AssetTypesForThisProject
                .Where(item => item.QualifiedRuntimeTypeName.QualifiedType ==
                    GueDerivingClassCodeGenerator.Self.GetQualifiedRuntimeTypeFor(element, prefixGlobal: false));

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

        }, String.Format(Localization.Texts.GumPluginHandleNewFrbScreen, newFrbScreen));
    }


    /// <summary>
    /// Tag for the "also delete the Gum screen" option, so the answer can be read back in
    /// <see cref="HandleElementRemoved"/> without matching on display text.
    /// </summary>
    const string DeleteGumScreenOptionTag = "GumPlugin.DeleteGumScreen";

    /// <summary>
    /// Adds the Gum screen question as a checkbox on Glue's delete dialog. This used to be a separate
    /// MessageBox raised part-way through the delete, which is the second popup GitHub issue #429 is about.
    /// </summary>
    private void HandleFillDeleteOptions(GlueElement element, DeleteOptionsViewModel viewModel)
    {
        var gumScreen = GetGumScreenFor(element);

        if (gumScreen == null)
        {
            return;
        }

        var option = viewModel.AddOption(
            String.Format(Localization.Texts.GumConfirmDeleteScreen, gumScreen.Name, element),
            DeleteGumScreenOptionTag);

        AddFile(CodeGeneratorManager.Self.CustomRuntimeCodeLocationFor(gumScreen));
        AddFile(CodeGeneratorManager.Self.GeneratedRuntimeCodeLocationFor(gumScreen));

        // If there are forms screens, remove those too:
        var formsCustomFilePath = CodeGeneratorManager.Self.CustomFormsCodeLocationFor(gumScreen);
        if (formsCustomFilePath.Exists())
        {
            AddFile(formsCustomFilePath);
        }

        var formsGeneratedFilePath = CodeGeneratorManager.Self.GeneratedFormsCodeLocationFor(gumScreen);
        if (formsGeneratedFilePath.Exists())
        {
            AddFile(formsGeneratedFilePath);
        }

        void AddFile(FilePath filePath) =>
            option.AdditionalFilesToRemove.Add(DeletionPlanner.ToCanonicalPath(filePath.FullPath));
    }

    private void HandleElementRemoved(GlueElement element, DeleteOptionsViewModel viewModel)
    {
        if (viewModel?.IsOptionChecked(DeleteGumScreenOptionTag) != true)
        {
            return;
        }

        var gumScreen = GetGumScreenFor(element);

        if (gumScreen != null)
        {
            // don't await this, the delete has already been approved and the files are already listed
            _ = GumPluginCommands.Self.RemoveScreen(gumScreen);
        }
    }

    private static Gum.DataTypes.ScreenSave GetGumScreenFor(GlueElement element)
    {
        if (AppState.Self.GumProjectSave == null || !(element is FlatRedBall.Glue.SaveClasses.ScreenSave glueScreen))
        {
            return null;
        }

        var gumScreenName = GumPluginCommands.Self.GetExpectedGumScreenNameFor(glueScreen);

        return AppState.Self.GumProjectSave.Screens.FirstOrDefault(item => item.Name == gumScreenName);
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
                if (gumRfs != null)
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
                    var behavior = EmbeddedResourceManager.Self.GetBehavior(gumRfs);
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

    public async Task CreateGumProjectWithForms(bool askToOverwrite)
    {
        await newGumProjectCreationLogic.CreateGumProjectInternal(
            shouldAlsoAddForms: true,
            askToOverwrite: askToOverwrite);

        UpdateAfterProjectCreation();

    }

    public async Task CreateGumProjectNoForms(bool askToOverwrite)
    {
        await newGumProjectCreationLogic.CreateGumProjectInternal(shouldAlsoAddForms: false, askToOverwrite: askToOverwrite);

        UpdateAfterProjectCreation();
    }

    private void UpdateAfterProjectCreation()
    {
        if (control == null)
        {
            GlueCommands.Self.DoOnUiThread(CreateGumControl);
        }
        // show the tab for the new file:
        tab.Focus();
        toolbarViewModel.HasGumProject = AppState.Self.GumProjectSave != null;
    }

    private void CreateGumControl()
    {
        control = new GumControl();
        GumPluginCommands.Self.GumControl = control;
        control.DataContext = viewModel;

        control.RebuildFontsClicked += async () => await HandleBuildMissingFonts();

        tab = this.CreateTab(control, Localization.Texts.GumProperties);
    }

    public static async Task HandleBuildMissingFonts()
    {
        // --rebuildfonts "C:\Users\Victor\Documents\TestProject2\TestProject2\Content\GumProject\GumProject.gumx"
        var gumFileName = AppState.Self.GumProjectSave.FullFileName;

        //string executable = GetGumExecutableLocation();

        await TaskManager.Self.AddAsync(async () =>
        {
            {
                //var startInfo = new System.Diagnostics.ProcessStartInfo();

                //// Even though this is "rebuildfonts" it actually doesn't "rebuild". It's just a "build"
                //startInfo.Arguments = $@"--rebuildfonts ""{gumFileName}""";
                //startInfo.FileName = executable;
                //startInfo.UseShellExecute = false;

                //GlueCommands.Self.PrintOutput($"{startInfo.FileName} {startInfo.Arguments}");

                //var gumStart = DateTime.Now;
                //var process = System.Diagnostics.Process.Start(startInfo);
                //await process.WaitForExitAsync();
                //var gumEnd = DateTime.Now;

                //var exitCode = process.ExitCode;

                //if(exitCode != 0)
                //{
                //    if(exitCode == 2)
                //    {
                //        GlueCommands.Self.PrintError("Gum has exited unexpected. Do you have the XNA runtime installed?");
                //    }
                //    else 
                //    {
                //        // Unknown error:
                //        GlueCommands.Self.PrintError("Gum has exited with an unknown error so fonts were not generated.");
                //    }
                //}
                //GlueCommands.Self.PrintOutput("Old font building: " + (gumEnd - gumStart).TotalSeconds + " seconds");

            }



            {
                var startInfo = new System.Diagnostics.ProcessStartInfo();

                // Even though this is "rebuildfonts" it actually doesn't "rebuild". It's just a "build"
                startInfo.Arguments = $@"""{gumFileName}""";
                startInfo.FileName =
                    "Plugins/GumPlugin/Tools/GumProjectFontGenerator/GumProjectFontGenerator.exe";
                //"C:\\Users\\Owner\\Documents\\GitHub\\Gum\\GumProjectFontGenerator\\bin\\Debug\\net8.0\\GumProjectFontGenerator.exe";
                startInfo.UseShellExecute = false;

                // use the new font building tool:
                var newToolStart = DateTime.Now;
                var process = new Process();
                process.StartInfo = startInfo;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                await process.WaitForExitAsync();

                var newToolExit = DateTime.Now;
                var exitCode = process.ExitCode;

                if (exitCode != 0)
                {
                    GlueCommands.Self.PrintOutput($"{startInfo.FileName} {startInfo.Arguments}");
                    var error = process.StandardError.ReadToEnd();
                    // Unknown error:
                    GlueCommands.Self.PrintError($"GumProjectFontGenerator has exited with an unknown error {exitCode} so fonts were not generated.\n{error}");
                }
            }


        },
        "Rebuilding Fonts");

        var gumRfs = GumProjectManager.Self.GetRfsForGumProject();
        if (gumRfs != null)
        {
            GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(gumRfs);
        }
    }

    static FilePath GumPrebuiltLocation =>
        GlueState.Self.GlueExeDirectory + "../Gum/Gum.exe";

    static FilePath GumFromSourceLocation =>
        GlueState.Self.GlueExeDirectory + "../../../../../../Gum/Gum/bin/Debug/Gum.exe";

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

        StateCodeGenerator.Self.RefreshVariablesToSkipForStates();
        StateCodeGenerator.Self.RefreshVariableNamesToSkipBasedOnGlueVersion();


        StandardsCodeGenerator.Self.RefreshVariableNamesToSkipForProperties();
    }

    private async void HandleGluxLoad()
    {
        base.AddToToolBar(gumToolbar, "Tools");

        var gumRfs = GumProjectManager.Self.GetRfsForGumProject();

        toolbarViewModel.HasGumProject = gumRfs != null;

        if (gumRfs != null)
        {
            FileChangeManager.Self.RegisterAdditionalContentTypes();

            var behavior = EmbeddedResourceManager.Self.GetBehavior(gumRfs);

            // todo: Removing a file should cause this to get called, but I don't think Gum lets us subscribe to that yet.
            await TaskManager.Self.AddAsync(async () =>
            {
                EmbeddedResourceManager.Self.UpdateCodeInProjectPresence(behavior);

                await CodeGeneratorManager.Self.GenerateDerivedGueRuntimesAsync();

                await CodeGeneratorManager.Self.GenerateAllBehaviors();

                FileReferenceTracker.Self.RemoveUnreferencedFilesFromVsProject();

                UpdateMenuItemVisibility();

                // the UpdatecodeInProjectPresence may add new files, so save:
                GlueCommands.Self.ProjectCommands.SaveProjects();

            }, "Gum Plugin reacting to Glue Project Load");

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
        // Was (MethodInvoker) - changed to (Action) because IMainGlueWindow.Invoke only declares an Action
        // overload. Same method, zero behavior change.
        Glue.MainGlueWindow.Self.Invoke((Action)RemoveAllMenuItems);

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
