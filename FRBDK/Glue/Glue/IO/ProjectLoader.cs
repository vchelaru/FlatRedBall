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
using FlatRedBall.Glue.Parsing;
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
using GluePropertyGridClasses.Interfaces;

namespace FlatRedBall.Glue.IO
{
    public class ProjectLoader
    {
        #region Fields

        static ProjectLoader mSelf;
        private static InitializationWindow mCurrentInitWindow;
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

        
        public void LoadProject(string projectFileName, InitializationWindow initializationWindow = null)
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

            FileWatchManager.PerformFlushing = false;

            bool closeInitWindow = PrepareInitializationWindow(initializationWindow);

            // close the project before turning off task processing...
            ClosePreviousProject(projectFileName);

            TaskManager.Self.WaitForAllTasksFinished(pumpEvents:true);

            // turn off task processing while this is loading, so that no background tasks are running while plugins are starting up.
            // Do this *after* closing previous project, because closing previous project waits for all tasks to finish.
            TaskManager.Self.IsTaskProcessingEnabled = false;

            SetInitWindowText("Loading code project");

            var result = ProjectCreator.CreateProject(projectFileName);
            ProjectManager.ProjectBase = result.Project;

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


                SetInitWindowText("Finding Game class");


                FileWatchManager.UpdateToProjectDirectory();
                FileManager.RelativeDirectory = FileManager.GetDirectory(projectFileName);
                // this will make other threads work properly:
                FileManager.DefaultRelativeDirectory = FileManager.RelativeDirectory;

                ElementViewWindow.AddDirectoryNodes();

                #region Load the GlueProjectSave file if one exists

                string glueProjectFile = ProjectManager.GlueProjectFileName;
                bool shouldSaveGlux = false;

                if (!FileManager.FileExists(glueProjectFile))
                {
                    ProjectManager.GlueProjectSave = new GlueProjectSave();

                    GlueCommands.Self.PrintOutput($"Trying to load {glueProjectFile}, but could not find it, so" +
                        $"creating a new .glux file");

                    // temporary - eventually this will just be done in the .glux itself, or by the plugin 
                    // but for now we do it here because we only want to do it on new projects
                    Plugins.EmbeddedPlugins.CameraPlugin.CameraMainPlugin.CreateGlueProjectSettingsFor(ProjectManager.GlueProjectSave);


                    ProjectManager.FindGameClass();
                    GluxCommands.Self.SaveGlux();

                    // no need to do this - will do it in PerformLoadGlux:
                    //PluginManager.ReactToLoadedGlux(ProjectManager.GlueProjectSave, glueProjectFile);
                    //shouldSaveGlux = true;

                    //// There's not a lot of code to generate so we can do it on the main thread
                    //// so we get the save to occur after
                    //GlueCommands.Self.GenerateCodeCommands.GenerateAllCodeSync();
                    //ProjectManager.SaveProjects();
                }
                PerformGluxLoad(projectFileName, glueProjectFile);

                #endregion

                SetInitWindowText("Cleaning extra Screens and Entities");


                foreach (ScreenTreeNode screenNode in ElementViewWindow.AllScreens)
                {
                    if (screenNode.SaveObject == null)
                    {
                        ScreenSave screenSave = new ScreenSave();
                        screenSave.Name = screenNode.Text;

                        ProjectManager.GlueProjectSave.Screens.Add(screenSave);
                        screenNode.SaveObject = screenSave;
                    }
                }

                foreach (EntityTreeNode entityNode in ElementViewWindow.AllEntities)
                {
                    if (entityNode.EntitySave == null)
                    {
                        EntitySave entitySave = new EntitySave();
                        entitySave.Name = entityNode.Text;
                        entitySave.Tags.Add("GLUE");
                        entitySave.Source = "GLUE";

                        ProjectManager.GlueProjectSave.Entities.Add(entitySave);
                        entityNode.EntitySave = entitySave;
                    }
                }


                UnreferencedFilesManager.Self.RefreshUnreferencedFiles(true);

                MainGlueWindow.Self.Text = "FlatRedBall Glue - " + projectFileName;

                if (shouldSaveGlux)
                {
                    GluxCommands.Self.SaveGlux();
                }

                TaskManager.Self.AddSync(() =>
                {
                    // Someone may have unloaded the project while it was starting up
                    if (GlueState.Self.CurrentGlueProject != null)
                    {
                        GlueCommands.Self.ProjectCommands.SaveProjects();
                    }
                }, "Save all projects after initial load");

                FileWatchManager.PerformFlushing = true;
                FileWatchManager.FlushAndClearIgnores();
            }
            if (closeInitWindow)
            {
                mCurrentInitWindow.Close();
            }


            Section.EndContextAndTime();

            TaskManager.Self.IsTaskProcessingEnabled = true;

            // If we ever want to make things go faster, turn this back on and let's see what's going on.
            //topSection.Save("Sections.xml");
        }

        private static bool DetermineIfShouldLoad(string projectFileName)
        {
            bool shouldLoad = true;

            // see if this project references any plugins that aren't installed:
            var glueFileName = FileManager.RemoveExtension(projectFileName) + ".glux";
            if (System.IO.File.Exists(glueFileName))
            {
                try
                {
                    var tempGlux = GlueProjectSaveExtensions.Load(glueFileName);

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
                        missingPluginMessage = $"The project {glueFileName} requires the following plugins:\n";

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
                    GlueGui.ShowMessageBox($"Could not load .glux file {glueFileName}. Error:\n\n{e.ToString()}");
                    shouldLoad = false;
                }
            }

            return shouldLoad;
        }

        public void GetCsprojToLoad(out string csprojToLoad)
        {
            csprojToLoad = CommandLineManager.Self.ProjectToLoad;
            var settingsSave = ProjectManager.GlueSettingsSave;

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

        private void PerformGluxLoad(string projectFileName, string glueProjectFile)
        {
            SetInitWindowText("Loading .glux");


            bool succeeded = true;

            succeeded = DeserializeGluxXmlInternal(projectFileName, glueProjectFile);

            if (succeeded)
            {
                // This seems to take some time (like 1 second). Can we possibly
                // make it faster by having it chek Game1.cs first? Why is this so slow?
                ProjectManager.FindGameClass();

                AvailableAssetTypes.Self.AdditionalExtensionsToTreatAsAssets.Clear();

                IdentifyAdditionalAssetTypes();

                SetInitWindowText("Finding and fixing .glux errors");
                ProjectManager.GlueProjectSave.FixErrors(true);
                ProjectManager.GlueProjectSave.RemoveInvalidStatesFromNamedObjects(true);

                FixProjectErrors();

                SetUnsetValues();


                

                ProjectManager.LoadOrCreateProjectSpecificSettings(FileManager.GetDirectory(projectFileName));

                SetInitWindowText("Notifying plugins of project...");


                // The project specific settings are needed before the plugins do their thing...
                PluginManager.ReactToLoadedGluxEarly(ProjectManager.GlueProjectSave);

                // and after that's done we can validate that the build tools are there
                BuildToolAssociationManager.Self.ProjectSpecificBuildTools.ValidateBuildTools(FileManager.GetDirectory(projectFileName));

                ProjectManager.GlueProjectSave.UpdateIfTranslationIsUsed();

                Section.GetAndStartContextAndTime("Add items");

                SetInitWindowText("Creating project view...");
                

                AddEmptyTreeItems();

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
                SetInitWindowText("Refreshing Source File Cache...");
                RefreshSourceFileCache();

                SetInitWindowText("Checking for additional missing files...");

                SetInitWindowText("Building out of date external files...");
                BuildAllOutOfDateFiles();
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("RefreshGlobalContentDirectory");

                SetInitWindowText("Refreshing global content dictionary...");
                ReferencedFileSaveCodeGenerator.RefreshGlobalContentDictionary();
                GlobalContentCodeGenerator.SuppressGlobalContentDictionaryRefresh = true;

                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("Screens");

                SetInitWindowText("Creating tree nodes...");

                CreateScreenTreeNodes();
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("Entities");

                CreateEntityTreeNodes();
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("SortEntities");



                ElementViewWindow.SortEntities();

                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("PrepareSyncedProjects");

                PrepareSyncedProjects(projectFileName);

                mLastLoadedFilename = projectFileName;
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("MakeGeneratedItemsNested");

                // This should happen after loading synced projects
                SetInitWindowText("Nesting generated items");
                GlueCommands.Self.ProjectCommands.MakeGeneratedCodeItemsNested();
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("GlobalContent");


                #region Update GlobalContent UI and code

                SetInitWindowText("Updating global content");

                ElementViewWindow.UpdateGlobalContentTreeNodes(false);

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

                SetInitWindowText("Setting StartUp Screen");


                #region Set the Startup Screen

                if (!string.IsNullOrEmpty(ProjectManager.GlueProjectSave.StartUpScreen))
                {
                    TreeNode startUpTreeNode = GlueState.Self.Find.ScreenTreeNode(ProjectManager.GlueProjectSave.StartUpScreen);

                    ElementViewWindow.StartUpScreen = startUpTreeNode;

                    if (startUpTreeNode == null)
                    {
                        ProjectManager.GlueProjectSave.StartUpScreen = "";
                    }
                }

                #endregion

                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("Performance code");


                FactoryCodeGenerator.AddGeneratedPerformanceTypes();
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("CSV generation");

                CsvCodeGenerator.RegenerateAllCsvs();
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("PluginManager Init");

                PluginManager.Initialize(false);

                SetInitWindowText("Notifying Plugins of startup");


                PluginManager.ReactToLoadedGlux(ProjectManager.GlueProjectSave, glueProjectFile, SetInitWindowText);
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("GenerateAllCode");
                SetInitWindowText("Generating all code");
                GlueCommands.Self.GenerateCodeCommands.GenerateAllCodeTask();
                Section.EndContextAndTime();
                GlobalContentCodeGenerator.SuppressGlobalContentDictionaryRefresh = false;
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


        private static void AddEmptyTreeItems()
        {
            #region Add the build items to the project - this generates UI for them
            // We need a project to be loaded so that we can investigate it for
            // what is a Screen/Entity in case there is no generated code already
            // present.
            // Update - I don't think this matters anymore, we should base it off of the
            // glux below

            Section.GetAndStartContextAndTime("Screens");
            foreach (ScreenSave screen in ProjectManager.GlueProjectSave.Screens)
            {
                ElementViewWindow.AddScreen(screen); ;

            }
            Section.EndContextAndTime();
            Section.GetAndStartContextAndTime("Entities");
            foreach (EntitySave entity in ProjectManager.GlueProjectSave.Entities)
            {
                ElementViewWindow.AddEntity(entity, generateCode: false);

            }
            ElementViewWindow.ResumeLayout();
            Section.EndContextAndTime();
            #endregion
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

        private static void ClosePreviousProject(string projectFileName)
        {
            if (ProjectManager.ProjectBase != null)
            {
                MainGlueWindow.CloseProject(shouldSave:false, isExiting:false);
            }
        }

        private static bool PrepareInitializationWindow(InitializationWindow initializationWindow)
        {
            mCurrentInitWindow = initializationWindow;
            bool closeInitWindow = false;

            if (mCurrentInitWindow == null)
            {
                closeInitWindow = true;

                mCurrentInitWindow = new InitializationWindow();

                if (GlueGui.ShowGui)
                {
                    mCurrentInitWindow.Show(MainGlueWindow.Self);
                }
            }
            return closeInitWindow;
        }

        private void CreateEntityTreeNodes()
        {
            //ProjectManager.GlueProjectSave.Entities[78].NamedObjects[9].UpdateCustomProperties();


            // Let's make this faster.
            //foreach (EntitySave entity in ProjectManager.GlueProjectSave.Entities)
            // Actually, the contained functions may show popups, and if 2 simultaneous popups
            // are shown, one can cancel the other out and this can cause Glue to freeze. This means
            // we can't parallel foreah it
            //Parallel.ForEach(ProjectManager.GlueProjectSave.Entities, (entity) =>
            foreach (EntitySave entity in ProjectManager.GlueProjectSave.Entities)
                {
                    entity.UpdateCustomProperties();
                    entity.UpdateFromBaseType();
                }

            for (int i = 0; i < ProjectManager.GlueProjectSave.Entities.Count; i++)
            {
                EntitySave entitySave = ProjectManager.GlueProjectSave.Entities[i];
                // This is so fast that we no longer need to show the
                // user details - and not doing it will make things even faster
                //SetInitWindowText("Creating Entity: " + entitySave.Name);

                EntityTreeNode entityTreeNode = GlueState.Self.Find.EntityTreeNode(entitySave.Name);

                #region If there is no EntityTreeNode

                if (entityTreeNode == null)
                {
                    // See if the file exists
                    string fileToSearchFor = FileManager.RelativeDirectory + entitySave.Name + ".cs";

                    if (System.IO.File.Exists(fileToSearchFor))
                    {
                        // If we got here that means there's probably not a build item for this file
                        MessageBox.Show("The Glue project has the following Entity:\n" + entitySave.Name + "\n" +
                            "but this file is not part of Visual Studio.  This file may have been removed manually or " +
                            "there may have been some saving error.  You should close Glue, manually add this and the Generated file " +
                            "to Visual Studio, then restart Glue.");
                        MainGlueWindow.Self.HasErrorOccurred = true;
                    }
                    else
                    {
                        MultiButtonMessageBox mbmb = new MultiButtonMessageBox();

                        mbmb.MessageText = "Could not find the file name\n\n" + fileToSearchFor + "\n\nwhich is used by the entity\n\n" + entitySave.Name + "\n\n" +
                            "What would you like to do?";


                        mbmb.AddButton("Create a new custom code file", DialogResult.Yes);
                        mbmb.AddButton("Delete this Entity", DialogResult.No);
                        mbmb.AddButton("Do nothing.  The Entity will not show up in Glue until this problem is fixed.", DialogResult.Cancel);

                        DialogResult result = mbmb.ShowDialog(MainGlueWindow.Self);

                        switch (result)
                        {
                            case DialogResult.Yes:
                                if (entityTreeNode == null)
                                {
                                    entityTreeNode = ElementViewWindow.AddEntity(entitySave);
                                }

                                CodeWriter.GenerateAndAddElementCustomCode(entitySave);

                                //entityTreeNode.GenerateCode(ProjectManager.EntityTemplateCode);

                                break;
                            case DialogResult.No:
                                ProjectManager.GlueProjectSave.Entities.RemoveAt(i);
                                i--;
                                continue;

                            //break;
                            case DialogResult.Cancel:
                                // do nothing
                                continue;

                            //break;
                        }

                        System.Windows.Forms.MessageBox.Show("Could not create the EntitySave for " + entitySave.Name +
                            " because the following file doesn't exist\n\n" + fileToSearchFor);
                    }
                    //mGlueProjectSave.Entities.RemoveAt(i);
                    //i--;
                }

                #endregion


                entityTreeNode.EntitySave = entitySave;

                CheckForMissingCustomFile(entityTreeNode);


                entityTreeNode.UpdateReferencedTreeNodes();

                // moved above
                //entityTreeNode.EntitySave.UpdateCustomProperties();
                //entityTreeNode.EntitySave.UpdateFromBaseType();
            }
        }

        private void CreateScreenTreeNodes()
        {
            for (int i = 0; i < ProjectManager.GlueProjectSave.Screens.Count; i++)
            {
                ScreenSave screenSave = ProjectManager.GlueProjectSave.Screens[i];
                // This is so fast now that we don't even need to show the user
                // what's going on
                //SetInitWindowText("Creating Screens: " + screenSave.Name);

                ScreenTreeNode screenTreeNode = GlueState.Self.Find.ScreenTreeNode(ProjectManager.GlueProjectSave.Screens[i].Name);

                #region If there is no screenTreeNode

                if (screenTreeNode == null)
                {
                    // See if the file exists
                    string fileToSearchFor = FileManager.RelativeDirectory + screenSave.Name + ".cs";

                    if (System.IO.File.Exists(fileToSearchFor))
                    {
                        // If we got here that means there's probably not a build item for this file
                        MessageBox.Show("The Glue project has the following Screen:\n" + screenSave.Name + "\n" +
                            "but this file is not part of Visual Studio.  This file may have been removed manually or " +
                            "there may have been some saving error.  You should close Glue, manually add this and the Generated file " +
                            "to Visual Studio, then restart Glue.");
                        MainGlueWindow.Self.HasErrorOccurred = true;
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("Could not create the ScreenSave for " + screenSave.Name);
                    }


                    //mGlueProjectSave.Screens.RemoveAt(i);
                    //i--;
                    continue;
                }

                #endregion

                screenTreeNode.SaveObject = ProjectManager.GlueProjectSave.Screens[i];

                CheckForMissingCustomFile(screenTreeNode);


                if (ProjectManager.GlueProjectSave.Screens[i].IsRequiredAtStartup)
                {
                    screenTreeNode.BackColor = ElementViewWindow.RequiredScreenColor;
                }

                screenTreeNode.UpdateReferencedTreeNodes();

                screenTreeNode.SaveObject.UpdateCustomProperties();
            }
        }

        private bool DeserializeGluxXmlInternal(string projectFileName, string glueProjectFile)
        {
            bool succeeded = true;
            try
            {
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
                MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                mbmb.MessageText = "There was an error loading the .glux file.  What would you like to do?";

                mbmb.AddButton("Nothing - Glue will abort loading the project.", DialogResult.None);
                mbmb.AddButton("See the Exception", DialogResult.OK);
                mbmb.AddButton("Try loading again", DialogResult.Retry);
                mbmb.AddButton("Test for conflicts", DialogResult.Yes);

                DialogResult result = mbmb.ShowDialog(MainGlueWindow.Self);
                mCurrentInitWindow.Close();

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

        public void SetInitWindowText(string subtext)
        {
            if (mCurrentInitWindow != null)
            {
                mCurrentInitWindow.SubMessage = subtext;
                Application.DoEvents();
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
                ErrorReporter.ReportError("", error, true);

            }

        }

        private void BuildAllOutOfDateFiles()
        {
            // this can be done async
            TaskManager.Self.AddAsyncTask(() =>
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
                },
                "Build all out of date files"
                );
        }

        private void BuildIfOutOfDate(List<ReferencedFileSave> rfsList, bool runBuildsAsync, bool runInParallel)
        {
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
                    ErrorReporter.ReportError(ProjectManager.MakeAbsolute(rfs.Name, true), error, false);
                }
            }
        }

        private void FixProjectErrors()
        {
            bool shouldSave = false;

            ProjectBase contentProjectBase = null;
            if (ProjectManager.ProjectBase != null)
            {
                contentProjectBase = ProjectManager.ProjectBase;

                if (ProjectManager.ProjectBase.ContentProject != null)
                {
                    contentProjectBase = ProjectManager.ProjectBase.ContentProject;
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
                ProjectManager.SaveProjects();
            }

        }

        private bool FixContentPipelineProjectValues(List<ReferencedFileSave> rfsList, ProjectBase contentProjectBase)
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
                            if (rfs.TextureFormat == Microsoft.Xna.Framework.Content.Pipeline.Processors.TextureProcessorOutputFormat.DxtCompressed &&
                                !item.HasMetadata("ProcessorParameters_TextureProcessorOutputFormat"))
                            {
                                hasMadeChanges = true;
                                // Gotta make this thing use the DxtCompression
                                ContentPipelineHelper.UpdateTextureFormatFor(rfs);
                            }
                        }
                    }
                }
            }

            return hasMadeChanges;
        }

        private void CheckForMissingCustomFile(BaseElementTreeNode baseElementTreeNode)
        {
            if (baseElementTreeNode != null)
            {
                IElement element = baseElementTreeNode.SaveObject;

                string fileToSearchFor = FileManager.RelativeDirectory + element.Name + ".cs";

                if (!System.IO.File.Exists(fileToSearchFor))
                {
                    MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                    mbmb.MessageText = "The following file is missing\n\n" + fileToSearchFor + 
                        "\n\nwhich is used by\n\n" + element.ToString() + "\n\nWhat would you like to do?";
                    mbmb.AddButton("Re-create an empty custom code file", DialogResult.OK);
                    mbmb.AddButton("Ignore this problem", DialogResult.Cancel);

                    DialogResult result = mbmb.ShowDialog(MainGlueWindow.Self);

                    switch (result)
                    {
                        case DialogResult.OK:

                            CodeWriter.GenerateAndAddElementCustomCode(element);

                            break;
                        case DialogResult.Cancel:
                            // Ignore, do nothing
                            break;

                    }
                }

            }
        }

        private void PrepareSyncedProjects(string projectFileName)
        {
            SetInitWindowText("Loading synced projects Entities");
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
            else if (FileManager.Standardize(absoluteFileName).ToLower() == FileManager.Standardize(ProjectManager.ProjectBase.FullFileName).ToLower())
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

                    if (FileManager.GetDirectory(absoluteFileName).ToLower() == FileManager.GetDirectory(ProjectManager.ProjectBase.FullFileName).ToLower())
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
