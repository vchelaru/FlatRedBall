using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Controls;
using System.IO;
using FlatRedBall.Glue.AutomatedGlue;
using Glue;
using FlatRedBall.IO;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.VSHelpers.Projects;
using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.ContentPipeline;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Performance.Measurement;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using System.Threading.Tasks;
using FlatRedBall.Glue.Data;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System.Reflection;
using GlueSaveClasses;
using GlueFormsCore.Controls;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.FactoryPlugin;
using Localization;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;

namespace FlatRedBall.Glue.IO
{
    public class ProjectLoader
    {
        #region Fields

        static ProjectLoader mSelf;
        private static string mLastLoadedFilename; //to prevent projects from loading/syncing twice
        private IProjectCommands _projectCommands;

        /// <summary>
        /// Started by Glue.exe's startup for the project it is about to open, taken by <see cref="LoadProject"/>.
        /// </summary>
        public EarlyProjectEvaluation EarlyEvaluation { get; set; } = new EarlyProjectEvaluation();

        // The .gluj next to the .csproj Glue.exe is about to open, read during startup; taken by LoadProject.
        (string CsprojFullPath, Task<GlueProjectSaveExtensions.PrefetchedGlueProject> Read)? earlyGlueProjectRead;

        /// <summary>
        /// Starts the .csproj evaluation and the read of the .gluj next to it, so both overlap the rest of
        /// startup. MSBUILD_EXE_PATH must already be final. <see cref="LoadProject"/> uses each only if it opens
        /// the same file and nothing it read has changed; otherwise it does that work itself as before.
        /// </summary>
        public void StartEarlyReads(string csprojFileName)
        {
            EarlyEvaluation.Start(csprojFileName);
            if (!string.IsNullOrEmpty(csprojFileName) && File.Exists(csprojFileName))
            {
                var gluj = new FilePath(FileManager.RemoveExtension(csprojFileName) + ".gluj");
                earlyGlueProjectRead = (Path.GetFullPath(csprojFileName), Task.Run(() => GlueProjectSaveExtensions.Prefetch(gluj)));
            }
        }

        /// <summary>
        /// Gives up whatever <see cref="StartEarlyReads"/> started, so a later load can't pick it up.
        /// </summary>
        public void DiscardEarlyReads()
        {
            EarlyEvaluation.TryTake(null);
            earlyGlueProjectRead = null;
        }

        internal Task<GlueProjectSaveExtensions.PrefetchedGlueProject> TakeEarlyGlueProjectRead(string csprojFileName)
        {
            var taken = earlyGlueProjectRead;
            earlyGlueProjectRead = null;
            if (taken == null ||
                !string.Equals(taken.Value.CsprojFullPath, Path.GetFullPath(csprojFileName), StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return taken.Value.Read;
        }

        #endregion

        #region Properties


        public string LastLoadedFilename
        {
            get;
            private set;
        }

        public static ProjectLoader Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new ProjectLoader();
                }
                return mSelf;
            }
        }

        #endregion

        /// <summary>
        /// What opening a project does beyond loading its model. <see cref="ProjectLoadScope.EditorSession"/>
        /// loads run all of it; a <see cref="ProjectLoadScope.ModelOnly"/> caller runs what it needs.
        /// </summary>
        public ProjectLoadOperations Operations { get; private set; }

        // Needed as we kill the singletons:
        public void Initialize(IProjectCommands projectCommands, ProjectLoadOperations operations)
        {
            _projectCommands = projectCommands;
            Operations = operations;
        }

        /// <summary>
        /// Opens the project the way the editor's File > Load Project does: <see cref="ProjectLoadScope.EditorSession"/>.
        /// </summary>
        public Task LoadProject(string projectFileName, InitializationWindowWpf initializationWindow = null) =>
            LoadProject(projectFileName, ProjectLoadScope.EditorSession, initializationWindow);

        /// <param name="scope">
        /// <see cref="ProjectLoadScope.EditorSession"/> runs the model load and then every
        /// <see cref="Operations"/> step the editor needs, interleaved in the order the editor has always run
        /// them. <see cref="ProjectLoadScope.ModelOnly"/> runs the same sequence without those steps.
        /// </param>
        public async Task LoadProject(string projectFileName, ProjectLoadScope scope, InitializationWindowWpf initializationWindow = null)
        {
            var isEditorSession = scope == ProjectLoadScope.EditorSession;
            TimeManager.Initialize();
            var topSection = Section.GetAndStartContextAndTime("All");
            ////////////////// EARLY OUT!!!!!!!!!
            if (!File.Exists(projectFileName))
            {
                DiscardEarlyReads();
                GlueGui.ShowException("Could not find the project " + projectFileName + "\n\nOpening Glue without a project.", "Error Loading Project", null);
                return;
            }
            //////////////////// End EARLY OUT////////////////////////////////

            // A FlatRedBall 2 game created by `dotnet new frb2-desktop` is two projects: MyGame.Common
            // holds Game1, the content and the engine reference, while MyGame.Desktop is a launcher that
            // reaches the engine through Common. Common is the one Glue edits, but Desktop is the
            // runnable one and just as natural a thing to pick, and picking it used to end at "could not
            // determine the project type" with nothing loaded. Resolving here rather than deeper in the
            // load keeps every later step - Load, RelativeDirectory, the solution lookup - agreeing on
            // one path.
            var frb2GameProject = Frb2ProjectDetector.FindFrb2GameProjectFor(projectFileName);
            if (frb2GameProject != null &&
                !string.Equals(FileManager.Standardize(frb2GameProject),
                    FileManager.Standardize(projectFileName), StringComparison.OrdinalIgnoreCase))
            {
                GlueCommands.Self.PrintOutput(
                    $"Opening {FileManager.RemovePath(frb2GameProject)} instead of " +
                    $"{FileManager.RemovePath(projectFileName)} - it is the FlatRedBall project of the two.");
                projectFileName = frb2GameProject;
            }

            // Null unless startup evaluated this same, unchanged file ahead of time; CreateProject then
            // evaluates it itself.
            var earlyEvaluatedProject = EarlyEvaluation.TryTake(projectFileName);
            var earlyGlueProject = TakeEarlyGlueProjectRead(projectFileName);

            TaskManager.Self.RecordTaskHistory($"--Received Load project Command {projectFileName}--");

            FileWatchManager.PerformFlushing = false;

            bool closeInitWindow = false;
            
            GlueCommands.Self.DoOnUiThread(() => closeInitWindow = PrepareInitializationWindow(ref initializationWindow));

            // close the project before turning off task processing...
            if (GlueState.Self.CurrentMainProject != null)
            {
                GlueCommands.Self.CloseGlueProject(shouldSave: false, isExiting: false, initWindow: initializationWindow);
            }

            // Vic says - do we really want to wait for this to finish?
            // If we do this, we can't run everything on a separate thread
            //await TaskManager.Self.WaitForAllTasksFinished();

            // turn off task processing while this is loading, so that no background tasks are running while plugins are starting up.
            // Do this *after* closing previous project, because closing previous project waits for all tasks to finish.
            TaskManager.Self.IsTaskProcessingEnabled = false;
            TaskManager.Self.RecordTaskHistory($"--Starting to load project {projectFileName}--");

            SetInitWindowText("Loading code project", initializationWindow);

            // Reading the .gluj and its element files doesn't need the .csproj, so it starts during startup
            // (StartEarlyReads) or, failing that, here alongside the evaluation. Which .gluj the project uses is
            // only known once the .csproj is loaded; the usual one sits next to it, and Load ignores the
            // prefetch if it turns out to be another. A failed or stale prefetch is dropped and Load reads the
            // files itself, reporting any error.
            var glujNextToCsproj = new FilePath(FileManager.RemoveExtension(projectFileName) + ".gluj");
            var prefetchedGlueProject = earlyGlueProject ??
                Task.Run(() => GlueProjectSaveExtensions.Prefetch(glujNextToCsproj));

            var result = ProjectCreator.CreateProject(projectFileName, earlyEvaluatedProject);
            GlueState.Self.CurrentMainProject = (VisualStudioProject)result.Project;

            bool shouldLoad = result.Project != null;

            if (shouldLoad)
            {

                GlueState.Self.CurrentMainProject.Load(projectFileName);

                var sln = GlueState.Self.CurrentSlnFileName;

                if(sln == null)
                {
                    GlueCommands.Self.PrintError("Could not find .sln file for project - this may cause file reference errors, and may need to be manually fixed");
                }


                SetInitWindowText("Finding Game class", initializationWindow);


                FileWatchManager.UpdateToProjectDirectory();
                FileManager.RelativeDirectory = FileManager.GetDirectory(projectFileName);
                // this will make other threads work properly:
                FileManager.DefaultRelativeDirectory = FileManager.RelativeDirectory;

                if (isEditorSession)
                {
                    GlueCommands.Self.DoOnUiThread( () => GlueCommands.Self.RefreshCommands.RefreshDirectoryTreeNodes());
                }

                #region Load the GlueProjectSave file if one exists

                FilePath glueProjectFile = GlueState.Self.GlueProjectFileName;


                // does the gluj exist? If so, use that since it's newer and should be used going forward:
                if(System.IO.File.Exists(glueProjectFile.RemoveExtension() + ".gluj"))
                {
                    glueProjectFile = glueProjectFile.RemoveExtension() + ".gluj";
                }


                if (!glueProjectFile.Exists())
                {
                    ProjectManager.GlueProjectSave = new GlueProjectSave();

                    ProjectManager.GlueProjectSave.FileVersion =
                        GlueProjectSave.GetFileVersionForNewProject(GlueState.Self.EngineDllSyntaxVersion);

                    // After assigning the file version the glue project may change version, so try to update it:
                    glueProjectFile = GlueState.Self.GlueProjectFileName;

                    GlueCommands.Self.PrintOutput($"Trying to load {glueProjectFile}, but could not find it, so " +
                        $"creating a new Glue Project file");

                    // temporary - eventually this will just be done in the .glux itself, or by the plugin 
                    // but for now we do it here because we only want to do it on new projects
                    Plugins.EmbeddedPlugins.CameraPlugin.CameraMainPlugin.CreateGlueProjectSettingsFor(ProjectManager.GlueProjectSave);


                    ProjectManager.FindGameClass();
                    if (isEditorSession)
                    {
                        GluxCommands.Self.SaveProjectAndElementsImmediately();
                    }

                    // FRB2 generates no code, so the one line telling the game to load Glue's project
                    // instead of its template GameScreen has to be written into Game1.cs by hand. This is
                    // the only safe point to do that automatically - the .gluj/.glux did not exist a
                    // moment ago, so Game1.cs is still whatever the template shipped.
                    if (isEditorSession && GlueState.Self.CurrentMainProject is Frb2Project frb2Project)
                    {
                        var game1FilePath = frb2Project.Directory + "Game1.cs";
                        var glueProjectContentRelativePath = frb2Project.GlueProjectSubdirectory +
                            FileManager.RemovePath(glueProjectFile.FullPath);

                        Frb2Game1InitializeUpdater.TryUpdateGame1ToLoadGlueProject(
                            game1FilePath, glueProjectContentRelativePath);
                    }

                }

                var section = Section.GetAndStartContextAndTime("ProjectLoader.LoadProject");

                PerformGluxLoad(projectFileName, glueProjectFile.FullPath, initializationWindow,
                    TakePrefetchIgnoringErrors(prefetchedGlueProject), isEditorSession);

                Section.EndContextAndTime();

                #endregion

                if (isEditorSession)
                {
                    SetInitWindowText("Cleaning extra Screens and Entities", initializationWindow);

                    Operations.RefreshUnreferencedFiles();

                    TaskManager.Self.OnUiThread(() => MainGlueWindow.Self.Text = Texts.FrbEditor + " - " + projectFileName);

                    GlueCommands.Self.ProjectCommands.SaveProjects();
                }

                FileWatchManager.PerformFlushing = true;
                FileWatchManager.FlushAndClearIgnores();
            }
            if (closeInitWindow)
            {
                GlueCommands.Self.DoOnUiThread(() => initializationWindow.Close());
            }


            TaskManager.Self.IsTaskProcessingEnabled = true;

            // If we ever want to make things go faster, turn this back on and let's see what's going on.
            //topSection.Save("Sections.xml");
        }

        public void GetCsprojToLoad(out string csprojToLoad)
        {
            csprojToLoad = CommandLineManager.Self.ProjectToLoad;
            var settingsSave = GlueState.Self.GlueSettingsSave;

            bool shouldTryLoadingFromSettings = string.IsNullOrEmpty(csprojToLoad) &&
                (Control.ModifierKeys & Keys.Shift) == 0;

            if (shouldTryLoadingFromSettings)
            {
                string glueExeFileName = GetGlueExeLocation();

                var foundGlueExeProjectLocationPair = settingsSave.GlueLocationSpecificLastProjectFiles
                    .FirstOrDefault(item => item.GlueFileName == glueExeFileName);

                if (foundGlueExeProjectLocationPair != null)
                {
                    csprojToLoad = foundGlueExeProjectLocationPair.GameProjectFileName;
                }
                else
                {
                    csprojToLoad = settingsSave.LastProjectFile;
                }
            }
        }

        public static string GetGlueExeLocation()
        {
            return FileManager.Standardize(Assembly.GetAssembly(typeof(MainGlueWindow)).Location.ToLowerInvariant());
        }

        internal static GlueProjectSaveExtensions.PrefetchedGlueProject TakePrefetchIgnoringErrors(
            Task<GlueProjectSaveExtensions.PrefetchedGlueProject> prefetch)
        {
            GlueProjectSaveExtensions.PrefetchedGlueProject prefetched;
            try
            {
                prefetched = prefetch.GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                // Load reads the files again without it and reports whatever went wrong.
                return null;
            }
            // A prefetch started at startup is seconds old by now; a file it read may have changed since.
            return prefetched?.IsUnchangedOnDisk() == true ? prefetched : null;
        }

        private void PerformGluxLoad(string projectFileName, string glueProjectFile, InitializationWindowWpf initializationWindow,
            GlueProjectSaveExtensions.PrefetchedGlueProject prefetchedGlueProject, bool isEditorSession)
        {
            SetInitWindowText("Loading FlatRedBall Project", initializationWindow);


            bool succeeded = true;

            succeeded = DeserializeGlueProjectInternal(projectFileName, glueProjectFile, initializationWindow, prefetchedGlueProject);

            if (succeeded)
            {
                // This seems to take some time (like 1 second). Can we possibly
                // make it faster by having it chek Game1.cs first? Why is this so slow?
                ProjectManager.FindGameClass();

                AvailableAssetTypes.Self.AdditionalExtensionsToTreatAsAssets.Clear();

                IdentifyAdditionalAssetTypes();

                SetInitWindowText("Finding and fixing .glux errors", initializationWindow);

                ChangedObjects changedObjects = new ChangedObjects();

                ProjectManager.GlueProjectSave.FixErrors(true, changedObjects);
                ProjectManager.GlueProjectSave.RemoveInvalidStatesFromNamedObjects(true);

                SetUnsetValues();

                ProjectManager.LoadOrCreateProjectSpecificSettings(FileManager.GetDirectory(projectFileName));

                SetInitWindowText("Notifying plugins of project...", initializationWindow);

                Section.GetAndStartContextAndTime("PluginManager Init");

                PluginManager.Initialize(false);

                // The project specific settings are needed before the plugins do their thing...
                PluginManager.ReactToLoadedGluxEarly(ProjectManager.GlueProjectSave);

                // and after that's done we can validate that the build tools are there
                // todo - maybe do this on the GlueSettingsSave?
                //BuildToolAssociationManager.Self.ProjectSpecificBuildTools.ValidateBuildTools(FileManager.GetDirectory(projectFileName));

                ProjectManager.GlueProjectSave.UpdateIfTranslationIsUsed();

                Section.GetAndStartContextAndTime("Add items");


                //AddEmptyTreeItems();


                // This has to be done before the tree nodes are created.  The reason is because a user
                // may create a ReferencedFileSave using a source type, but not check in the built file.
                // Glue depends on the built file being there, so we gotta build to make sure that file gets
                // generated.
                // Update on May 4, 2011:  This should be done AFTER BuildAllOutOfDateFiles because Refreshing
                // source file cache requires looking at all referenced files, and this requires the files existing
                // so that dependencies can be tracked.
                // Update May 4, 2011 Part 2:  The SourceFileCache is used when building files.  So instead, the refreshing
                // of the source file cache will also build a file if it encounters a missing file.  This should greatly reduce
                // popup count.
                SetInitWindowText("Refreshing Source File Cache...", initializationWindow);
                RefreshSourceFileCache();
                GlueState.Self.TiledCache.RefreshCache();

                if (isEditorSession)
                {
                    SetInitWindowText("Building out-of-date external files...", initializationWindow);
                    Operations.BuildAllOutOfDateFiles();
                }
                Section.EndContextAndTime();

                if (isEditorSession)
                {
                    // This task is going to run after the load finishes, so we will not pass the ChangedObjects
                    _projectCommands.CallUpdateFileMembershipsOnAllFiles();
                }

                foreach (var element in ObjectFinder.Self.GlueProject.Screens)
                {
                    element.UpdateCustomProperties();
                }
                foreach (var entity in ObjectFinder.Self.GlueProject.Entities)
                {
                    entity.UpdateCustomProperties();
                }

                if (isEditorSession)
                {
                    Operations.CheckForMissingCustomCodeFiles();
                }

                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("PrepareSyncedProjects");

                PrepareSyncedProjects(projectFileName, initializationWindow);

                mLastLoadedFilename = projectFileName;
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("MakeGeneratedItemsNested");

                if (isEditorSession)
                {
                    // This should happen after loading synced projects
                    SetInitWindowText("Nesting generated code files in .csproj", initializationWindow);
                    GlueCommands.Self.ProjectCommands.MakeGeneratedCodeItemsNested();

                    SetInitWindowText("Updating global content tree nodes", initializationWindow);
                    GlueCommands.Self.RefreshCommands.RefreshGlobalContent();

                    Operations.GeneratePerformanceTypes();
                }
                Section.EndContextAndTime();


                SetInitWindowText("Notifying Plugins of startup", initializationWindow);


                PluginManager.ReactToLoadedGlux(ProjectManager.GlueProjectSave, glueProjectFile, (newString) => SetInitWindowText(newString, initializationWindow));
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("GenerateAllCode");
                SetInitWindowText("Generating all code", initializationWindow);

                // Fix before doing any generation
                GlueState.Self.CurrentGlueProject.FixAllTypesPostLoad();
                ReferencedFileSaveCodeGenerator.GenerateCaseSensitive =
                    GlueState.Self.CurrentGlueProject.FileVersion >= (int)SaveClasses.GlueProjectSave.GluxVersions.CaseSensitiveLoading;

                if (isEditorSession)
                {
                    Operations.SaveFixedUpElements(changedObjects);
                    Operations.GenerateAllCode();
                    Operations.RemoveOrphanedGeneratedCsprojEntries();
                }
                Section.EndContextAndTime();
            }
        }


        /// <summary>
        /// Sets any values that should not be left uninitialized.
        /// </summary>
        private void SetUnsetValues() // intentionally left blank:
        {
            // This is going to give us the .sln directory,
            // but that's okay, that way it catches all external files.
            string directoryToSet = ProjectManager.ProjectRootDirectory;
            if(!string.IsNullOrEmpty(directoryToSet))
            {

                SetExternallyBuiltFileIfHigherThanCurrent(directoryToSet, false);
            }
        }


        public void SetExternallyBuiltFileIfHigherThanCurrent(string directoryOfFile, bool performSave)
        {
            if (directoryOfFile == null)
            {
                throw new ArgumentNullException(nameof(directoryOfFile));
            }
            string currentExternalDirectory = null;

            if (!string.IsNullOrEmpty(ProjectManager.GlueProjectSave.ExternallyBuiltFileDirectory))
            {
                currentExternalDirectory = GlueCommands.Self.GetAbsoluteFileName(ProjectManager.GlueProjectSave.ExternallyBuiltFileDirectory, true);
            }

            if (string.IsNullOrEmpty(currentExternalDirectory) ||
                !FileManager.IsRelativeTo(directoryOfFile, currentExternalDirectory))
            {

                //FileWatchManager.SetExternallyBuiltContentDirectory(directoryOfFile);
                //      
                string newExternalDirectoryRelativeToContent = ProjectManager.MakeRelativeContent(directoryOfFile);

                ProjectManager.GlueProjectSave.ExternallyBuiltFileDirectory = newExternalDirectoryRelativeToContent;

                if (performSave)
                {
                    GluxCommands.Self.SaveProjectAndElements();
                }
            }
        }


        private static void IdentifyAdditionalAssetTypes()
        {
            List<ReferencedFileSave> rfsList = ProjectManager.GlueProjectSave.GetAllReferencedFiles();

            foreach (ReferencedFileSave nos in rfsList)
            {
                string extension = FileManager.GetExtension(nos.Name);

                if (AvailableAssetTypes.Self.GetAssetTypeFromExtension(extension) == null &&
                    !AvailableAssetTypes.Self.AdditionalExtensionsToTreatAsAssets.Contains(extension))
                {
                    AvailableAssetTypes.Self.AdditionalExtensionsToTreatAsAssets.Add(extension);
                }
            }
        }

        private static bool PrepareInitializationWindow(ref InitializationWindowWpf initializationWindow)
        {
            bool closeInitWindow = false;

            // Constructing the window (not just showing it) needs an STA thread and live WPF services, so
            // the ShowGui check has to cover the constructor too - otherwise a headless host (tests) throws
            // "The calling thread must be STA" before load even starts. SetInitWindowText is already
            // null-safe, so leaving this null just means no progress text.
            if (initializationWindow == null && GlueGui.ShowGui)
            {
                closeInitWindow = true;

                initializationWindow = new InitializationWindowWpf();
                initializationWindow.Show();
            }
            return closeInitWindow;
        }

        private bool DeserializeGlueProjectInternal(string projectFileName, string glueProjectFile, InitializationWindowWpf initializationWindow,
            GlueProjectSaveExtensions.PrefetchedGlueProject prefetchedGlueProject)
        {
            bool succeeded = true;
            try
            {
                ProjectManager.GlueProjectSave = GlueProjectSaveExtensions.Load(glueProjectFile, prefetchedGlueProject);

                string errors;
                ProjectManager.GlueProjectSave.PostLoadInitialize(out errors);

                if (errors != null)
                {
                    GlueGui.ShowMessageBox(errors);
                }
            }
            catch (Exception e)
            {
                // DialogService.ShowChoice returns default(DialogResult) (None) if the dialog is closed
                // without a button click (e.g. Escape), which matches the explicit None case below.
                var choice = DialogService.ShowChoice("There was an error loading the .glux file.  What would you like to do?",
                    ("Nothing - Glue will abort loading the project.", DialogResult.None),
                    ("See the Exception", DialogResult.OK),
                    ("Try loading again", DialogResult.Retry),
                    ("Test for conflicts", DialogResult.Yes));

                initializationWindow.Close();

                switch (choice)
                {
                    case DialogResult.None:
                        // Do nothing;

                        break;
                    case DialogResult.OK:
                        DialogService.ShowMessage(e.ToString());
                        break;
                    case DialogResult.Retry:
                        _=LoadProject(projectFileName);
                        break;
                    case DialogResult.Yes:
                        string text = FileManager.FromFileText(glueProjectFile);

                        if (text.Contains("<<<"))
                        {
                            DialogService.ShowMessage("There are conflicts in your GLUX file.  You will need to use a merging " +
                                "tool or text editor to resolve these conflicts.");
                        }
                        else
                        {
                            DialogService.ShowMessage("No Subversion conflicts found in your GLUX.");
                        }
                        break;
                }
                succeeded = false;
            }
            return succeeded;
        }

        public void SetInitWindowText(string subtext, InitializationWindowWpf initializationWindow)
        {
            if (initializationWindow != null)
            {
                initializationWindow.SubMessage = subtext;
            }
        }

        private void RefreshSourceFileCache()
        {
            List<string> errors = new List<string>();


            // parallelizing this seems to screw things up when a plugin tries to do something on the UI thread
            //Parallel.ForEach(ProjectManager.GlueProjectSave.Screens, (screen) =>
            foreach (ScreenSave screen in ProjectManager.GlueProjectSave.Screens)
            {
                foreach (ReferencedFileSave rfs in screen.ReferencedFiles)
                {
                    string error;
                    rfs.RefreshSourceFileCache(true, out error);

                    if (!string.IsNullOrEmpty(error))
                    {
                        lock (errors)
                        {
                            errors.Add(error + " in " + screen.ToString());
                        }
                    }
                }
            }
            //);


            //Parallel.ForEach(ProjectManager.GlueProjectSave.Entities, (entitySave) =>
            foreach (EntitySave entitySave in ProjectManager.GlueProjectSave.Entities)
            {
                foreach (ReferencedFileSave rfs in entitySave.ReferencedFiles)
                {
                    string error;
                    rfs.RefreshSourceFileCache(true, out error);
                    if (!string.IsNullOrEmpty(error))
                    {
                        lock (errors)
                        {
                            errors.Add(error + " in " + entitySave.ToString());
                        }
                    }
                }
            }
            //);

            //Parallel.ForEach(ProjectManager.GlueProjectSave.GlobalFiles, (rfs) =>
            foreach (ReferencedFileSave rfs in ProjectManager.GlueProjectSave.GlobalFiles)
            {
                string error;
                rfs.RefreshSourceFileCache(true, out error);
                if (!string.IsNullOrEmpty(error))
                {
                    lock (errors)
                    {
                        errors.Add(error + " in Global Content Files");
                    }
                }
            }
            //);


            foreach (var error in errors)
            {
                // popups suck! Just output it:
                //ErrorReporter.ReportError("", error, true);
                GlueCommands.Self.PrintError(error);
            }

        }

        private void PrepareSyncedProjects(string projectFileName, InitializationWindowWpf initializationWindow)
        {
            SetInitWindowText("Loading synced projects Entities", initializationWindow);
            for (int i = ProjectManager.GlueProjectSave.SyncedProjects.Count - 1; i > -1; i--)
            {
                string projectName;

                if (FileManager.IsRelative(ProjectManager.GlueProjectSave.SyncedProjects[i]))
                {
                    projectName = FileManager.RelativeDirectory + ProjectManager.GlueProjectSave.SyncedProjects[i];

                    projectName = FileManager.RemoveDotDotSlash(projectName);

                }
                else
                {
                    projectName = ProjectManager.GlueProjectSave.SyncedProjects[i];

                }

                bool succeeded = AddSyncedProjectToProjectManager(projectName);

                if (!succeeded)
                {
                    ProjectManager.GlueProjectSave.SyncedProjects.RemoveAt(i);
                }
            }

            if (!projectFileName.Equals(ProjectLoader.Self.LastLoadedFilename))
            {
                lock (ProjectManager.SyncedProjects)
                {
                    foreach (ProjectBase syncedProject in ProjectManager.SyncedProjects)
                    {
                        try
                        {
                            ProjectSyncer.SyncProjects(GlueState.Self.CurrentMainProject, syncedProject, false);
                        }
                        catch (Exception e)
                        {
                            DialogService.ShowMessage("Error syncing project:\n\n" + syncedProject.Name +
                                "\n\nThe main project will still function properly - Glue just won't be able " +
                                "to maintain the synced project.  Error details:\n\n" + e.ToString());
                        }
                    }
                }
            }
        }


        public static bool AddSyncedProjectToProjectManager(string absoluteFileName)
        {
            bool succeeded = false;


            if(!File.Exists(absoluteFileName))
            {
                DialogService.ShowMessage("Could not find the project" + absoluteFileName + ", removing project from synched project list.");
            }
            else if (absoluteFileName == GlueState.Self.CurrentMainProject.FullFileName)
            {
                // Victor Chelaru
                // January 1, 2013
                // One user had the
                // synced and main project
                // as the same.  This screws
                // up Glue pretty badly.  We need
                // to check for this and not allow
                // it.
                DialogService.ShowMessage("A synced project is using the same file as the main project.  This is not allowed.  Glue will remove this synced project the synced project list.");
            }
            else
            {
                try
                {
                    ProjectBase vsp = ProjectCreator.CreateProject(absoluteFileName).Project;
                    vsp.OriginalProjectBaseIfSynced = GlueState.Self.CurrentMainProject;

                    vsp.Load(absoluteFileName);

                    if (vsp.SaveAsRelativeSyncedProject == false && vsp.SaveAsAbsoluteSyncedProject == false)
                    {
                        vsp.SaveAsRelativeSyncedProject = true;
                        vsp.SaveAsAbsoluteSyncedProject = false;
                    }

                    if (String.Equals(FileManager.GetDirectory(absoluteFileName),
                            FileManager.GetDirectory(GlueState.Self.CurrentMainProject.FullFileName.FullPath),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        vsp.SaveAsRelativeSyncedProject = false;
                        vsp.SaveAsAbsoluteSyncedProject = false;
                    }

                    lock (ProjectManager.SyncedProjects)
                    {
                        GlueState.Self.SyncedProjects.Add(vsp);
                        PluginManager.ReactToSyncedProjectLoad(vsp);
                    }
                    succeeded = true;
                }
                catch (Exception e)
                {
                    GlueCommands.Self.PrintError($"Error loading sycned project. Glue will remove this synced project: {absoluteFileName}:\n{e.ToString()}");
                }

            }
            return succeeded;
        }
    }
}
