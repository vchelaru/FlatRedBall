using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Elements;
using System.Diagnostics;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Utilities;
using EditorObjects.Parsing;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.Events;
using EditorObjects.Cleaners;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Instructions;
using FlatRedBall.Glue.ContentPipeline;
using FlatRedBall.Glue.Projects;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Navigation;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.TypeConversions;
using System.Drawing;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using System.ServiceModel;
//using GlueWcfServices;
//using Glue.Wcf;
//using FlatRedBall.Glue.Wcf;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Data;
//using System.Management;
using FlatRedBall.Glue.SetVariable;
using Container = EditorObjects.IoC;
using FlatRedBall.Glue.UnreferencedFiles;
using FlatRedBall.Glue.Controls.ProjectSync;
using System.Linq;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using System.Threading.Tasks;
using FlatRedBall.Instructions.Reflection;
using Microsoft.Xna.Framework.Audio;

//using EnvDTE;

namespace Glue
{
    public partial class MainGlueWindow : Form
    {
        public bool HasErrorOccurred = false;
        private System.Windows.Forms.Timer FileWatchTimer;

        private static MainGlueWindow mSelf;

        public static MainGlueWindow Self
        {
            get { return mSelf; }
        }

        public System.ComponentModel.IContainer Components => components;

        public System.Windows.Forms.PropertyGrid PropertyGrid;

        private int NumberOfStoredRecentFiles
        {
            get;
            set;
        }

        public MainGlueWindow()
        {
            mSelf = this;

            InitializeComponent();

            this.FileWatchTimer = new System.Windows.Forms.Timer(this.components);

            this.FileWatchTimer.Enabled = true;
            // the frequency of file change flushes. Reducing this time
            // makes Glue more responsive, but increases the chance of 
            // Glue performing a check mid update like on a git pull.
            // Note that the ChangeInformation also keeps a timer since the last
            // file was added, and will wait mMinimumTimeAfterChangeToReact until 
            // flushing.
            this.FileWatchTimer.Interval = 400;
            this.FileWatchTimer.Tick += new System.EventHandler(this.FileWatchTimer_Tick);

        }

        public void Invoke(Action action)
        {
            this.Invoke((MethodInvoker)delegate
            {
                try
                {
                    action();
                }
                catch(Exception e)
                {
                    if(!IsDisposed)
                    {
                        throw e;
                    }
                    // otherwise, we don't care, they're exiting
                }
            });

        }

        public async Task LoadProject(string projectFileName, InitializationWindow initializationWindow)
        {
            await ProjectLoader.Self.LoadProject(projectFileName, initializationWindow);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                StartUpGlue();
            }
            catch (FileNotFoundException fnfe)
            {
                if (fnfe.ToString().Contains("Microsoft.Xna.Framework.dll"))
                {
                    var message = "Could not load Glue, probably because XNA 4 is not installed. Click OK to go to the XNA runtime page to install the XNA runtime, then run Glue again";
                    MessageBox.Show(message);
                    System.Diagnostics.Process.Start("https://www.microsoft.com/en-us/download/details.aspx?id=20914");
                    this.Close();
                }
                else
                {
                    throw fnfe;
                }
            }
        }

        internal void StartUpGlue()
        {
            // Some stuff can be parallelized.  We're going to run stuff
            // that can be parallelized in parallel, and then block to wait for
            // all tasks to finish when we need to

            AddObjectsToIocContainer();

            AddErrorReporters();

            InitializationWindow initializationWindow = new InitializationWindow();

            // Initialize GlueGui before using it:
            GlueGui.Initialize(mMenu);
            GlueGui.ShowWindow(initializationWindow, this);

            initializationWindow.Message = "Initializing Glue Systems";
            Application.DoEvents();

            // Add Glue.Common
            PropertyValuePair.AdditionalAssemblies.Add(typeof(PlatformSpecificType).Assembly);

            // Monogame:
            PropertyValuePair.AdditionalAssemblies.Add(typeof(SoundEffectInstance).Assembly);

            // Async stuff
            {

                initializationWindow.SubMessage = "Initializing WCF"; Application.DoEvents();
                //TaskManager.Self.AddAsyncTask(() => WcfManager.Self.Initialize(), "Initializing WCF");

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
                    TaskManager.Self.AddAsyncTask(() => 
                        GlueGui.ShowException("Could not load assemblies - you probably need to rebuild Glue.", "Error", excep),
                        "Show error message about not being able to load assemblies");

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

            initializationWindow.SubMessage = "Initializing SearchBar"; Application.DoEvents();
            SearchBarHelper.Initialize(SearchTextbox);
            initializationWindow.SubMessage = "Initializing Navigation Stack"; Application.DoEvents();
            TreeNodeStackManager.Self.Initialize(NavigateBackButton, NavigateForwardButton);






            initializationWindow.Message = "Loading Glue Settings"; Application.DoEvents();
            // We need to load the glue settings before loading the plugins so that we can 
            // shut off plugins according to settings
            LoadGlueSettings(initializationWindow);


            // Initialize before loading GlueSettings;
            // Also initialize before loading plugins so that plugins
            // can access the standard ATIs
#if GLUE
            string startupPath =
                FileManager.GetDirectory(System.Reflection.Assembly.GetExecutingAssembly().Location);
#else
                string startupPath = FileManager.StartupPath;
#endif
            AvailableAssetTypes.Self.Initialize(startupPath);

            initializationWindow.Message = "Loading Plugins"; Application.DoEvents();
            List<string> pluginsToIgnore = new List<string>();
            if (GlueState.Self.CurrentPluginSettings != null)
            {
                pluginsToIgnore = GlueState.Self.CurrentPluginSettings.PluginsToIgnore;
            }

            PluginManager.SetTabs(tcTop, tcBottom, tcLeft, tcRight, MainTabControl, toolbarControl1);

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
                // InitializeElementViewWindow needs to happen before LoadGlueSettings
                InitializeElementViewWindow();

                Application.DoEvents();
                // Gotta do this too before Loading Glue Settings
                ProjectManager.Initialize();

                initializationWindow.Message = "Loading Project";
                Application.DoEvents();

                // LoadSettings before loading projects
                EditorData.LoadPreferenceSettings();

                VisibilityManager.ReactivelySetItemViewVisibility();

                while (TaskManager.Self.AreAllAsyncTasksDone == false)
                {
                    System.Threading.Thread.Sleep(100);
                }
                LoadProjectConsideringSettingsAndArgs(initializationWindow);

                // This needs to happen after loading the project:
                ShareUiReferences(PluginCategories.ProjectSpecific);

                Application.DoEvents();
                EditorData.FileAssociationSettings.LoadSettings();

                EditorData.LoadGlueLayoutSettings();

                rightPanelContainer.Panel2MinSize = 125;
                try
                {
                    leftPanelContainer.SplitterDistance = EditorData.GlueLayoutSettings.LeftPanelSplitterPosition;
                    topPanelContainer.SplitterDistance = EditorData.GlueLayoutSettings.TopPanelSplitterPosition;
                    rightPanelContainer.SplitterDistance = EditorData.GlueLayoutSettings.RightPanelSplitterPosition;
                    bottomPanelContainer.SplitterDistance = EditorData.GlueLayoutSettings.BottomPanelSplitterPosition;
                }
                catch
                {
                    // do nothing
                }
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
        }

        private void AddErrorReporters()
        {
            EditorObjects.IoC.Container.Get<List<IErrorReporter>>()
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
            EditorObjects.IoC.Container.Set(new EntitySaveSetVariableLogic());
            EditorObjects.IoC.Container.Set(new ScreenSaveSetVariableLogic());
            EditorObjects.IoC.Container.Set(new GlobalContentSetVariableLogic());
            EditorObjects.IoC.Container.Set(new PluginUpdater());

            EditorObjects.IoC.Container.Set<IGlueState>(GlueState.Self);
            EditorObjects.IoC.Container.Set<IGlueCommands>(GlueCommands.Self);

            EditorObjects.IoC.Container.Set<List<IErrorReporter>>(new List<IErrorReporter>());
        }

        private void LoadProjectConsideringSettingsAndArgs(InitializationWindow initializationWindow)
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
                LoadProject(csprojToLoad, initializationWindow);
            }
        }

        private void ShareUiReferences(PluginCategories pluginCategories)
        {
            PluginManager.ShareMenuStripReference(mMenu, pluginCategories);
            PluginManager.ShareTopTabReference(tcTop, pluginCategories);
            PluginManager.ShareLeftTabReference(tcLeft, pluginCategories);
            PluginManager.ShareBottomTabReference(tcBottom, pluginCategories);
            PluginManager.ShareRightTabReference(tcRight, pluginCategories);
            PluginManager.ShareCenterTabReference(MainTabControl, pluginCategories);

            PluginManager.PrintPreInitializeOutput();
        }

        private void InitializeElementViewWindow()
        {
            TreeNode entityNode = new TreeNode("Entities");
            TreeNode screenNode = new TreeNode("Screens");
            TreeNode globalContentNode = new TreeNode("Global Content Files");

            ElementTreeView.Nodes.Add(entityNode);
            ElementTreeView.Nodes.Add(screenNode);
            ElementTreeView.Nodes.Add(globalContentNode);

            ElementViewWindow.Initialize(ElementTreeView, entityNode, screenNode, globalContentNode);
        }

        private void LoadGlueSettings(InitializationWindow initializationWindow)
        {
            string settingsFileLocation = GlueSettingsSave.SettingsFileName;
            if (FileManager.FileExists(settingsFileLocation))
            {
                GlueSettingsSave settingsSave = null;

                bool didErrorOccur = false;

                try
                {
                    settingsSave = FileManager.XmlDeserialize<GlueSettingsSave>(settingsFileLocation);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Error loading your settings file which is located at\n\n" +
                        settingsFileLocation + "\n\nError details:\n\n" + e.ToString());
                    didErrorOccur = true;
                }
                
                if (!didErrorOccur)
                {
                    ProjectManager.GlueSettingsSave = settingsSave;

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
                    this.Height = settingsSave.WindowHeight > 100 ? settingsSave.WindowHeight : 480;
                    this.Width = settingsSave.WindowWidth > 100 ? settingsSave.WindowWidth : 640;
                    this.rightPanelContainer.SplitterDistance = settingsSave.MainSplitterDistance > 0 ? settingsSave.MainSplitterDistance : 450;
                }
            }
            else
            {
                ProjectManager.GlueSettingsSave.Save();
            }
        }

        private void mElementTreeView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                RightClickHelper.PopulateRightClickItems(ElementTreeView.GetNodeAt(e.X, e.Y));
            }
        }

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void UpdateGlueSettings()
        {
            GlueSettingsSave save = ProjectManager.GlueSettingsSave;

            string lastFileName = null;

            if (ProjectManager.ProjectBase != null)
            {
                lastFileName = ProjectManager.ProjectBase.FullFileName;
            }

            save.LastProjectFile = lastFileName;

            var glueExeFileName = ProjectLoader.GetGlueExeLocation();
            var foundItem = save.GlueLocationSpecificLastProjectFiles
                .FirstOrDefault(item => item.GlueFileName == glueExeFileName);

            var alreadyIsListed = foundItem != null;

            if(!alreadyIsListed)
            {
                foundItem = new ProjectFileGlueFilePair();
                save.GlueLocationSpecificLastProjectFiles.Add(foundItem);
            }
            foundItem.GlueFileName = glueExeFileName;
            foundItem.GameProjectFileName = lastFileName;
            
            // set up the positions of the window
            save.WindowLeft = this.Left;
            save.WindowTop = this.Top;
            save.WindowHeight = this.Height;
            save.WindowWidth = this.Width;
            save.MainSplitterDistance = this.rightPanelContainer.SplitterDistance;
            save.StoredRecentFiles = this.NumberOfStoredRecentFiles;

            GlueCommands.Self.GluxCommands.SaveSettings();
        }

        private void mElementTreeView_DoubleClick(object sender, EventArgs e)
        {
            Point point = new Point(Cursor.Position.X, Cursor.Position.Y);

            Point topLeft = ElementTreeView.PointToScreen(new Point( 0, 0));

            Point relative = new Point(point.X - topLeft.X, point.Y - topLeft.Y);

            var node = this.ElementTreeView.GetNodeAt(relative);
            var hitTestResult = ElementTreeView.HitTest(relative);
            if (node != null && 
                (hitTestResult.Location == TreeViewHitTestLocations.Image || 
                hitTestResult.Location == TreeViewHitTestLocations.Label))
            {
                ElementViewWindow.ElementDoubleClicked();
            }
        }

        public static void CloseProject(bool shouldSave, bool isExiting)
        {
            // Let's set this to true so all tasks can end
            ProjectManager.WantsToClose = true;

            // But give them a chance to end...
            while (TaskManager.Self.AreAllAsyncTasksDone == false)
            {
                // We want to wait until all tasks are done, but
                // if the task is to reload, we can continue or else
                // we'll have a deadlock
                var canContinue = TaskManager.Self.CurrentTask == UpdateReactor.ReloadingProjectDescription ||
                    TaskManager.Self.CurrentTask.StartsWith("Reacting to changed file") && TaskManager.Self.CurrentTask.EndsWith(".glux");

                if(canContinue)
                {
                    break;
                }
                else
                {
                    System.Threading.Thread.Sleep(50);

                    // pump events
                    Application.DoEvents();
                }

            }


            if (shouldSave)
            {
                if (ProjectManager.ProjectBase != null && !string.IsNullOrEmpty(ProjectManager.ProjectBase.FullFileName))
                {
                    GlueCommands.Self.ProjectCommands.SaveProjectsImmediately();
                    Self.UpdateGlueSettings();
                }
            }


            ProjectManager.UnloadProject(isExiting);

            #region Clear existing nodes and re-add base nodes
            
            // Select null so plugins deselect:
            Self.ElementTreeView.SelectedNode = null;

            // This only matters if we're not exiting the app:
            if(isExiting == false)
            {

                ElementViewWindow.AfterSelect();

                Self.ElementTreeView.Nodes.Clear();

                Self.InitializeElementViewWindow();
            }



            #endregion



            #region Clear PropertyGrid and Code window
            MainGlueWindow.Self.PropertyGrid.SelectedObject = null;
            MainGlueWindow.Self.CodePreviewTextBox.Clear();
            #endregion

            MainGlueWindow.Self.Text = "FlatRedBall Glue";
            ProjectManager.WantsToClose = false;

        }

        private void FileWatchTimer_Tick(object sender, EventArgs e)
        {
            if(ProjectManager.ProjectBase != null && !string.IsNullOrEmpty(ProjectManager.ProjectBase.FullFileName))
                FileWatchManager.Flush();
        }

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            ProjectManager.WantsToClose = true;
            EditorData.GlueLayoutSettings.LeftPanelSplitterPosition = leftPanelContainer.SplitterDistance;
            EditorData.GlueLayoutSettings.TopPanelSplitterPosition = topPanelContainer.SplitterDistance;
            EditorData.GlueLayoutSettings.RightPanelSplitterPosition = rightPanelContainer.SplitterDistance;
            EditorData.GlueLayoutSettings.BottomPanelSplitterPosition = bottomPanelContainer.SplitterDistance;
            EditorData.GlueLayoutSettings.Maximized = this.WindowState == FormWindowState.Maximized;
            EditorData.GlueLayoutSettings.SaveSettings();

            await TaskManager.Self.WaitForAllTasksFinished();

            PluginManager.ReactToGlueClose();
            CloseProject(true, true);
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void ElementTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            ElementViewWindow.AfterSelect();

        }

        private void ElementTreeView_KeyPress(object sender, KeyPressEventArgs e)
        {
            // copy, paste, ctrl c, ctrl v, ctrl + c, ctrl + v, ctrl+c, ctrl+v
            #region Copy ( (char)3 )

            if (e.KeyChar == (char)3)
            {
                e.Handled = true;

                if(GlueState.Self.CurrentNamedObjectSave != null)
                {
                    GlueState.Self.Clipboard.CopiedObject = GlueState.Self.CurrentNamedObjectSave;
                }
                else if (GlueState.Self.CurrentEntitySave != null && MainGlueWindow.Self.ElementTreeView.SelectedNode is EntityTreeNode)
                {
                    // copy ElementSave
                    GlueState.Self.Clipboard.CopiedObject = GlueState.Self.CurrentEntitySave;
                }
                else if (GlueState.Self.CurrentScreenSave != null && MainGlueWindow.Self.ElementTreeView.SelectedNode is ScreenTreeNode)
                {
                    // copy ScreenSave
                    GlueState.Self.Clipboard.CopiedObject = GlueState.Self.CurrentScreenSave;
                }


            }

            #endregion

            #region Paste ( (char)22 )

            else if (e.KeyChar == (char)22)
            {
                e.Handled = true;

                // Vic says: Currently pasting does NOT bring over any non-generated code.  This will
                // need to be fixed eventually

                // Paste CTRL+V stuff



                if (GlueState.Self.Clipboard.CopiedEntity != null)
                {
                    MessageBox.Show("Pasted Entities will not copy any code that you have written in custom functions.");

                    EntitySave newEntitySave = GlueState.Self.Clipboard.CopiedEntity.Clone();

                    newEntitySave.Name += "Copy";

                    string oldFile = newEntitySave.Name + ".cs";
                    string oldGeneratedFile = newEntitySave.Name + ".Generated.cs";
                    string newFile = newEntitySave.Name + "Copy.cs";
                    string newGeneratedFile = newEntitySave.Name + "Copy.Generated.cs";

                    // Not sure why we are adding here - the ProjectManager.AddEntity takes care of it.
                    //ProjectManager.GlueProjectSave.Entities.Add(newEntitySave);
                    GlueCommands.Self.GluxCommands.EntityCommands.AddEntity(newEntitySave);

                    GlueState.Self.Find.EntityTreeNode(newEntitySave).RefreshTreeNodes();
                }
                else if (GlueState.Self.Clipboard.CopiedScreen != null)
                {
                    MessageBox.Show("Pasted Screens will not copy any code that you have written in custom functions.");

                    ScreenSave newScreenSave = GlueState.Self.Clipboard.CopiedScreen.Clone();

                    newScreenSave.Name += "Copy";

                    string oldFile = newScreenSave.Name + ".cs";
                    string oldGeneratedFile = newScreenSave.Name + ".Generated.cs";
                    string newFile = newScreenSave.Name + "Copy.cs";
                    string newGeneratedFile = newScreenSave.Name + "Copy.Generated.cs";

                    // Not sure why we are adding here - AddScreen takes care of it.

                    GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen(newScreenSave);

                    GlueState.Self.Find.ScreenTreeNode(newScreenSave).RefreshTreeNodes();
                }
                else if(GlueState.Self.Clipboard.CopiedNamedObject != null)
                {
                    // todo: implement this, using duplicate
                }
            }

            #endregion

            else if (e.KeyChar == '\r')
            {
                // treat it like a double-click
                e.Handled = true;
            }

        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            int m = 3;
        }

        private void ElementTreeView_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void ElementTreeView_DragOver(object sender, DragEventArgs e)
        {
            ElementViewWindow.DragOver(sender, e);
        }

        private void ElementTreeView_DragDrop(object sender, DragEventArgs e)
        {
            ElementViewWindow.DragDrop(sender, e);
        }

        private void ElementTreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            // Get the tree.
            TreeView tree = (TreeView)sender;

            // Get the node underneath the mouse.
            TreeNode node = e.Item as TreeNode;
            tree.SelectedNode = node;

            // Start the drag-and-drop operation with a cloned copy of the node.
            if (node != null)
            {
                ElementViewWindow.TreeNodeDraggedOff = node;

                TreeNode targetNode = null;
                targetNode = ElementTreeView.SelectedNode;
                ElementViewWindow.ButtonUsed = e.Button;

                //ElementTreeView_DragDrop(node, DragDropEffects.Move | DragDropEffects.Copy);
                tree.DoDragDrop(node, DragDropEffects.Move | DragDropEffects.Copy);
            }
        }

        private void ElementTreeView_MouseHover(object sender, EventArgs e)
        {

        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
        }
        
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            SearchBarHelper.SearchBarTextChange();

        }

        private void SearchListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //SearchBarHelper.SearchListBoxIndexChanged();

        }

        private void SearchTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            SearchBarHelper.TextBoxKeyDown(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if(HotkeyManager.Self.TryHandleKeys(keyData))
            {
                return true;
            }
            else
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        private void ElementTreeView_KeyDown(object sender, KeyEventArgs e)
        {

            #region Delete key

            if (e.KeyCode == Keys.Delete)
            {
                RightClickHelper.RemoveFromProjectToolStripMenuItem();
            }
            #endregion

            else if (e.KeyCode == Keys.Enter)
            {
                ElementViewWindow.ElementDoubleClicked();
                e.Handled = true;
            }
        }

        private void ControlAddedToRightView(object sender, ControlEventArgs e)
        {
            SplitContainer parent = (SplitContainer)((Control)sender).Parent.Parent;

            parent.Panel2Collapsed = false;


        }

        private void ControlRemovedFromRightView(object sender, ControlEventArgs e)
        {
            SplitContainer parent = (SplitContainer)((Control)sender).Parent.Parent;
            TabControlEx tc = (TabControlEx)sender;
            bool show = tc.TabCount > 1;

            if (show)
                parent.Panel2Collapsed = false;
            else
                parent.Panel2Collapsed = true;

        }

        private void tcPanel1_ControlAdded(object sender, ControlEventArgs e)
        {
            SplitContainer parent = (SplitContainer)((Control)sender).Parent.Parent;

            parent.Panel1Collapsed = false;
        }

        private void tcPanel1_ControlRemoved(object sender, ControlEventArgs e)
        {
            SplitContainer parent = (SplitContainer)((Control)sender).Parent.Parent;
            TabControlEx tc = (TabControlEx)sender;
            bool show = tc.TabCount > 1;

            if (show)
                parent.Panel1Collapsed = false;
            else
                parent.Panel1Collapsed = true;

        }

        private void tcPanel2_ControlAdded(object sender, ControlEventArgs e)
        {
            SplitContainer parent = (SplitContainer)((Control)sender).Parent.Parent;

            parent.Panel2Collapsed = false;
        }

        private void tcPanel2_ControlRemoved(object sender, ControlEventArgs e)
        {
            SplitContainer parent = (SplitContainer)((Control)sender).Parent.Parent;
            TabControlEx tc = (TabControlEx)sender;
            bool show = tc.TabCount > 1;

            if (show)
                parent.Panel2Collapsed = false;
            else
                parent.Panel2Collapsed = true;

        }

        private void MsProcessesItemAdded(object sender, ToolStripItemEventArgs e)
        {
            if (msProcesses.Items.Count > 0)
                msProcesses.BeginInvoke(new EventHandler(delegate { msProcesses.Show(); }));
        }

        private void MsProcessesItemRemoved(object sender, ToolStripItemEventArgs e)
        {
            if (msProcesses.Items.Count <= 0)
                msProcesses.BeginInvoke(new EventHandler(delegate { msProcesses.Hide(); }));
        }

        private void SearchListBox_Click(object sender, EventArgs e)
        {
            SearchBarHelper.SearchListBoxIndexChanged();
        }

        private void ElementTreeView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void rightPanelContainer_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void ElementTreeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (this.ElementTreeView.SelectedNode != null

                // August 31 2019
                // If the user drag+dropped off a tree node, then as they move over
                // other nodes they will get selected. We don't want to record that as
                // a movement.
                // Actually this value doesn't get nulled out when dropping a node, so 
                // can't use this now. Oh well, I won't bother with fixing this for now, 
                // I thought it would be a quick fix...
                // && ElementViewWindow.TreeNodeDraggedOff == null
                )
            {
                TreeNodeStackManager.Self.Push(ElementTreeView.SelectedNode);
            }
        }

        private void SearchTextbox_Leave(object sender, EventArgs e)
        {
            SearchBarHelper.TextBoxLeave(SearchTextbox);
        }

        private void NavigateBackButton_Click(object sender, EventArgs e)
        {
            TreeNodeStackManager.Self.GoBack();
        }

        private void NavigateForwardButton_Click(object sender, EventArgs e)
        {
            TreeNodeStackManager.Self.GoForward();
        }
    }
}
