using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.SaveClasses;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using System.IO;
using Ionic.Zip;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using FlatRedBall.IO;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Reflection;
using System.ComponentModel;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Content.Instructions;

namespace FlatRedBall.Glue.Plugins
{
    public class PluginManager : PluginManagerBase
    {
        #region Interface Lists

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<PluginBase> ImportedPlugins { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<ITreeViewRightClick> TreeViewPlugins { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<IStateChange> StateChangePlugins { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<IPropertyGridRightClick> PropertyGridRightClickPlugins { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<INewObject> NewObjectPlugins { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<IOpenVisualStudio> OpenVisualStudioPlugins { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<ITreeItemSelect> TreeItemSelectPlugins { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<INewFile> NewFilePlugins { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<IMenuStripPlugin> MenuStripPlugins { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<ITopTab> TopTabPlugins { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<ILeftTab> LeftTabPlugins { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<IBottomTab> BottomTabPlugins { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<IRightTab> RightTabPlugins { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<ICenterTab> CenterTabPlugins { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<IGluxLoad> GluxLoadPlugins { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<INamedObject> NamedObjectsPlugins { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<ICurrentElement> CurrentElementPlugins { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<IPropertyChange> PropertyChangePlugins { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<ICodeGeneratorPlugin> CodeGeneratorPlugins { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<IContentFileChange> ContentFileChangePlugins { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<IOutputReceiver> OutputReceiverPlugins { get; set; }

        #endregion


        private static MenuStrip mMenuStrip;
        private static TabControl mTopTab;
        private static TabControl mLeftTab;
        private static TabControl mBottomTab;
        private static TabControl mRightTab;
        private static TabControl mCenterTab;


        // not sure who should provide access to these tabs, but
        // we want to make it easier to get access to them instead
        // of having to explicitly define plugin types tied to certain
        // sides:
        public static TabControl TopTab
        {
            get { return mTopTab; }
        }
        public static TabControl LeftTab
        {
            get { return mLeftTab; }
        }
        public static TabControl BottomTab
        {
            get { return mBottomTab; }
        }
        public static TabControl RightTab
        {
            get { return mRightTab; }
        }
        public static TabControl CenterTab
        {
            get { return mCenterTab; }
        }
        public static System.Windows.Controls.ToolBarTray ToolBarTray
        {
            get;
            private set;
        }

        GlueCommands mGlueCommands = new GlueCommands();
        GlueState mGlueState = new GlueState();

        static StringBuilder mPreInitializeOutput = new StringBuilder();
        static StringBuilder mPreInitializeError = new StringBuilder();

        private static bool mHandleExceptions = true;
        public static bool HandleExceptions
        {
            get { return mHandleExceptions; }
            set { mHandleExceptions = value; }
        }

        #region Exported objects

        [Export("GlueProjectSave")]
        public GlueProjectSave GlueProjectSave
        {
            get
            {
                return ProjectManager.GlueProjectSave;
            }
        }

        [Export("GlueCommands")]
        public IGlueCommands GlueCommands
        {
            get
            {
                return mGlueCommands;
            }
        }

        [Export("GlueState")]
        public IGlueState GlueState
        {
            get
            {
                return mGlueState;
            }
        }

        #endregion

        #region Methods

        public PluginManager(bool global)
            : base(global)
        {
            // This forces the RenderingLibrary.dll to be loaded
            // so we don't allow plugins to load their own versions.
            // We're just accessing the class, we're not going to actually use it.
            RenderingLibrary.IPositionedSizedObject test = null ;
            // ... and we should do the same thing with ToolsUtilities
            var throwaway = ToolsUtilities.FileManager.GetExtension("something.png");
            // ... and XNA+winforms
            var anotherThrowaway = typeof(XnaAndWinforms.GraphicsDeviceControl);
            // .. specialized xna controls:
            var throwaway2 = typeof(FlatRedBall.SpecializedXnaControls.TimeManager);
            var throwaway3 = typeof(FlatRedBall.SpecializedXnaControls.Input.CameraPanningLogic);

            var throwaway4 = typeof(InputLibrary.Keyboard);
            var throwaway5 = typeof(RenderingLibrary.Camera);
            var throwaway6 = typeof(ToolsUtilities.FileManager);
            var throwaway7 = typeof(XnaAndWinforms.GraphicsDeviceControl);
        }

        protected override void StartAllPlugins(List<string> pluginsToIgnore = null)
        {
            var allPlugins = new List<IEnumerable<IPlugin>>
            {
                TreeViewPlugins, PropertyGridRightClickPlugins, NewObjectPlugins,
                OpenVisualStudioPlugins, TreeItemSelectPlugins, NewFilePlugins, MenuStripPlugins,
                TopTabPlugins, LeftTabPlugins, BottomTabPlugins, RightTabPlugins, CenterTabPlugins,
                GluxLoadPlugins, NamedObjectsPlugins, PropertyChangePlugins, CodeGeneratorPlugins,
                ContentFileChangePlugins, OutputReceiverPlugins, CurrentElementPlugins
            };

            foreach (var pluginList in allPlugins)
            {
                foreach (var plugin in pluginList)
                    StartupPlugin(plugin);
            }

            foreach (var plugin in ImportedPlugins)
            {
                StartupPlugin(plugin);

                // did it fail?
                var container = mPluginContainers[plugin];
                if(container.FailureException != null)
                {
                    PluginManager.ReceiveError(container.FailureException.ToString());
                }
            }

            if (pluginsToIgnore != null)
            {
                foreach (var ignored in pluginsToIgnore)
                {
                    AddDisabledPlugin(ignored);
                }
            }
        }

        protected override void InstantiateAllListsAsEmpty()
        {
            ImportedPlugins = new List<PluginBase>();
            TreeViewPlugins = new List<ITreeViewRightClick>();
            PropertyGridRightClickPlugins = new List<IPropertyGridRightClick>();
            NewObjectPlugins = new List<INewObject>();
            OpenVisualStudioPlugins = new List<IOpenVisualStudio>();
            TreeItemSelectPlugins = new List<ITreeItemSelect>();
            NewFilePlugins = new List<INewFile>();
            MenuStripPlugins = new List<IMenuStripPlugin>();
            TopTabPlugins = new List<ITopTab>();
            LeftTabPlugins = new List<ILeftTab>();
            BottomTabPlugins = new List<IBottomTab>();
            RightTabPlugins = new List<IRightTab>();
            CenterTabPlugins = new List<ICenterTab>();
            GluxLoadPlugins = new List<IGluxLoad>();
            NamedObjectsPlugins = new List<INamedObject>();
            CurrentElementPlugins = new List<ICurrentElement>();
            PropertyChangePlugins = new List<IPropertyChange>();
            CodeGeneratorPlugins = new List<ICodeGeneratorPlugin>();
            ContentFileChangePlugins = new List<IContentFileChange>();
            OutputReceiverPlugins = new List<IOutputReceiver>();
        }

        internal static void Initialize(bool isStartup, List<string> pluginsToIgnore = null)
        {
            if (isStartup)
            {
                CopyIntalledPluginsToRunnableLocation();
                UninstallPlugins();
            }

            if (mGlobalInstance == null)
            {
                mGlobalInstance = new PluginManager(true);
                mGlobalInstance.LoadPlugins(@"FRBDK\Plugins", pluginsToIgnore);

                foreach (var output in mGlobalInstance.CompileOutput)
                {
                    ReceiveOutput(output);
                }
                foreach (var output in mGlobalInstance.CompileErrors)
                {
                    ReceiveError(output);
                }
            }

            if (mProjectInstance != null)
            {
                foreach (IPlugin plugin in ((PluginManager)mProjectInstance).mPluginContainers.Keys)
                {
                    ShutDownPlugin(plugin, PluginShutDownReason.GlueShutDown);
                }
            }

            mProjectInstance = new PluginManager(false);

            mInstances.Clear();
            mInstances.Add(mGlobalInstance);
            mInstances.Add(mProjectInstance);


            mProjectInstance.LoadPlugins(@"FRBDK\Plugins");

            foreach (var error in mProjectInstance.CompileErrors)
            {
                GlueGui.ShowException(error, "Plugin Error", new Exception(error));
            }
        }

        private static void UninstallPlugins()
        {
            if (File.Exists(UninstallPluginWindow.UninstallPluginFile))
            {
                string line;
                // Read the file and display it line by line.
                var file = new StreamReader(UninstallPluginWindow.UninstallPluginFile);
                while ((line = file.ReadLine()) != null)
                {
                    try
                    {
                        Directory.Delete(line, true);
                    }
                    catch (Exception e)
                    {
                        // Tolerate this
                        // do nothing (for now)
                    }
                }

                file.Close();

                try
                {
                    File.Delete(UninstallPluginWindow.UninstallPluginFile);
                }
                catch (Exception e)
                {
                    // Tolerate this, don't crash Glue
                }
            }
        }

        private static void CopyIntalledPluginsToRunnableLocation()
        {
            string installedDirectory = FileManager.UserApplicationDataForThisApplication + "InstalledPlugins\\";
            string pluginDirectory = FileManager.UserApplicationData + @"FRBDK\Plugins\";

            // Making Glue startup not so verbose:
            //PluginManager.ReceiveOutput("Looking to copy plugins from " + installedDirectory);

            if (Directory.Exists(installedDirectory))
            {
                //PluginManager.ReceiveOutput("Install directory found");
                var directories = Directory.GetDirectories(installedDirectory);

                foreach (var directory in directories)
                {
                    string directoryName = FileManager.RemovePath(directory);

                    try
                    {
                        // Copy this entire directory into the plugin folder, then delete it
                        FileManager.CopyDirectory(directory, pluginDirectory + directoryName, true);
                        PluginManager.ReceiveOutput("Copying from " + directory + " to " + pluginDirectory + directoryName);
                        FileManager.DeleteDirectory(directory);
                    }
                    catch (UnauthorizedAccessException uae)
                    {
                        GlueGui.ShowException("Glue does not have permission to install the plugin - please restart Glue as an administrator.", "Error installing plugin", uae);
                    }
                    catch (Exception e)
                    {
                        GlueGui.ShowException("Error finishing installation for plugin " + directoryName, "Error installing plugin", e);
                    }
                }
            }
            else
            {
                //PluginManager.ReceiveOutput("Plugin install directory not found, so not installing any plugins");

            }
        }

        protected override void AddDirectoriesForInstance(List<string> pluginDirectories)
        {
            if (ProjectManager.GlueProjectFileName != null && Directory.Exists(FileManager.GetDirectory(ProjectManager.GlueProjectFileName) + "Plugins"))
            {
                pluginDirectories.AddRange(Directory.GetDirectories(FileManager.GetDirectory(ProjectManager.GlueProjectFileName) + "Plugins"));
            }
        }

        internal static void AddNewFileOptions(NewFileWindow newFileWindow)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                foreach (INewFile plugin in pluginManager.NewFilePlugins)
                {
                    PluginContainer container = pluginManager.mPluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        INewFile plugin1 = plugin;
                        PluginCommand(() =>
                                          {
                                              plugin1.AddNewFileOptions(newFileWindow);
                                          }, container, "Failed in AddNewFileOptions");
                    }
                }

                // Execute the new style plugins
                var plugins = pluginManager.ImportedPlugins.Where(x => x.AddNewFileOptionsHandler != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginBase plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.AddNewFileOptionsHandler(newFileWindow);
                            }, container, "Failed in AddNewFileOptions");
                    }
                }
            }
        }

        internal static string CreateNewFile(AssetTypeInfo assetTypeInfo, object extraData, string directory, string name)
        {
            string createdFile = null;
            bool created = false;

            foreach (PluginManager pluginManager in mInstances)
            {
                if (created)
                {
                    break;
                }

                foreach (INewFile plugin in pluginManager.NewFilePlugins)
                {
                    PluginContainer container = pluginManager.mPluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        var exit = false;

                        INewFile plugin1 = plugin;
                        PluginCommand(() =>
                                          {
                                              if (plugin1.CreateNewFile(assetTypeInfo, extraData, directory, name, out createdFile))
                                              {
                                                  created = true;
                                                  exit = true;
                                              }
                                          }, container, "Failed in CreateNewFile");

                        if (exit) break;
                    }
                }

                // Execute the new style plugins
                if (!created)
                {
                    var plugins = pluginManager.ImportedPlugins.Where(x => x.CreateNewFileHandler != null);
                    foreach (var plugin in plugins)
                    {
                        var container = pluginManager.mPluginContainers[plugin];
                        if (container.IsEnabled)
                        {
                            PluginBase plugin1 = plugin;
                            bool exit = false;
                            PluginCommand(() =>
                                              {
                                                  if (plugin1.CreateNewFileHandler(assetTypeInfo, extraData, directory, name, out createdFile))
                                                  {
                                                      created = true;
                                                      exit = true;
                                                  }
                                              }, container, "Failed in CreateNewFile");

                            if (exit) break;
                        }
                    }
                }
            }

            return createdFile;
        }

        internal static void ShareMenuStripReference(MenuStrip menuStrip, PluginCategories pluginCategories)
        {
            mMenuStrip = menuStrip;

            foreach (PluginManager pluginManager in mInstances)
            {
                if (ShouldProcessPluginManager(pluginCategories, pluginManager))
                {
                    foreach (IMenuStripPlugin plugin in pluginManager.MenuStripPlugins)
                    {
                        PluginContainer container = pluginManager.mPluginContainers[plugin];

                        if (container.IsEnabled)
                        {
                            IMenuStripPlugin plugin1 = plugin;
                            PluginCommand(() =>
                                              {
                                                  plugin1.InitializeMenu(menuStrip);
                                              }, container, "Failed in InitializeMenu");
                        }
                    }

                    // Execute the new style plugins
                    var plugins = pluginManager.ImportedPlugins.Where(x => x.InitializeMenuHandler != null);
                    foreach (var plugin in plugins)
                    {
                        var container = pluginManager.mPluginContainers[plugin];
                        if (container.IsEnabled)
                        {
                            PluginBase plugin1 = plugin;
                            PluginCommand(() =>
                                              {
                                                  plugin1.InitializeMenuHandler(menuStrip);
                                              }, container, "Failed in InitializeMenu");
                        }
                    }
                }
            }
        }

        internal static void SetTabs(TabControl top, TabControl bottom, TabControl left, TabControl right, TabControl center, ToolbarControl toolbar)
        {
            mTopTab = top;
            mLeftTab = left;
            mRightTab = right;
            mBottomTab = bottom;
            mCenterTab = center;

            ToolBarTray = toolbar.ToolBarTray;
        }

        internal static void ShareTopTabReference(TabControl tabControl, PluginCategories pluginCategories)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                if (ShouldProcessPluginManager(pluginCategories, pluginManager))
                {
                    foreach (ITopTab plugin in pluginManager.TopTabPlugins)
                    {
                        PluginContainer container = pluginManager.mPluginContainers[plugin];

                        if (container.IsEnabled)
                        {
                            ITopTab plugin1 = plugin;
                            PluginCommand(() =>
                                              {
                                                  plugin1.InitializeTab(tabControl);
                                              }, container, "Failed in InitializeTab");
                        }
                    }

                    // Execute the new style plugins
                    var plugins = pluginManager.ImportedPlugins.Where(x => x.InitializeTopTabHandler != null);
                    foreach (var plugin in plugins)
                    {
                        var container = pluginManager.mPluginContainers[plugin];
                        if (container.IsEnabled)
                        {
                            PluginBase plugin1 = plugin;
                            PluginCommand(() =>
                                              {
                                                  plugin1.InitializeTopTabHandler(tabControl);
                                              }, container, "Failed in InitializeTab");
                        }
                    }
                }
            }
        }

        internal static void ShareLeftTabReference(TabControl tabControl, PluginCategories pluginCategories)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                if (ShouldProcessPluginManager(pluginCategories, pluginManager))
                {
                    foreach (ILeftTab plugin in pluginManager.LeftTabPlugins)
                    {
                        PluginContainer container = pluginManager.mPluginContainers[plugin];

                        if (container.IsEnabled)
                        {
                            ILeftTab plugin1 = plugin;
                            PluginCommand(() =>
                                              {
                                                  plugin1.InitializeTab(tabControl);
                                              }, container, "Failed in InitializeTab");
                        }
                    }

                    // Execute the new style plugins
                    var plugins = pluginManager.ImportedPlugins.Where(x => x.InitializeLeftTabHandler != null);
                    foreach (var plugin in plugins)
                    {
                        var container = pluginManager.mPluginContainers[plugin];
                        if (container.IsEnabled)
                        {
                            PluginBase plugin1 = plugin;
                            PluginCommand(() =>
                                              {
                                                  plugin1.InitializeLeftTabHandler(tabControl);
                                              }, container, "Failed in InitializeTab");
                        }
                    }
                }
            }
        }

        internal static void ShareBottomTabReference(TabControl tabControl, PluginCategories pluginCategories)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                if (ShouldProcessPluginManager(pluginCategories, pluginManager))
                {
                    foreach (IBottomTab plugin in pluginManager.BottomTabPlugins)
                    {
                        PluginContainer container = pluginManager.mPluginContainers[plugin];

                        if (container.IsEnabled)
                        {
                            IBottomTab plugin1 = plugin;
                            PluginCommand(() =>
                                {
                                    plugin1.InitializeTab(tabControl);
                                }, container, "Failed in InitializeTab");
                        }
                    }

                    // Execute the new style plugins
                    var plugins = pluginManager.ImportedPlugins.Where(x => x.InitializeBottomTabHandler != null);
                    foreach (var plugin in plugins)
                    {
                        var container = pluginManager.mPluginContainers[plugin];
                        if (container.IsEnabled)
                        {
                            PluginBase plugin1 = plugin;
                            PluginCommand(() =>
                                {
                                    plugin1.InitializeBottomTabHandler(tabControl);
                                }, container, "Failed in InitializeTab");
                        }
                    }
                }
            }
        }

        internal static void ShareRightTabReference(TabControl tabControl, PluginCategories pluginCategories)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                if (ShouldProcessPluginManager(pluginCategories, pluginManager))
                {
                    foreach (IRightTab plugin in pluginManager.RightTabPlugins)
                    {
                        PluginContainer container = pluginManager.mPluginContainers[plugin];

                        if (container.IsEnabled)
                        {
                            IRightTab plugin1 = plugin;
                            PluginCommand(() =>
                            {
                                plugin1.InitializeTab(tabControl);
                            },container, "Failed in InitializeTab");
                        }
                    }

                    // Execute the new style plugins
                    var plugins = pluginManager.ImportedPlugins.Where(x => x.InitializeRightTabHandler != null);
                    foreach (var plugin in plugins)
                    {
                        var container = pluginManager.mPluginContainers[plugin];
                        if (container.IsEnabled)
                        {
                            PluginBase plugin1 = plugin;
                            PluginCommand(() =>
                                {
                                    plugin1.InitializeRightTabHandler(tabControl);
                                }, container, "Failed in InitializeTab");
                        }
                    }
                }
            }
        }

        internal static void ShareCenterTabReference(TabControl tabControl, PluginCategories pluginCategories)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                if (ShouldProcessPluginManager(pluginCategories, pluginManager))
                {
                    foreach (ICenterTab plugin in pluginManager.CenterTabPlugins)
                    {
                        PluginContainer container = pluginManager.mPluginContainers[plugin];

                        if (container.IsEnabled)
                        {
                            ICenterTab plugin1 = plugin;
                            PluginCommand(() =>
                                {
                                    plugin1.InitializeTab(tabControl);
                                }, container, "Failed in InitializeTab");
                        }
                    }

                    // Execute the new style plugins
                    var plugins = pluginManager.ImportedPlugins.Where(x => x.InitializeCenterTabHandler != null);
                    foreach (var plugin in plugins)
                    {
                        var container = pluginManager.mPluginContainers[plugin];
                        if (container.IsEnabled)
                        {
                            PluginBase plugin1 = plugin;
                            PluginCommand(() =>
                                {
                                    plugin1.InitializeCenterTabHandler(tabControl);
                                }, container, "Failed in InitializeTab");
                        }
                    }
                }
            }
        }

        internal static void ReactToTreeViewRightClick(TreeNode rightClickedTreeNode, ContextMenuStrip menuToModify)
        {

            SaveRelativeDirectory();

            foreach (PluginManager pluginManager in mInstances)
            {
                foreach (ITreeViewRightClick plugin in pluginManager.TreeViewPlugins)
                {
                    PluginContainer container = pluginManager.mPluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        ITreeViewRightClick plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToRightClick(rightClickedTreeNode, menuToModify);
                            }, container, "Failed in ReactToRightClick");
                    }
                }

                // Execute the new style plugins
                var plugins = pluginManager.ImportedPlugins.Where(x => x.ReactToTreeViewRightClickHandler != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginBase plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToTreeViewRightClickHandler(rightClickedTreeNode, menuToModify);
                            }, container, "Failed in ReactToRightClick");
                    }
                }
            }


            ResumeRelativeDirectory("ReactToTreeViewRightClickHandler");
        }

        internal static void ReactToStateNameChange(IElement element, string oldName, string newName)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                foreach (IStateChange plugin in pluginManager.StateChangePlugins)
                {
                    PluginContainer container = pluginManager.mPluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        IStateChange plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToStateNameChange(element, oldName, newName);
                            }, container, "Failed in ReactToStateNameChange");
                    }
                }

                // Execute the new style plugins
                var plugins = pluginManager.ImportedPlugins.Where(x => x.ReactToStateNameChangeHandler != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginBase plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToStateNameChangeHandler(element, oldName, newName);
                            }, container, "Failed in ReactToStateNameChange");
                    }
                }
            }
        }

        internal static void ReactToStateRemoved(IElement element, string stateName)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                foreach (IStateChange plugin in pluginManager.StateChangePlugins)
                {
                    PluginContainer container = pluginManager.mPluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        IStateChange plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToStateRemoved(element, stateName);
                            }, container, "Failed in ReactToStateRemoved");
                    }
                }

                // Execute the new style plugins
                var plugins = pluginManager.ImportedPlugins.Where(x => x.ReactToStateRemovedHandler != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginBase plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToStateRemovedHandler(element, stateName);
                            }, container, "Failed in ReactToStateRemoved");
                    }
                }
            }
        }

        internal static void ReactToEventResponseRemoved(IElement element, EventResponseSave eventResponse)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                var plugins = pluginManager.ImportedPlugins.Where(x => x.ReactToEventRemoved != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginBase plugin1 = plugin;
                        PluginCommand(() =>
                        {
                            plugin1.ReactToEventRemoved(element, eventResponse);
                        }, container, "Failed in ReactToEventResponseRemoved");
                    }
                }
            }
        }

        internal static void ReactToFileRemoved(IElement element, ReferencedFileSave file)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                var plugins = pluginManager.ImportedPlugins.Where(x => x.ReactToFileRemoved != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginBase plugin1 = plugin;
                        PluginCommand(() =>
                        {
                            plugin1.ReactToFileRemoved(element, file);
                        }, container, "Failed in ReactToFileRemoved");
                    }
                }
            }
        }

        internal static void ReactToElementVariableChange(IElement element, CustomVariable variable)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                var plugins = pluginManager.ImportedPlugins.Where(x => x.ReactToElementVariableChange != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginBase plugin1 = plugin;
                        PluginCommand(() =>
                        {
                            plugin1.ReactToElementVariableChange(element, variable);
                        }, container, "Failed in ReactToNewObject");
                   }
                }
            }
        }

        internal static void ReactToNewObject(NamedObjectSave newObject)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                foreach (INewObject plugin in pluginManager.NewObjectPlugins)
                {
                    PluginContainer container = pluginManager.mPluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        INewObject plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToNewObject(newObject);
                            }, container, "Failed in ReactToNewObject");
                    }
                }

                // Execute the new style plugins
                var plugins = pluginManager.ImportedPlugins.Where(x => x.ReactToNewObjectHandler != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginBase plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToNewObjectHandler(newObject);
                            }, container, "Failed in ReactToNewObject");
                    }
                }
            }
        }

        internal static void ReactToNewScreenCreated(ScreenSave screen)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                var plugins = pluginManager.ImportedPlugins.Where(x => x.ReactToNewScreenCreated != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginBase plugin1 = plugin;
                        PluginCommand(() =>
                        {
                            plugin1.ReactToNewScreenCreated(screen);
                        }, container, "Failed in ReactToNewScreenCreated");
                    }
                }
            }
        }

        internal static bool OpenSolution(string solutionName)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                foreach (var openSolutionPlugin in pluginManager.OpenVisualStudioPlugins)
                {
                    PluginContainer container = pluginManager.mPluginContainers[openSolutionPlugin];

                    if (container.IsEnabled)
                    {
                        var shouldReturnTrue = false;
                        PluginCommandWithThrow(() =>
                            {
                                if (openSolutionPlugin.OpenSolution(solutionName))
                                {
                                    shouldReturnTrue = true;
                                }
                            }, container, "Failed to Open Solution");

                        if(shouldReturnTrue) return true;
                    }
                }

                // Execute the new style plugins
                var plugins = pluginManager.ImportedPlugins.Where(x => x.OpenSolutionHandler != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginBase plugin1 = plugin;
                        var shouldReturnTrue = false;

                        PluginCommand(() =>
                            {
                                if(plugin1.OpenSolutionHandler(solutionName))
                                {
                                    shouldReturnTrue = true;            
                                }
                            },container, "Failed in Open Solution");

                        if (shouldReturnTrue) return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Raised when the user clicks the menu item to open a project.  This allows plugins to handle opening projects in other
        /// IDEs (like Eclipse).
        /// </summary>
        /// <param name="projectName">The name of the project being opened.</param>
        /// <returns>Whether any plugin handled the opening of the project.</returns>
        internal static bool OpenProject(string projectName)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                foreach (var openVisualStudio in pluginManager.OpenVisualStudioPlugins)
                {
                    PluginContainer container = pluginManager.mPluginContainers[openVisualStudio];

                    if (container.IsEnabled)
                    {
                        IOpenVisualStudio studio = openVisualStudio;
                        bool shouldReturnTrue = false;
                        PluginCommandWithThrow(() =>
                            {
                                if (studio.OpenProject(projectName))
                                {
                                    shouldReturnTrue = true;
                                }
                            },container, "Failed to Open Project.");

                        if (shouldReturnTrue) return true;
                    }
                }

                // Execute the new style plugins
                var plugins = pluginManager.ImportedPlugins.Where(x => x.OpenProjectHandler != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginBase plugin1 = plugin;
                        bool shouldReturnTrue = false;
                        PluginCommand(() =>
                            {
                                if (plugin1.OpenProjectHandler(projectName))
                                {
                                    shouldReturnTrue = true;
                                }

                            },container, "Failed to Open Project");

                        if (shouldReturnTrue) return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Loops through all contained plugins and raises the appropriate events
        /// in response to a new file being created through the new file dialog (although
        /// it's possible new files could be created without that dialog in the future).
        /// </summary>
        /// <param name="newRfs">The newly-created ReferencedFileSave/param>
        internal static void ReactToNewFile(ReferencedFileSave newRfs)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                foreach (INewFile plugin in pluginManager.NewFilePlugins)
                {
                    PluginContainer container = pluginManager.mPluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        INewFile plugin1 = plugin;
                        PluginCommand(()=>
                            {
                                plugin1.ReactToNewFile(newRfs);
                            },container, "Failed in ReactToNewFile");
                    }
                }

                // Execute the new style plugins
                var plugins = pluginManager.ImportedPlugins.Where(x => x.ReactToNewFileHandler != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginCommand(() =>
                            {
                                plugin.ReactToNewFileHandler(newRfs);
                            },container, "Failed in ReactToNewFile");
                    }
                }
            }
        }

        internal static void ReactToItemSelect(TreeNode selectedTreeNode)
        {
            CenterTab.SuspendLayout();

            foreach (PluginManager pluginManager in mInstances)
            {
                foreach (ITreeItemSelect plugin in pluginManager.TreeItemSelectPlugins)
                {
                    PluginContainer container = pluginManager.mPluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        ITreeItemSelect plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToItemSelect(selectedTreeNode);
                            },container, "Failed in ReactToItemSelect");
                    }
                }

                // Execute the new style plugins
                var plugins = pluginManager.ImportedPlugins.Where(x => x.ReactToItemSelectHandler != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginBase plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToItemSelectHandler(selectedTreeNode);
                            },container, "Failed in ReactToItemSelect");
                    }
                }
            }

            ShowMostRecentTabFor(TopTab);
            ShowMostRecentTabFor(LeftTab);
            ShowMostRecentTabFor(RightTab);
            ShowMostRecentTabFor(BottomTab);
            ShowMostRecentTabFor(CenterTab);

            CenterTab.ResumeLayout();

        }

        private static void ShowMostRecentTabFor(TabControl tabControl)
        {
            if (tabControl.TabPages.Count > 1)
            {
                List<PluginTab> tabs = new List<PluginTab>();
                foreach (TabPage tab in tabControl.TabPages)
                {
                    // If it's not a plugin tab, then it's not able to
                    // report its last usage time
                    if (tab is PluginTab)
                    {
                        tabs.Add(((object)tab) as PluginTab);
                    }
                }

                var ordered = tabs.OrderByDescending(item => item.LastTimeClicked).ToList();

                if (ordered.Count > 1)
                {

                    if (ordered[0].LastTimeClicked != ordered[1].LastTimeClicked &&
                        tabControl.SelectedIndex != tabControl.TabPages.IndexOf(ordered[0]))
                    {
                        tabControl.SelectedTab = ordered[0];
                    }
                }
            }

        }

        internal static void ReactToPropertyGridRightClick(PropertyGrid rightClickedPropertyGrid, ContextMenu menuToModify)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                foreach (IPropertyGridRightClick plugin in pluginManager.PropertyGridRightClickPlugins)
                {
                    PluginContainer container = pluginManager.mPluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        IPropertyGridRightClick plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToRightClick(rightClickedPropertyGrid, menuToModify);
                            },container, "Failed in ReactToRightClick");
                    }
                }

                // Execute the new style plugins
                var plugins = pluginManager.ImportedPlugins.Where(x => x.ReactToRightClickHandler != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginBase plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToRightClickHandler(rightClickedPropertyGrid, menuToModify);
                            },container, "Failed in ReactToRightClick");
                    }
                }
            }
        }

        internal static void ReactToChangedFile(string fileName)
        {

            SaveRelativeDirectory();

            foreach (PluginManager pluginManager in mInstances)
            {
                foreach (IContentFileChange plugin in pluginManager.ContentFileChangePlugins)
                {
                    PluginContainer container = pluginManager.mPluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        IContentFileChange plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToFileChange(fileName);
                            },container, "Failed in ReactToChangedFile");
                    }
                }

                // Execute the new style plugins
                var plugins = pluginManager.ImportedPlugins.Where(x => x.ReactToFileChangeHandler != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginBase plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToFileChangeHandler(fileName);
                            },container,"Failed in ReactToChangedFile");
                    }
                }
            }


            ResumeRelativeDirectory("ReactToFileChangeHandler");
        }

        internal static void ReactToChangedBuiltFile(string fileName)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                // Execute the new style plugins
                var plugins = pluginManager.ImportedPlugins.Where(x => x.ReactToBuiltFileChangeHandler != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginBase plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToBuiltFileChangeHandler(fileName);
                            },container, "Failed in ReactToChangedFile");
                    }
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// Receives output and passes it to any output plugins.
        /// If the PluginManager is not initialized yet then it will
        /// store off the output until it is finished with initialization,
        /// then it will pass all output to the output plugins.
        /// </summary>
        /// <param name="output">The output to print.</param>
        #endregion
        public static void ReceiveOutput(string output)
        {
            if (ProjectManager.WantsToClose == false)
            {
                output = System.DateTime.Now.ToLongTimeString() + " - " + output;

                if (mInstances == null || mInstances.Count == 0)
                {
                    mPreInitializeOutput.AppendLine(output);
                }
                else
                {
                    try
                    {
                        var instances = mInstances.ToList();

                        foreach (PluginManager pluginManager in instances)
                        {
                            PrintOutput(output, pluginManager);
                        }
                    }
                    catch (Exception)
                    {
                        // This is okay if this happens - it may be that output is happening
                        // while Glue is reloading. 
                    }
                }
            }
        }

        private static void PrintOutput(string output, PluginManager pluginManager)
        {
            foreach (IOutputReceiver plugin in pluginManager.OutputReceiverPlugins)
            {
                PluginContainer container = pluginManager.mPluginContainers[plugin];

                if (container.IsEnabled)
                {
                    IOutputReceiver plugin1 = plugin;
                    PluginCommand(() =>
                        {
                            plugin1.OnOutput(output);
                        },container, "Failed in ReactToChangedFile");
                }
            }

            // Execute the new style plugins
            var plugins = pluginManager.ImportedPlugins.Where(x => x.OnOutputHandler != null);
            foreach (var plugin in plugins)
            {
                var container = pluginManager.mPluginContainers[plugin];
                if (container.IsEnabled)
                {
                    PluginBase plugin1 = plugin;
                    PluginCommand(() =>
                        {
                            plugin1.OnOutputHandler(output);
                        },container, "Failed in OnOutput");
                }
            }
        }

        public static void ReceiveError(string output)
        {
            if (!string.IsNullOrEmpty(output))
            {
                output = System.DateTime.Now.ToLongTimeString() + " - " + output;

                if (mInstances == null || mInstances.Count == 0)
                {
                    mPreInitializeError.AppendLine(output);
                }
                else
                {
                    foreach (PluginManager pluginManager in mInstances)
                    {
                        PrintError(output, pluginManager);
                    }
                }
            }
        }

        private static void PrintError(string output, PluginManager pluginManager)
        {
            foreach (IOutputReceiver plugin in pluginManager.OutputReceiverPlugins)
            {
                PluginContainer container = pluginManager.mPluginContainers[plugin];

                if (container.IsEnabled)
                {
                    IOutputReceiver plugin1 = plugin;
                    PluginCommand(() =>
                        {
                            plugin1.OnErrorOutput(output);
                        },container, "Failed in ReactToChangedFile");
                }
            }

            // Execute the new style plugins
            var plugins = pluginManager.ImportedPlugins.Where(x => x.OnErrorOutputHandler != null);
            foreach (var plugin in plugins)
            {
                var container = pluginManager.mPluginContainers[plugin];
                if (container.IsEnabled)
                {
                    PluginBase plugin1 = plugin;
                    PluginCommand(() =>
                        {
                            plugin1.OnErrorOutputHandler(output);
                        },container, "Failed in OnErrorOutput");
                }
            }
        }

        internal static void ReactToLoadedGluxEarly(GlueProjectSave glueProjectSave)
        {

            SaveRelativeDirectory();
            foreach (PluginManager pluginManager in mInstances)
            {
                // Execute the new style plugins
                var plugins = pluginManager.ImportedPlugins.Where(x => x.ReactToLoadedGluxEarly != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginBase plugin1 = plugin;
                        PluginCommand(() =>
                        {
                            plugin1.ReactToLoadedGluxEarly();
                        }, container, "Failed in ReactToLoadedGluxEarly");
                    }
                }
            }
            ResumeRelativeDirectory("ReactToLoadedGluxEarly");
        }

        internal static void ReactToLoadedGlux(GlueProjectSave glueProjectSave, string fileName, Action<string> displayCurrentStatusMethod)
        {

            SaveRelativeDirectory();

            foreach (PluginManager pluginManager in mInstances)
            {
                foreach (IGluxLoad plugin in pluginManager.GluxLoadPlugins)
                {
                    PluginContainer container = pluginManager.mPluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        displayCurrentStatusMethod?.Invoke("Notifying " + container.Name + " of startup...");
                        IGluxLoad plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToGluxLoad(glueProjectSave, fileName);
                            },container, "Failed in ReactToGluxLoad");
                    }
                }

                // Execute the new style plugins
                var plugins = pluginManager.ImportedPlugins.Where(x => x.ReactToLoadedGlux != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        displayCurrentStatusMethod?.Invoke("Notifying " + container.Name + " of startup...");

                        PluginBase plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToLoadedGlux();
                            }, container, "Failed in ReactToLoadedGlux");
                    }
                }
            }


            ResumeRelativeDirectory("ReactToLoadedGlux");
        }

        internal static void RefreshCurrentElement()
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                foreach (ICurrentElement plugin in pluginManager.CurrentElementPlugins)
                {
                    PluginContainer container = pluginManager.mPluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        ICurrentElement plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.RefreshCurrentElement();
                            },container, "Failed in RefreshCurrentElement");
                    }
                }
            }
        }

        internal static void ReactToNamedObjectChangedValue(string changedMember, object oldValue)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                foreach (INamedObject plugin in pluginManager.NamedObjectsPlugins)
                {
                    PluginContainer container = pluginManager.mPluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        INamedObject plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToNamedObjectChangedValue(changedMember, oldValue);
                            },container, "Failed in ReactToNamedObjectChangedValue");
                    }
                }

                // Execute the new style plugins
                var plugins = pluginManager.ImportedPlugins.Where(x => x.ReactToNamedObjectChangedValueHandler != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginBase plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToNamedObjectChangedValueHandler(changedMember, oldValue);
                            },container, "Failed in ReactToNamedObjectChangedValue");
                    }
                }
            }
        }

        /// <summary>
        /// Notifies all contained plugins that a property has changed. A propert, not
        /// to be confused with a variable, is a value that is usually not related to the
        /// type of object, and is not controlled by the user. 
        /// </summary>
        /// <param name="changedMember">The member that has changed</param>
        /// <param name="oldValue">The value of the member before the change</param>
        internal static void ReactToChangedProperty(string changedMember, object oldValue)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                foreach (IPropertyChange plugin in pluginManager.PropertyChangePlugins)
                {
                    PluginContainer container = pluginManager.mPluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        IPropertyChange plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToChangedProperty(changedMember, oldValue);
                            },container, "Failed in ReactToChangedProperty");
                    }
                }

                // Execute the new style plugins
                var plugins = pluginManager.ImportedPlugins.Where(x => x.ReactToChangedPropertyHandler != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginBase plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToChangedPropertyHandler(changedMember, oldValue);
                            },container, "Failed in ReactToChangedProperty");
                    }
                }
            }
        }

        internal static void ReactToGluxSave()
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                foreach (IGluxLoad plugin in pluginManager.GluxLoadPlugins)
                {
                    PluginContainer container = pluginManager.mPluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        IGluxLoad plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToGluxSave();
                            },container, "Failed in ReactToGluxSave");
                    }
                }
            }
        }

        internal static void ReactToGluxUnload(bool isExiting)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                foreach (IGluxLoad plugin in pluginManager.GluxLoadPlugins)
                {
                    PluginContainer container = pluginManager.mPluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        IGluxLoad plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.ReactToGluxUnload(isExiting);
                            },container, "Failed in ReactToGluxUnload");
                    }
                }

                
                // Execute the new style plugins
                var plugins = pluginManager.ImportedPlugins.Where(x => x.ReactToUnloadedGlux != null);
                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginBase plugin1 = plugin;
                        PluginCommand(() =>
                        {
                            plugin1.ReactToUnloadedGlux();
                        }, container, "Failed in ReactToUnloadedGlux");
                    }
                }
            }
        }

        internal static void RefreshGlux()
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                foreach (IGluxLoad plugin in pluginManager.GluxLoadPlugins)
                {
                    PluginContainer container = pluginManager.mPluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        IGluxLoad plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                plugin1.RefreshGlux();
                            },container, "Failed in RefreshGlux");
                    }
                }
            }
        }

        internal static void ReactToGlueClose()
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                foreach (KeyValuePair<IPlugin, PluginContainer> kvp in pluginManager.mPluginContainers)
                {
                    if (kvp.Value.IsEnabled)
                    {
                        kvp.Value.IsEnabled = false;

                        if (HandleExceptions)
                        {
                            try
                            {
                                kvp.Key.ShutDown(PluginShutDownReason.GlueShutDown);
                            }
                            catch
                            {
                                MessageBox.Show("Plugin " + kvp.Key.FriendlyName + " failed to shut down properly");
                                // Doesn't matter, we're shutting down
                            }
                        }else
                        {
                            kvp.Key.ShutDown(PluginShutDownReason.GlueShutDown);
                        }
                    }
                }
            }
        }

        internal static void ReactToGluxClose()
        {
            foreach (KeyValuePair<IPlugin, PluginContainer> kvp in ((PluginManager)mProjectInstance).mPluginContainers)
            {
                if (kvp.Value.IsEnabled)
                {
                    kvp.Value.IsEnabled = false;
                    kvp.Key.ShutDown(PluginShutDownReason.GluxUnload);
                }
            }
        }

        internal static void ReenablePlugin(IPlugin pluginToReenable)
        {
            if (pluginToReenable is PluginBase)
            {
                // Reinitialize the plugin interfaces
                var plugin = pluginToReenable as PluginBase;
                if (plugin.InitializeBottomTabHandler != null)
                    plugin.InitializeBottomTabHandler(mBottomTab);

                if (plugin.InitializeCenterTabHandler != null)
                    plugin.InitializeCenterTabHandler(mCenterTab);

                if (plugin.InitializeLeftTabHandler != null)
                    plugin.InitializeLeftTabHandler(mLeftTab);

                if (plugin.InitializeMenuHandler != null)
                    plugin.InitializeMenuHandler(mMenuStrip);

                if (plugin.InitializeRightTabHandler != null)
                    plugin.InitializeRightTabHandler(mRightTab);

                if (plugin.InitializeTopTabHandler != null)
                    plugin.InitializeTopTabHandler(mTopTab);
            }

            if (pluginToReenable is IMenuStripPlugin)
            {
                ((IMenuStripPlugin)pluginToReenable).InitializeMenu(mMenuStrip);
            }

            if (pluginToReenable is ITopTab)
            {
                ((ITopTab)pluginToReenable).InitializeTab(mTopTab);
            }

            if (pluginToReenable is ILeftTab)
            {
                ((ILeftTab)pluginToReenable).InitializeTab(mLeftTab);
            }

            if (pluginToReenable is IBottomTab)
            {
                ((IBottomTab)pluginToReenable).InitializeTab(mBottomTab);
            }

            if (pluginToReenable is IRightTab)
            {
                ((IRightTab)pluginToReenable).InitializeTab(mRightTab);
            }

            if (pluginToReenable is ICenterTab)
            {
                ((ICenterTab)pluginToReenable).InitializeTab(mCenterTab);
            }
        }

        protected override void CompilePluginOutput(string problem)
        {
            ReceiveOutput(problem);
        }
        protected override void CompilePluginError(string problem)
        {
            ReceiveError(problem);
        }

        public static void SelectItemInCurrentFile(string objectInFile)
        {

            CallMethodOnPluginNotUiThread(
                delegate (PluginBase plugin)
                {
                    if (plugin.SelectItemInCurrentFile != null)
                    {
                        plugin.SelectItemInCurrentFile(objectInFile);
                    }
                },
                "SelectItemInCurrentFile");
        }

        public static void HitBreakpoint()
        {
            System.Diagnostics.Debugger.Break();

        }

        internal static void AdjustDisplayedScreen(ScreenSave screenSave, ScreenSavePropertyGridDisplayer screenSaveDisplayer)
        {
            CallMethodOnPlugin(
                delegate(PluginBase plugin)
                {
                    if (plugin.AdjustDisplayedScreen != null)
                    {
                        plugin.AdjustDisplayedScreen(screenSave, screenSaveDisplayer);
                    }
                },
                "AdjustDisplayedScreen");
        }

        internal static void AdjustDisplayedEntity(EntitySave entitySave, EntitySavePropertyGridDisplayer entitySaveDisplayer)
        {
            CallMethodOnPlugin(
                delegate(PluginBase plugin)
                {
                    if (plugin.AdjustDisplayedEntity != null)
                        plugin.AdjustDisplayedEntity(entitySave, entitySaveDisplayer);
                },
                "AdjustDisplayedEntity");
        }

        internal static void AdjustDisplayedNamedObject(NamedObjectSave namedObject, NamedObjectPropertyGridDisplayer displayer)
        {
            CallMethodOnPlugin(
                delegate(PluginBase plugin)
                {
                    if (plugin.AdjustDisplayedNamedObject != null)
                        plugin.AdjustDisplayedNamedObject(namedObject, displayer);
                },
                "AdjustDisplayedNamedObject");
        }

        internal static void AdjustDisplayedCustomVariable(CustomVariable customVariable, CustomVariablePropertyGridDisplayer displayer)
        {
            CallMethodOnPlugin(
                delegate(PluginBase plugin)
                {
                    if (plugin.AdjustDisplayedCustomVariable != null)
                        plugin.AdjustDisplayedCustomVariable(customVariable, displayer);
                },
                "AdjustDisplayedNamedObject");
        }

        internal static void AdjustDisplayedReferencedFile(ReferencedFileSave referencedFileSave, ReferencedFileSavePropertyGridDisplayer displayer)
        {
            CallMethodOnPlugin(
                delegate(PluginBase plugin)
                {
                    if (plugin.AdjustDisplayedReferencedFile != null)
                        plugin.AdjustDisplayedReferencedFile(referencedFileSave, displayer);
                },
                "AdjustDisplayedNamedObject");
        }

        static void CallMethodOnPluginNotUiThread(Action<PluginBase> methodToCall, string methodName)
        {
            foreach (PluginManager manager in mInstances)
            {
                foreach (var plugin in manager.PluginContainers.Keys.Where(plugin => plugin is PluginBase))
                {
                    PluginContainer container = manager.PluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        IPlugin plugin1 = plugin;
                        PluginCommandNotUiThread(() =>
                            {
                                methodToCall(plugin1 as PluginBase);
                            },container, "Failed in " + methodName);
                    }
                }
            }
            
        }

        static void CallMethodOnPlugin(Action<PluginBase> methodToCall, string methodName)
        {
            foreach (PluginManager manager in mInstances)
            {
                foreach (var plugin in manager.PluginContainers.Keys.Where(plugin => plugin is PluginBase))
                {
                    PluginContainer container = manager.PluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        IPlugin plugin1 = plugin;
                        PluginCommand(() =>
                            {
                                methodToCall(plugin1 as PluginBase);
                            },container, "Failed in " + methodName);
                    }
                }
            }
        }

        private static void PluginCommandNotUiThread(Action action, PluginContainer container, string message)
        {
            if (HandleExceptions)
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    container.Fail(e, message);

                    ReceiveError(message + "\n" + e.ToString());
                }
            }
            else
            {
                action();
            }
        }

        private static void PluginCommand(Action action, PluginContainer container, string message)
        {
            if (HandleExceptions)
            {
                if (mMenuStrip.IsDisposed)
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        container.Fail(e, message);

                        ReceiveError(message + "\n" + e.ToString());


                    }
                }
                else
                {
                    if (mMenuStrip.IsDisposed == false)
                    {
                        // Do this on a UI thread
                        mMenuStrip.Invoke((MethodInvoker)delegate
                        {
                            try
                            {
                                action();
                            }
                            catch (Exception e)
                            {
                                container.Fail(e, message);

                                ReceiveError(message + "\n" + e.ToString());


                            }
                        });
                    }
                    else
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception e)
                        {
                            container.Fail(e, message);

                            ReceiveError(message + "\n" + e.ToString());


                        }
                    }
                }
            }
            else
            {
                action();
            }
        }

        private static void PluginCommandWithThrow(Action action, PluginContainer container, string message)
        {
            if (HandleExceptions)
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    container.Fail(e, message);
                    throw;
                }
            }
            else
            {
                action();
            }
        }

        #endregion

        internal static void PrintPreInitializeOutput()
        {
            if (mPreInitializeOutput.Length != 0)
            {
                PrintOutput(mPreInitializeOutput.ToString(), ((PluginManager)mGlobalInstance));
            }

            //System.Threading.Thread.Sleep(300);
            if (mPreInitializeError.Length != 0)
            {
                PrintError(mPreInitializeError.ToString(), ((PluginManager)mGlobalInstance));
            }
            mPreInitializeOutput.Clear();
            mPreInitializeError.Clear();
        }

        internal static bool CanFileReferenceContent(string absoluteName)
        {

            SaveRelativeDirectory();

            bool toReturn = false;


            CallMethodOnPluginNotUiThread(
                delegate(PluginBase plugin)
                {
                    if (plugin.CanFileReferenceContent != null)
                    {
                        toReturn |= plugin.CanFileReferenceContent(absoluteName);

                    }
                },
                "CanFileReferenceContent");

            ResumeRelativeDirectory("CanFileReferenceContent");
            return toReturn;


        }

        internal static void GetFilesReferencedBy(string absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive, List<string> listToFill)
        {

            SaveRelativeDirectory();
            CallMethodOnPluginNotUiThread(
                delegate(PluginBase plugin)
                {
                    if (plugin.GetFilesReferencedBy != null)
                    {
                        plugin.GetFilesReferencedBy(absoluteName, topLevelOrRecursive, listToFill);

                    }
                },
                "GetFilesReferencedBy");

            ResumeRelativeDirectory("GetFilesReferencedBy");
        }

        internal static void GetFilesNeededOnDiskBy(string absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive, List<string> listToFill)
        {
            SaveRelativeDirectory();
            CallMethodOnPluginNotUiThread(
                delegate (PluginBase plugin)
                {
                    if (plugin.GetFilesNeededOnDiskBy != null)
                    {
                        plugin.GetFilesNeededOnDiskBy(absoluteName, listToFill);
                    }
                    else if (plugin.GetFilesReferencedBy != null)
                    {
                        plugin.GetFilesReferencedBy(absoluteName, topLevelOrRecursive, listToFill);

                    }
                },
                "GetFilesReferencedBy");

            ResumeRelativeDirectory("GetFilesReferencedBy");
        }

        internal static bool TryHandleException(Exception exception)
        {
            bool wasHandled = false;

            string source = exception.Source;

            foreach (var instance in mInstances)
            {
                foreach (var plugin in instance.PluginContainers.Values)
                {
                    if (WasExceptionCausedByPlugin(exception, plugin))
                    {
                        // We're going to blame this plugin for the error
                        MessageBox.Show("A plugin has had an error.  Shutting down the plugin " + plugin.Name + "\n\nAdditional information:\n\n" + 
                            exception.ToString());

                        wasHandled = true;

                        plugin.Fail(exception, "This plugin had an error that was not caught by its code, and was not caused by a Glue-initiated call");
                    }

                    if (wasHandled)
                    {
                        break;
                    }
                }
                if (wasHandled)
                {
                    break;
                }
            }

            return wasHandled;
        }

        private static bool WasExceptionCausedByPlugin(Exception exception, PluginContainer plugin)
        {
            if(plugin.Plugin.GetType().Assembly.GetName().Name == exception.Source)
            {
                return true;
            }

            foreach (var name in plugin.Plugin.GetType().Assembly.GetReferencedAssemblies())
            {
                if (name.Name == exception.Source)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool TryCopyFile(string sourceFile, string targetFile)
        {

            SaveRelativeDirectory();

            bool wasCopyHandled = false;

            foreach (PluginManager pluginManager in mInstances)
            {
                if (wasCopyHandled)
                {
                    break;
                }

                var plugins = pluginManager.ImportedPlugins.Where(plugin => 
                    {
                        return plugin.TryHandleCopyFile != null &&
                        pluginManager.mPluginContainers[plugin].IsEnabled;
                    });

                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        if (HandleExceptions)
                        {
                            try
                            {
                                wasCopyHandled |= plugin.TryHandleCopyFile(sourceFile, FileManager.GetDirectory(sourceFile), targetFile);
                                
                            }
                            catch (Exception e)
                            {
                                container.Fail(e, "Failed in TryHandleCopyFile");
                            }
                        }
                        else
                        {
                            wasCopyHandled |= plugin.TryHandleCopyFile(sourceFile, FileManager.GetDirectory(sourceFile), targetFile);
                        }



                        if (wasCopyHandled)
                        {
                            break;
                        }
                    }
                }
            }

            ResumeRelativeDirectory("TryCopyFile");
            return wasCopyHandled;
        }

        internal static bool TryAddContainedObjects(string sourceFile, List<string> listToAddTo)
        {

            SaveRelativeDirectory();
            bool wasAddHandled = false;

            foreach (PluginManager pluginManager in mInstances)
            {
                if (wasAddHandled)
                {
                    break;
                }

                var plugins = pluginManager.ImportedPlugins.Where(plugin =>
                {
                    return plugin.TryAddContainedObjects != null &&
                    pluginManager.mPluginContainers[plugin].IsEnabled;
                });

                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {

                        if (HandleExceptions)
                        {
                            try
                            {
                                wasAddHandled |= plugin.TryAddContainedObjects(sourceFile, listToAddTo);

                            }
                            catch (Exception e)
                            {
                                container.Fail(e, "Failed in TryHandleCopyFile");
                            }
                        }
                        else
                        {
                            wasAddHandled |= plugin.TryAddContainedObjects(sourceFile, listToAddTo);
                        }



                        if (wasAddHandled)
                        {
                            break;
                        }
                    }
                }
            }

            ResumeRelativeDirectory("TryAddContainedObjects");
            return wasAddHandled;
        }

        internal static void ReactToSyncedProjectLoad(ProjectBase projectBase)
        {
            SaveRelativeDirectory();

            CallMethodOnPlugin(
                delegate(PluginBase plugin)
                {
                    if (plugin.ReactToLoadedSyncedProject != null)
                        plugin.ReactToLoadedSyncedProject(projectBase);
                },
                "ReactToSyncedProjectLoad");

            ResumeRelativeDirectory("ReactToSyncedProjectLoad");
        }

        public static TypeConverter GetTypeConverter(IElement container, NamedObjectSave instance, TypedMemberBase typedMember)
        {
            TypeConverter toReturn = null;

            SaveRelativeDirectory();

            CallMethodOnPlugin(
                delegate (PluginBase plugin)
                {
                    if (plugin.GetTypeConverter != null)
                    {
                        var foundValue = plugin.GetTypeConverter(container, instance, typedMember);
                        if (foundValue != null)
                        {
                            toReturn = foundValue;
                        }
                    }
                },
                "GetTypeConverter");

            ResumeRelativeDirectory("GetTypeConverter");

            return toReturn;
        }

        public static void GetEventSignatureArgs(NamedObjectSave namedObjectSave, EventResponseSave eventResponseSave, out string type, out string args)
        {
            string foundType = null;
            string foundArgs = null;

            CallMethodOnPlugin(
                delegate (PluginBase plugin)
                {
                    if (plugin.GetEventSignatureArgs != null)
                    {
                        string tempFoundType;
                        string tempFoundArgs;
                        plugin.GetEventSignatureArgs(namedObjectSave, eventResponseSave, out tempFoundType, out tempFoundArgs);
                        if (tempFoundType != null)
                        {
                            foundType = tempFoundType;
                            foundArgs = tempFoundArgs;
                        }
                    }
                },
                "GetEventSignatureArgs");

            type = foundType;
            args = foundArgs;
        }

        public static void WriteInstanceVariableAssignment(NamedObjectSave namedObject, ICodeBlock codeBlock, InstructionSave instructionSave)
        {
            TypeConverter toReturn = null;

            SaveRelativeDirectory();

            CallMethodOnPlugin(
                delegate (PluginBase plugin)
                {
                    if (plugin.WriteInstanceVariableAssignment != null)
                    {
                        plugin.WriteInstanceVariableAssignment(namedObject, codeBlock, instructionSave);
                    }
                },
                "WriteInstanceVariableAssignment");

            ResumeRelativeDirectory("GetTypeConverter");
        }

        internal static void AddEventsForObject(NamedObjectSave namedObjectSave, List<ExposableEvent> listToFill)
        {
            SaveRelativeDirectory();

            foreach (PluginManager pluginManager in mInstances)
            {
                var plugins = pluginManager.ImportedPlugins.Where(plugin =>
                {
                    return plugin.AddEventsForObject != null &&
                    pluginManager.mPluginContainers[plugin].IsEnabled;
                });

                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];


                    if (HandleExceptions)
                    {
                        try
                        {
                            plugin.AddEventsForObject(namedObjectSave, listToFill);

                        }
                        catch (Exception e)
                        {
                            container.Fail(e, "Failed in AddEventsForObject");
                        }
                    }
                    else
                    {
                        plugin.AddEventsForObject(namedObjectSave, listToFill);
                    }


                }
            }
            ResumeRelativeDirectory("AddEventsForObject");

        }


        static System.Collections.Concurrent.ConcurrentStack<string> mOldRelativeDirectories = new System.Collections.Concurrent.ConcurrentStack<string>();

        static void SaveRelativeDirectory()
        {

            mOldRelativeDirectories.Push(FileManager.RelativeDirectory);
        }

        static void ResumeRelativeDirectory(string function)
        {
            if (mOldRelativeDirectories.Count == 0)
            {
                FileManager.RelativeDirectory = FileManager.GetDirectory(
                    FlatRedBall.Glue.Plugins.ExportedImplementations.GlueState.Self.CurrentGlueProjectFileName);
            }
            else
            {
                bool differs = true;

                try
                {
                    string top = null;
                    if(mOldRelativeDirectories.TryPeek(out top))
                    {
                        differs = FileManager.RelativeDirectory != top;
                    }
                }
                catch
                {
                    // no big deal we'll just act as if it differs
                }
                if (differs)
                {
                    ReceiveError("The relativeDirectory wasn't set properly in " + function);

                    string top = null;
                    if (mOldRelativeDirectories.TryPeek(out top))
                    {
                        FileManager.RelativeDirectory = top;
                    }

                }

                try
                {
                    string throwaway;
                    mOldRelativeDirectories.TryPop(out throwaway);
                }
                catch(ArgumentOutOfRangeException)
                {
                    // no big deal
                }
            }
        }

    }
}
