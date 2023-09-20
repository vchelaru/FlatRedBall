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

namespace FlatRedBall.Glue.IO
{
    public class ProjectLoader
    {
        #region Fields

        static ProjectLoader mSelf;
        private static string mLastLoadedFilename; //to prevent projects from loading/syncing twice

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

        
        public async Task LoadProject(string projectFileName, InitializationWindowWpf initializationWindow = null)
        {
            TimeManager.Initialize();
            var topSection = Section.GetAndStartContextAndTime("All");
            ////////////////// EARLY OUT!!!!!!!!!
            if (!File.Exists(projectFileName))
            {
                GlueGui.ShowException("Could not find the project " + projectFileName + "\n\nOpening Glue without a project.", "Error Loading Project", null);
                return;
            }
            //////////////////// End EARLY OUT////////////////////////////////

            TaskManager.Self.RecordTaskHistory($"--Received Load project Command {projectFileName}--");

            FileWatchManager.PerformFlushing = false;

            bool closeInitWindow = false;
            
            GlueCommands.Self.DoOnUiThread(() => closeInitWindow = PrepareInitializationWindow(ref initializationWindow));

            // close the project before turning off task processing...
            if (ProjectManager.ProjectBase != null)
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

            var result = ProjectCreator.CreateProject(projectFileName);
            ProjectManager.ProjectBase = (VisualStudioProject)result.Project;

            bool shouldLoad = result.Project != null;
            if (shouldLoad && result.ShouldTryToLoadProject)
            {
                shouldLoad = DetermineIfShouldLoad(projectFileName);
            }

            if (shouldLoad)
            {

                ProjectManager.ProjectBase.Load(projectFileName);

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

                GlueCommands.Self.DoOnUiThread( () => GlueCommands.Self.RefreshCommands.RefreshDirectoryTreeNodes());

                #region Load the GlueProjectSave file if one exists

                FilePath glueProjectFile = GlueState.Self.GlueProjectFileName;


                // does the gluj exist? If so, use that since it's newer and should be used going forward:
                if(System.IO.File.Exists(glueProjectFile.RemoveExtension() + ".gluj"))
                {
                    glueProjectFile = glueProjectFile.RemoveExtension() + ".gluj";
                }


                bool shouldSaveGlux = false;

                if (!glueProjectFile.Exists())
                {
                    if(!TaskManager.Self.IsInTask())
                    {
                        int m = 3;
                    }
                    ProjectManager.GlueProjectSave = new GlueProjectSave();

                    ProjectManager.GlueProjectSave.FileVersion = GlueProjectSave.LatestVersion;

                    // After assigning the file version the glue project may change version, so try to update it:
                    glueProjectFile = GlueState.Self.GlueProjectFileName;

                    GlueCommands.Self.PrintOutput($"Trying to load {glueProjectFile}, but could not find it, so " +
                        $"creating a new Glue Project file");

                    // temporary - eventually this will just be done in the .glux itself, or by the plugin 
                    // but for now we do it here because we only want to do it on new projects
                    Plugins.EmbeddedPlugins.CameraPlugin.CameraMainPlugin.CreateGlueProjectSettingsFor(ProjectManager.GlueProjectSave);


                    ProjectManager.FindGameClass();
                    GluxCommands.Self.SaveGlueProjectImmediately();

                    // no need to do this - will do it in PerformLoadGlux:
                    //PluginManager.ReactToLoadedGlux(ProjectManager.GlueProjectSave, glueProjectFile);
                    //shouldSaveGlux = true;

                    //// There's not a lot of code to generate so we can do it on the main thread
                    //// so we get the save to occur after
                    //GlueCommands.Self.GenerateCodeCommands.GenerateAllCodeSync();
                    //ProjectManager.SaveProjects();
                }
                PerformGluxLoad(projectFileName, glueProjectFile.FullPath, initializationWindow);

                #endregion

                SetInitWindowText("Cleaning extra Screens and Entities", initializationWindow);

                UnreferencedFilesManager.Self.RefreshUnreferencedFiles(true);

                TaskManager.Self.OnUiThread(() => MainGlueWindow.Self.Text = "FlatRedBall - " + projectFileName);

                if (shouldSaveGlux)
                {
                    GluxCommands.Self.SaveGlux(TaskExecutionPreference.AddOrMoveToEnd);
                }

                GlueCommands.Self.ProjectCommands.SaveProjects();

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

        private static bool DetermineIfShouldLoad(string projectFileName)
        {
            bool shouldLoad = true;

            // see if this project references any plugins that aren't installed:
            var gluxFileName = FileManager.RemoveExtension(projectFileName) + ".glux";
            var glueJavascriptFileName = FileManager.RemoveExtension(projectFileName) + ".gluj";
            var fileToLoad = System.IO.File.Exists(gluxFileName) ? gluxFileName
                : System.IO.File.Exists(glueJavascriptFileName) ? glueJavascriptFileName
                : string.Empty;
            if (!string.IsNullOrEmpty(fileToLoad))
            {
                try
                {
                    var tempGlux = GlueProjectSaveExtensions.Load(fileToLoad);

                    var requiredPlugins = tempGlux.PluginData.RequiredPlugins;

                    List<string> individualPluginMessages = new List<string>();

                    foreach (var requiredPlugin in requiredPlugins)
                    {
                        var matchingPlugin = PluginManager.AllPluginContainers.FirstOrDefault(item => item.Name == requiredPlugin.Name);

                        if (matchingPlugin == null)
                        {
                            individualPluginMessages.Add(requiredPlugin.Name);
                        }
                        else
                        {
                            switch (requiredPlugin.VersionRequirement)
                            {
                                case VersionRequirement.EqualToOrNewerThan:
                                    bool isNewerOrEqual = matchingPlugin.Plugin.Version >= new Version(requiredPlugin.Version);
                                    if (!isNewerOrEqual)
                                    {
                                        individualPluginMessages.Add($"{requiredPlugin.Name} must be updated\n\t{requiredPlugin.Version} required\n\t{matchingPlugin.Plugin.Version} installed");
                                    }
                                    break;
                                default: // eventually fill in the rest
                                    throw new NotImplementedException();
                                    //break;
                            }
                        }
                    }

                    string missingPluginMessage = null;
                    if (individualPluginMessages.Count != 0)
                    {
                        missingPluginMessage = $"The project {fileToLoad} requires the following plugins:\n";

                        foreach (var item in individualPluginMessages)
                        {
                            missingPluginMessage += "\n" + item;
                        }

                        missingPluginMessage += "\n\nWould you like to load the project anyway? It may not run, or may run incorrectly until all plugins are installed/updated.";
                    }

                    if (!string.IsNullOrEmpty(missingPluginMessage))
                    {
                        var result = MessageBox.Show(missingPluginMessage, "Missing Plugins", MessageBoxButtons.YesNo);

                        shouldLoad = result == DialogResult.Yes;

                    }

                }
                catch (Exception e)
                {
                    GlueGui.ShowMessageBox($"Could not load .glux file {fileToLoad}. Error:\n\n{e.ToString()}");
                    shouldLoad = false;
                }
            }

            return shouldLoad;
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

        private void PerformGluxLoad(string projectFileName, string glueProjectFile, InitializationWindowWpf initializationWindow)
        {
            SetInitWindowText("Loading FlatRedBall Project", initializationWindow);


            bool succeeded = true;

            succeeded = DeserializeGlueProjectInternal(projectFileName, glueProjectFile, initializationWindow);

            if (succeeded)
            {
                // This seems to take some time (like 1 second). Can we possibly
                // make it faster by having it chek Game1.cs first? Why is this so slow?
                ProjectManager.FindGameClass();

                AvailableAssetTypes.Self.AdditionalExtensionsToTreatAsAssets.Clear();

                IdentifyAdditionalAssetTypes();

                SetInitWindowText("Finding and fixing .glux errors", initializationWindow);
                ProjectManager.GlueProjectSave.FixErrors(true);
                ProjectManager.GlueProjectSave.RemoveInvalidStatesFromNamedObjects(true);

                FixProjectErrors();

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

                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("RefreshSourceFileCache");

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

                SetInitWindowText("Building out-of-date external files...", initializationWindow);
                BuildAllOutOfDateFiles();
                Section.EndContextAndTime();
                
                var allReferencedFileSaves = ObjectFinder.Self.GetAllReferencedFiles();
                Managers.TaskManager.Self.Add(() =>
                {
                    var wasAnythingModified = false;
                    foreach (var rfs in allReferencedFileSaves)
                    {
                        if(GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(rfs))
                        {
                            wasAnythingModified = true;
                        }
                    }

                    if(wasAnythingModified)
                    {
                        GlueCommands.Self.ProjectCommands.SaveProjects();
                    }
                },
                $"Calling UpdateFileMembershipInProject on {allReferencedFileSaves.Count} file(s)",
                TaskExecutionPreference.AddOrMoveToEnd);
                

                foreach(var element in ObjectFinder.Self.GlueProject.Screens)
                {
                    element.UpdateCustomProperties();
                    CheckForMissingCustomFile(element);

                }
                foreach (var entity in ObjectFinder.Self.GlueProject.Entities)
                {
                    entity.UpdateCustomProperties();
                    CheckForMissingCustomFile(entity);
                }

                // this was moved to be handled by the plugin on the "late" call:

                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("PrepareSyncedProjects");

                PrepareSyncedProjects(projectFileName, initializationWindow);

                mLastLoadedFilename = projectFileName;
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("MakeGeneratedItemsNested");

                // This should happen after loading synced projects
                SetInitWindowText("Nesting generated code files in .csproj", initializationWindow);
                GlueCommands.Self.ProjectCommands.MakeGeneratedCodeItemsNested();
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("GlobalContent");


                #region Update GlobalContent UI and code

                SetInitWindowText("Updating global content tree nodes", initializationWindow);

                GlueCommands.Self.RefreshCommands.RefreshGlobalContent();

                // Screens and Entities have the membership of their files
                // automatically updated when the tree nodes are created. This
                // is bad. GlobalContent does this better by requiring the call
                // to be explicitly made:
                UpdateGlobalContentFileProjectMembership();

                // I think this is handled automatically when regenerating all code...
                // Yes, down in GenerateAllCodeTask
                //GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();

                #endregion
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("Startup");

                SetInitWindowText("Setting StartUp Screen", initializationWindow);



                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("Performance code");


                FactoryElementCodeGenerator.AddGeneratedPerformanceTypes();
                Section.EndContextAndTime();


                SetInitWindowText("Notifying Plugins of startup", initializationWindow);


                PluginManager.ReactToLoadedGlux(ProjectManager.GlueProjectSave, glueProjectFile, (newString) => SetInitWindowText(newString, initializationWindow));
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("GenerateAllCode");
                SetInitWindowText("Generating all code", initializationWindow);

                // Fix before doing any generation
                GlueState.Self.CurrentGlueProject.FixAllTypesPostLoad();
                GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();
                Section.EndContextAndTime();

            }
        }


        private void UpdateGlobalContentFileProjectMembership()
        {
            bool wasAnythingAdded = false;
            foreach(var file in GlueState.Self.CurrentGlueProject.GlobalFiles)
            {
                if (GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(file))
                {
                    wasAnythingAdded = true;
                }
            }
            if(wasAnythingAdded)
            {
                // save the projects:
                GlueCommands.Self.ProjectCommands.SaveProjects();
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

                RightClickHelper.SetExternallyBuiltFileIfHigherThanCurrent(directoryToSet, false);
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

            if (initializationWindow == null)
            {
                closeInitWindow = true;

                    initializationWindow = new InitializationWindowWpf();

                    if (GlueGui.ShowGui)
                    {
                        initializationWindow.Show();
                    
                    }
            }
            return closeInitWindow;
        }

        private bool DeserializeGlueProjectInternal(string projectFileName, string glueProjectFile, InitializationWindowWpf initializationWindow)
        {
            bool succeeded = true;
            try
            {
                if (!TaskManager.Self.IsInTask())
                {
                    int m = 3;
                }
                ProjectManager.GlueProjectSave = GlueProjectSaveExtensions.Load(glueProjectFile);

                string errors;
                ProjectManager.GlueProjectSave.PostLoadInitialize(out errors);

                if (errors != null)
                {
                    GlueGui.ShowMessageBox(errors);
                }
            }
            catch (Exception e)
            {
                var mbmb = new MultiButtonMessageBox();
                mbmb.MessageText = "There was an error loading the .glux file.  What would you like to do?";

                mbmb.AddButton("Nothing - Glue will abort loading the project.", DialogResult.None);
                mbmb.AddButton("See the Exception", DialogResult.OK);
                mbmb.AddButton("Try loading again", DialogResult.Retry);
                mbmb.AddButton("Test for conflicts", DialogResult.Yes);

                DialogResult result = mbmb.ShowDialog(MainGlueWindow.Self);
                initializationWindow.Close();

                switch (result)
                {
                    case DialogResult.None:
                        // Do nothing;

                        break;
                    case DialogResult.OK:
                        MessageBox.Show(e.ToString());
                        break;
                    case DialogResult.Retry:
                        LoadProject(projectFileName);
                        break;
                    case DialogResult.Yes:
                        string text = FileManager.FromFileText(glueProjectFile);

                        if (text.Contains("<<<"))
                        {
                            MessageBox.Show("There are conflicts in your GLUX file.  You will need to use a merging " +
                                "tool or text editor to resolve these conflicts.");
                        }
                        else
                        {
                            MessageBox.Show("No Subversion conflicts found in your GLUX.");
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

        private void BuildAllOutOfDateFiles()
        {
            TaskManager.Self.AddOrRunIfTasked(() =>
            {
                if(ProjectManager.GlueProjectSave != null)
                {
                    Parallel.ForEach(ProjectManager.GlueProjectSave.Screens, (screenSave) =>
                    //foreach (ScreenSave screenSave in ProjectManager.GlueProjectSave.Screens)
                    {
                        BuildIfOutOfDate(screenSave.ReferencedFiles, runBuildsAsync: false, runInParallel: true);
                    }
                    );


                    Parallel.ForEach(ProjectManager.GlueProjectSave.Entities, (entitySave) =>
                    //foreach (EntitySave entitySave in ProjectManager.GlueProjectSave.Entities)
                    {
                        BuildIfOutOfDate(entitySave.ReferencedFiles, runBuildsAsync: false, runInParallel: true);
                    }
                    );

                    BuildIfOutOfDate(ProjectManager.GlueProjectSave.GlobalFiles, runBuildsAsync: false, runInParallel: true);
                }
            },
            "Build all out of date files");
        }

        private void BuildIfOutOfDate(List<ReferencedFileSave> rfsList, bool runBuildsAsync, bool runInParallel)
        {

            if(rfsList.Any(item => item == null))
            {
                throw new ArgumentException("List contains null files, which it should not!");
            }

            if (runInParallel)
            {
                Parallel.ForEach(rfsList, (rfs) =>
                {
                    BuildIfOutOfDate(runBuildsAsync, rfs);
                }
                );
            }
            else
            {
                foreach (ReferencedFileSave rfs in rfsList)
                {
                    BuildIfOutOfDate(runBuildsAsync, rfs);
                }
            }
        }

        private static void BuildIfOutOfDate(bool runBuildsAsync, ReferencedFileSave rfs)
        {
            if (rfs.GetIsBuiltFileOutOfDate())
            {
                string error = rfs.PerformExternalBuild(runAsync: runBuildsAsync);

                if (!string.IsNullOrEmpty(error))
                {
                    ErrorReporter.ReportError(GlueCommands.Self.GetAbsoluteFileName(rfs), error, false);
                }
            }
        }

        private void FixProjectErrors()
        {
            bool shouldSave = false;

            VisualStudioProject contentProjectBase = null;
            if (ProjectManager.ProjectBase != null)
            {
                contentProjectBase = ProjectManager.ProjectBase;

                if (ProjectManager.ProjectBase.ContentProject != null)
                {
                    contentProjectBase = (VisualStudioProject) ProjectManager.ProjectBase.ContentProject;
                }
            }

            List<ReferencedFileSave> rfsList = null;
            foreach (EntitySave entitySave in ProjectManager.GlueProjectSave.Entities)
            {
                rfsList = entitySave.ReferencedFiles;

                shouldSave |= FixContentPipelineProjectValues(rfsList, contentProjectBase);

            }

            foreach (ScreenSave screenSave in ProjectManager.GlueProjectSave.Screens)
            {
                rfsList = screenSave.ReferencedFiles;

                shouldSave |= FixContentPipelineProjectValues(rfsList, contentProjectBase);
            }

            rfsList = ProjectManager.GlueProjectSave.GlobalFiles;
            shouldSave |= FixContentPipelineProjectValues(rfsList, contentProjectBase);

            if (shouldSave)
            {
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }

        }

        private bool FixContentPipelineProjectValues(List<ReferencedFileSave> rfsList, VisualStudioProject contentProjectBase)
        {
            bool hasMadeChanges = false;

            if (contentProjectBase != null)
            {
                foreach (ReferencedFileSave rfs in rfsList)
                {
                    var item = contentProjectBase.GetItem(rfs.Name);


                    if (item != null)
                    {
                        if (rfs.UseContentPipeline)
                        {
                            //if (rfs.TextureFormat == Microsoft.Xna.Framework.Content.Pipeline.Processors.TextureProcessorOutputFormat.DxtCompressed &&
                            //    !item.HasMetadata("ProcessorParameters_TextureProcessorOutputFormat"))
                            //{
                            //    hasMadeChanges = true;
                            //    // Gotta make this thing use the DxtCompression
                            //    ContentPipelineHelper.UpdateTextureFormatFor(rfs);
                            //}
                        }
                    }
                }
            }

            return hasMadeChanges;
        }

        private void CheckForMissingCustomFile(GlueElement element)
        {
            string fileToSearchFor = FileManager.RelativeDirectory + element.Name + ".cs";

            if (!System.IO.File.Exists(fileToSearchFor))
            {
                var mbmb = new MultiButtonMessageBoxWpf();
                mbmb.MessageText = "The following file is missing\n\n" + fileToSearchFor + 
                    "\n\nwhich is used by\n\n" + element.ToString() + "\n\nWhat would you like to do?";
                mbmb.AddButton("Re-create an empty custom code file", DialogResult.OK);
                mbmb.AddButton("Ignore this problem", DialogResult.Cancel);

                var result = mbmb.ShowDialog();

                var dialogResult = mbmb.ClickedResult;

                if(dialogResult?.Equals(DialogResult.OK) == true)
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCustomCode(element);
                }
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
                            ProjectSyncer.SyncProjects(ProjectManager.ProjectBase, syncedProject, false);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Error syncing project:\n\n" + syncedProject.Name +
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
                MessageBox.Show("Could not find the project" + absoluteFileName + ", removing project from synched project list.");
            }
            else if (absoluteFileName == ProjectManager.ProjectBase.FullFileName)
            {
                // Victor Chelaru
                // January 1, 2013
                // One user had the
                // synced and main project
                // as the same.  This screws
                // up Glue pretty badly.  We need
                // to check for this and not allow
                // it.
                MessageBox.Show("A synced project is using the same file as the main project.  This is not allowed.  Glue will remove this synced project the synced project list.");
            }
            else
            {
                try
                {
                    ProjectBase vsp = ProjectCreator.CreateProject(absoluteFileName).Project;
                    vsp.OriginalProjectBaseIfSynced = ProjectManager.ProjectBase;

                    vsp.Load(absoluteFileName);

                    if (vsp.SaveAsRelativeSyncedProject == false && vsp.SaveAsAbsoluteSyncedProject == false)
                    {
                        vsp.SaveAsRelativeSyncedProject = true;
                        vsp.SaveAsAbsoluteSyncedProject = false;
                    }

                    if (String.Equals(FileManager.GetDirectory(absoluteFileName),
                            FileManager.GetDirectory(ProjectManager.ProjectBase.FullFileName.FullPath),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        vsp.SaveAsRelativeSyncedProject = false;
                        vsp.SaveAsAbsoluteSyncedProject = false;
                    }

                    lock (ProjectManager.SyncedProjects)
                    {
                        ProjectManager.AddSyncedProject(vsp);

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
