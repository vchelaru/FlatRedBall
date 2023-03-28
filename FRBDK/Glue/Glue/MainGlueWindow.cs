using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.IO;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Instructions;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.TypeConversions;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Data;
using FlatRedBall.Glue.SetVariable;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using System.Threading.Tasks;
using FlatRedBall.Instructions.Reflection;
using Microsoft.Xna.Framework.Audio;
using System.Windows.Forms.Integration;
using GlueFormsCore.Controls;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.Build.Evaluation;
using Newtonsoft.Json;
using SkiaSharp;

namespace Glue
{
    public partial class MainGlueWindow : Form
    {
        #region Fields/Properties

        public bool HasErrorOccurred = false;

        private static MainGlueWindow mSelf;

        public static MainPanelControl MainWpfControl { get; private set; }

        public static MainGlueWindow Self => mSelf;
        

        public static int UiThreadId { get; private set; }

        //internal MainExplorerPlugin mainExplorerPlugin;

        private System.Windows.Forms.MenuStrip mMenu;


        public System.ComponentModel.IContainer Components => components;

        public System.Windows.Forms.PropertyGrid PropertyGrid;

        public int NumberOfStoredRecentFiles
        {
            get;
            set;
        }

        #endregion

        private static void SetMsBuildEnvironmentVariable()
        {
            var startInfo = new ProcessStartInfo("dotnet", "--list-sdks")
            {
                RedirectStandardOutput = true
            };

            var process = Process.Start(startInfo);
            process.WaitForExit(1000);

            var output = process.StandardOutput.ReadToEnd();
            var sdkPaths = Regex.Matches(output, "([0-9]+.[0-9]+.[0-9]+) \\[(.*)\\]")
                .OfType<Match>()

                .Select(m => System.IO.Path.Combine(m.Groups[2].Value, m.Groups[1].Value, "MSBuild.dll"))
                // https://stackoverflow.com/questions/75702346/why-does-the-presence-of-net-7-0-2-sdk-cause-the-sdk-resolver-microsoft-dotnet?noredirect=1#comment133550210_75702346
                // "7.0.20" instead of "7.0.201"
                .Where(item => item.Contains("7.0.20") == false)
                .ToArray();

            if(sdkPaths.Count() > 0)
            {

                var sdkPath = sdkPaths.FirstOrDefault(item => item.Contains("sdk\\6."));
                if(string.IsNullOrEmpty(sdkPath))
                {
                    sdkPath = sdkPaths.Last();
                }
                    
                Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", sdkPath);

                GlueCommands.Self.PrintOutput($"Using MSBUILD from {sdkPath}");
            }
            else
            {
                var message = "Could not find any .NET SDKs installed. Glue will not be able to load projets. We recommend installing .NET 6 SDK";
                GlueCommands.Self.PrintOutput(message);

                MessageBox.Show(message);
            }
        }

        public MainGlueWindow()
        {
            // Vic says - this makes Glue use the latest MSBuild environments
            // Running on AnyCPU means we run in 64 bit and can load VS 22 64 bit libs.
            SetMsBuildEnvironmentVariable();

            mSelf = this;
            UiThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            InitializeComponent();

            CreateMenuStrip();

            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Move += HandleWindowMoved;

            // this fires continually, so instead overriding wndproc
            this.ResizeEnd += HandleResizeEnd;

            CreateMainWpfPanel();
            // so docking works
            //this.Controls.SetChildIndex(this.MainPanelSplitContainer, 0);
            this.Controls.Add(this.mMenu);

        }

        private void Form1_Load(object sender, EventArgs e)
        {

            StartUpGlue();

        }
        internal async void StartUpGlue()
        {
            //Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();

            Microsoft.Build.Evaluation.Project item = null;


            // Some stuff can be parallelized.  We're going to run stuff
            // that can be parallelized in parallel, and then block to wait for
            // all tasks to finish when we need to

            AddObjectsToIocContainer();

            AddErrorReporters();

            var initializationWindow = new InitializationWindowWpf();

            // Initialize GlueGui before using it:
            GlueGui.Initialize(mMenu);
            initializationWindow.Show();

            initializationWindow.Message = "Initializing Glue Systems";
            Application.DoEvents();

            // Add Glue.Common
            PropertyValuePair.AdditionalAssemblies.Add(typeof(PlatformSpecificType).Assembly);

            // Monogame:
            PropertyValuePair.AdditionalAssemblies.Add(typeof(SoundEffectInstance).Assembly);

            // Async stuff
            {

                initializationWindow.SubMessage = "Initializing EventManager"; Application.DoEvents();
                TaskManager.Self.Add(() => EventManager.Initialize(), "Initializing EventManager");

                Application.DoEvents();

                initializationWindow.SubMessage = "Initializing ExposedVariableManager"; Application.DoEvents();
                try
                {
                    ExposedVariableManager.Initialize();
                }
                catch (Exception excep)
                {
                    GlueGui.ShowException("Could not load assemblies - you probably need to rebuild Glue.", "Error", excep);
                    return;
                }
            }

            initializationWindow.SubMessage = "Initialize Error Reporting"; Application.DoEvents();
            ErrorReporter.Initialize(this);

            initializationWindow.SubMessage = "Initializing Right Click Menus"; Application.DoEvents();
            RightClickHelper.Initialize();
            initializationWindow.SubMessage = "Initializing Property Grids"; Application.DoEvents();
            PropertyGridRightClickHelper.Initialize();
            initializationWindow.SubMessage = "Initializing InstructionManager"; Application.DoEvents();
            InstructionManager.Initialize();
            initializationWindow.SubMessage = "Initializing TypeConverter"; Application.DoEvents();
            TypeConverterHelper.InitializeClasses();

            initializationWindow.SubMessage = "Initializing Navigation Stack"; Application.DoEvents();

            initializationWindow.Message = "Loading Settings"; Application.DoEvents();
            // We need to load the glue settings before loading the plugins so that we can 
            // shut off plugins according to settings
            LoadGlueSettings(initializationWindow);


            // Initialize before loading GlueSettings;
            // Also initialize before loading plugins so that plugins
            // can access the standard ATIs
            string startupPath =
                FileManager.GetDirectory(System.Reflection.Assembly.GetExecutingAssembly().Location);

            AvailableAssetTypes.Self.Initialize(startupPath);

            initializationWindow.Message = "Loading Plugins"; Application.DoEvents();
            List<string> pluginsToIgnore = new List<string>();
            if (GlueState.Self.CurrentPluginSettings != null)
            {
                pluginsToIgnore = GlueState.Self.CurrentPluginSettings.PluginsToIgnore;
            }

            // This plugin initialization needs to happen before LoadGlueSettings
            // EVentually we can break this out
            // Vic asks - why does it need to happen first?
            //mainExplorerPlugin = new MainExplorerPlugin();
            //mainExplorerPlugin.Initialize();

            PluginManager.Initialize(true, pluginsToIgnore);

            ShareUiReferences(PluginCategories.All);

            try
            {
                FileManager.PreserveCase = true;

                initializationWindow.Message = "Initializing File Watch";
                Application.DoEvents();
                // Initialize the FileWatchManager before LoadGlueSettings
                FileWatchManager.Initialize();

                initializationWindow.Message = "Loading Custom Type Info";
                Application.DoEvents();

                // Gotta do this too before Loading Glue Settings
                ProjectManager.Initialize();

                initializationWindow.Message = "Loading Project";
                Application.DoEvents();

                // LoadSettings before loading projects
                EditorData.LoadPreferenceSettings();

                while (TaskManager.Self.AreAllAsyncTasksDone == false)
                {
                    System.Threading.Thread.Sleep(100);
                }
                await LoadProjectConsideringSettingsAndArgs(initializationWindow);

                // This needs to happen after loading the project:
                ShareUiReferences(PluginCategories.ProjectSpecific);

                Application.DoEvents();
                EditorData.FileAssociationSettings.LoadSettings();

                EditorData.LoadGlueLayoutSettings();

                //MainPanelSplitContainer.UpdateSizesFromSettings();

                if (EditorData.GlueLayoutSettings.Maximized)
                    WindowState = FormWindowState.Maximized;


                //ProcessLocations.Initialize();

                ProjectManager.mForm = this;

            }
            catch (Exception exc)
            {
                if (GlueGui.ShowGui)
                {
                    System.Windows.Forms.MessageBox.Show(exc.ToString());

                    FileManager.SaveText(exc.ToString(),
                                         FileManager.UserApplicationDataForThisApplication + "InitError.txt");
                    PluginManager.ReceiveError(exc.ToString());

                    HasErrorOccurred = true;
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                if (GlueGui.ShowGui)
                {
                    initializationWindow.Close();
                    this.BringToFront();
                }
            }

            // this gives the search bar focus, so hotkeys work
            // If we don't wait a little bit, it won't work, so give 
            // a small delay:
            await Task.Delay(100);
            PluginManager.ReactToCtrlF();
        }

        private void HandleResizeEnd(object sender, EventArgs e)
        {
            PluginManager.ReactToMainWindowResizeEnd();
        }

        // I thought this was needed but I think it will work with ResizeEnd event, I had a bug initially.
        //protected override void WndProc(ref Message m)
        //{
        //    const int WM_EXITSIZEMOVE = 0x232;

        //    switch (m.Msg)
        //    {
        //        case WM_EXITSIZEMOVE:
        //            base.WndProc(ref m);
        //            GlueCommands.Self.PrintOutput("End resize");
        //            PluginManager.ReactToMainWindowResizeEnd();
        //            break;
        //        default:
        //            base.WndProc(ref m);
        //            break;
        //    }
        //}

        private void HandleWindowMoved(object sender, EventArgs e)
        {
            PluginManager.ReactToMainWindowMoved();
        }

        private void CreateMenuStrip()
        {
            this.mMenu = new System.Windows.Forms.MenuStrip();
            // 
            // mMenu
            // 
            this.mMenu.Location = new System.Drawing.Point(0, 0);
            this.mMenu.Name = "mMenu";
            this.mMenu.Size = new System.Drawing.Size(764, 24);
            this.mMenu.TabIndex = 1;
            this.mMenu.Text = "menuStrip1";
            this.MainMenuStrip = this.mMenu;
        }

        private void CreateMainWpfPanel()
        {
            var wpfHost = new ElementHost();
            wpfHost.Dock = DockStyle.Fill;
            MainWpfControl = new MainPanelControl();
            wpfHost.Child = MainWpfControl;
            this.Controls.Add(wpfHost);
            this.PerformLayout();
        }

        public void Invoke(Action action)
        {
            var wasInTask = TaskManager.Self.IsInTask();

            this.Invoke((MethodInvoker)delegate
            {
                try
                {
                    if(wasInTask)
                    {
                        RunOnUiThreadTasked(action);
                    }
                    else
                    {
                        action();
                    }
                }
                catch(Exception e)
                {
                    if(!IsDisposed && !ProjectManager.WantsToCloseProject)
                    {
                        throw e;
                    }
                    // otherwise, we don't care, they're exiting
                }
            });
        }

        public T Invoke<T>(Func<T> func)
        {
            var wasInTask = TaskManager.Self.IsInTask();

            T toReturn = default(T);
            base.Invoke((MethodInvoker)delegate
            {
                try
                {
                    if (wasInTask)
                    {
                        RunOnUiThreadTasked(func);
                    }
                    else
                    {
                        func();
                    }
                }
                catch (Exception e)
                {
                    if (!IsDisposed)
                    {
                        throw e;
                    }
                    // otherwise, we don't care, they're exiting
                }
            });

            return toReturn;
        }

        public Task Invoke(Func<Task> func)
        {
            var wasInTask = TaskManager.Self.IsInTask();
            Task toReturn = Task.CompletedTask;

            var asyncResult = base.BeginInvoke((MethodInvoker)delegate
            {
                try
                {
                    if (wasInTask)
                    {
                        toReturn = RunOnUiThreadTasked(func);
                    }
                    else
                    {
                        toReturn = func();
                    }
                }
                catch (Exception e)
                {
                    if (!IsDisposed)
                    {
                        throw e;
                    }
                    // otherwise, we don't care, they're exiting
                }
            });

            asyncResult.AsyncWaitHandle.WaitOne();

            return toReturn;
        }

        public Task<T> Invoke<T>(Func<Task<T>> func)
        {
            var wasInTask = TaskManager.Self.IsInTask();
            Task<T> toReturn = Task.FromResult(default(T));

            base.Invoke((MethodInvoker)delegate
            {
                try
                {
                    if (wasInTask)
                    {
                        toReturn = RunOnUiThreadTasked(func);
                    }
                    else
                    {
                        toReturn = func();
                    }
                }
                catch (Exception e)
                {
                    if (!IsDisposed)
                    {
                        throw e;
                    }
                    // otherwise, we don't care, they're exiting
                }
            });

            return toReturn;
        }

        private void RunOnUiThreadTasked(Action action) => action();
        private T RunOnUiThreadTasked<T>(Func<T> action) => action();
        private Task<T> RunOnUiThreadTasked<T>(Func<Task<T>> action) => action();



        private void AddErrorReporters()
        {
            EditorObjects.IoC.Container.Get<GlueErrorManager>()
                .Add(new CsvErrorReporter());

        }

        private void AddObjectsToIocContainer()
        {
            EditorObjects.IoC.Container.Set(new SetPropertyManager());
            EditorObjects.IoC.Container.Set(new NamedObjectSetVariableLogic());
            EditorObjects.IoC.Container.Set(new StateSaveCategorySetVariableLogic());
            EditorObjects.IoC.Container.Set(new StateSaveSetVariableLogic());
            EditorObjects.IoC.Container.Set(new EventResponseSaveSetVariableLogic());
            EditorObjects.IoC.Container.Set(new ReferencedFileSaveSetPropertyManager());
            EditorObjects.IoC.Container.Set(new CustomVariableSaveSetPropertyLogic());
            EditorObjects.IoC.Container.Set(new EntitySaveSetPropertyLogic());
            EditorObjects.IoC.Container.Set(new ScreenSaveSetVariableLogic());
            EditorObjects.IoC.Container.Set(new GlobalContentSetVariableLogic());
            EditorObjects.IoC.Container.Set(new PluginUpdater());

            EditorObjects.IoC.Container.Set<IGlueState>(GlueState.Self);
            EditorObjects.IoC.Container.Set<IGlueCommands>(GlueCommands.Self);

            EditorObjects.IoC.Container.Set<GlueErrorManager>(new GlueErrorManager());
        }

        private async Task LoadProjectConsideringSettingsAndArgs(InitializationWindowWpf initializationWindow)
        {
            // This must be called after setting the GlueSettingsSave
            string csprojToLoad;
            ProjectLoader.Self.GetCsprojToLoad(out csprojToLoad);

            if (!string.IsNullOrEmpty(csprojToLoad))
            {
                if (initializationWindow != null)
                {
                    initializationWindow.Message = "Loading " + csprojToLoad;
                }

                await ProjectLoader.Self.LoadProject(csprojToLoad, initializationWindow);
            }
        }

        private void ShareUiReferences(PluginCategories pluginCategories)
        {
            PluginManager.ShareMenuStripReference(mMenu, pluginCategories);

            PluginManager.PrintPreInitializeOutput();
        }

        private void LoadGlueSettings(InitializationWindowWpf initializationWindow)
        {
            FilePath settingsFileLocation = null;
            // Need to fix up saving/loading of this in json since there's some converter causing problems
            //if(FileManager.FileExists(GlueSettingsSave.SettingsFileNameJson))
            //{
            //    settingsFileLocation = GlueSettingsSave.SettingsFileNameJson;
            //}
            //else 
            if (FileManager.FileExists(GlueSettingsSave.SettingsFileName))
            {
                settingsFileLocation = GlueSettingsSave.SettingsFileName;
            }
            if (settingsFileLocation != null)
            {
                GlueSettingsSave settingsSave = null;

                bool didErrorOccur = false;

                try
                {
                    if(settingsFileLocation.Extension == "json")
                    {
                        var text = System.IO.File.ReadAllText(settingsFileLocation.FullPath);
                        settingsSave = JsonConvert.DeserializeObject<GlueSettingsSave>(text);
                    }
                    else
                    {
                        settingsSave = FileManager.XmlDeserialize<GlueSettingsSave>(settingsFileLocation.FullPath);
                    }
                    settingsSave.FixAllTypes();
                }
                catch (Exception e)
                {
                    MessageBox.Show("Error loading your settings file which is located at\n\n" +
                        settingsFileLocation + "\n\nError details:\n\n" + e.ToString());
                    didErrorOccur = true;
                }
                
                if (!didErrorOccur)
                {
                    GlueState.Self.GlueSettingsSave = settingsSave;

                    string csprojToLoad;
                    ProjectLoader.Self.GetCsprojToLoad(out csprojToLoad);


                    // Load the plugins settings if it exists
                    string gluxDirectory = null;

                    if (!string.IsNullOrEmpty(csprojToLoad))
                    {
                        gluxDirectory = FileManager.GetDirectory(csprojToLoad);
                    }

                    if (PluginSettings.FileExists(gluxDirectory))
                    {
                        ProjectManager.PluginSettings = PluginSettings.Load(gluxDirectory);
                    }
                    else
                    {
                        ProjectManager.PluginSettings = new PluginSettings();
                    }



                    // attempt to update the positions

                    // This sets the last position, but doesn't work on multiple monitors
                    //this.Left = settingsSave.WindowLeft;
                    //this.Top = settingsSave.WindowTop;

                    // This used to be 0, b
                    //this.Height = settingsSave.WindowHeight > 100 ? settingsSave.WindowHeight : 480;
                    //this.Width = settingsSave.WindowWidth > 100 ? settingsSave.WindowWidth : 640;

                    MainPanelControl.Self.ApplyGlueSettings(GlueState.Self.GlueSettingsSave);
                }
            }
            else
            {
                GlueState.Self.GlueSettingsSave.Save();
            }
        }

        static bool WantsToExit = false;
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // If this function is async, all the awaited calls in here may get called after the window
            // is closed, and that's bad. But we can't Wait the task to finish as that would freeze the UI.
            // Therefore to fix this, we'll tell Glue to not shut down if this is the first time the user wanted
            // to shut it. Then we'll wait for all tasks to finish and then try again to close it.
            if(!WantsToExit)
            {
                CloseAfterTasks();
                e.Cancel = true;
            }

        }

        private async void CloseAfterTasks()
        {
            ProjectManager.WantsToCloseProject = true;
            WantsToExit = true;
            //MainPanelSplitContainer.ReactToFormClosing();
            
            //EditorData.GlueLayoutSettings.BottomPanelSplitterPosition = MainPanelSplitContainer.SplitterDistance;
            EditorData.GlueLayoutSettings.Maximized = this.WindowState == FormWindowState.Maximized;
            EditorData.GlueLayoutSettings.SaveSettings();

            await TaskManager.Self.WaitForAllTasksFinished();

            // ReactToCloseProject should be called before ReactToGlueClose so that plugins 
            // can react to the glux unloaded before the plugins get disabled.
            MainWpfControl.ReactToCloseProject(true, true);

            PluginManager.ReactToGlueClose();

            GlueCommands.Self.CloseGlue();            
        }
    }
}
