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
//using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Reflection;
using System.ComponentModel;
using System.Runtime.Loader;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using Microsoft.Build.Framework;
using GeneralResponse = ToolsUtilities.GeneralResponse;
using WpfTabControl = System.Windows.Controls.TabControl;
using GlueFormsCore.Controls;
using System.Runtime.CompilerServices;
using FlatRedBall.Glue.FormHelpers;
using GlueFormsCore.FormHelpers;
using GlueFormsCore.ViewModels;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
//using Gum.Wireframe;
//using Gum.Converters;

namespace FlatRedBall.Glue.Plugins
{
    public class PluginManager : PluginManagerBase
    {
        #region Fields/Properties

        #region Interface Lists

        List<PluginBase> importedPlugins = new List<PluginBase>();
        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<PluginBase> ImportedPlugins
        {
            get => importedPlugins;
            set => importedPlugins = value.ToList() ;
        }

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<IMenuStripPlugin> MenuStripPlugins { get; set; } = new List<IMenuStripPlugin>();

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<ICurrentElement> CurrentElementPlugins { get; set; } = new List<ICurrentElement>();

        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<ICodeGeneratorPlugin> CodeGeneratorPlugins { get; set; } = new List<ICodeGeneratorPlugin>();


        #endregion

        private static MenuStrip mMenuStrip;

        public static TabControlViewModel TabControlViewModel { get; private set; }

        public static System.Windows.Controls.ToolBarTray ToolBarTray
        {
            get;
            private set;
        }

        GlueCommands mGlueCommands = new GlueCommands();

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
                return FlatRedBall.Glue.Plugins.ExportedImplementations.GlueState.Self;
            }
        }



        #endregion

        #endregion

        #region Constructor/Initialize


        public PluginManager(bool global)
            : base(global)
        {
            // Forces this class to be accesed. Vic is not sure if this is needed as of Sept 13, 2021
            //var throwaway8 = typeof(Microsoft.Build.Utilities.AssemblyFoldersFromConfigInfo);
            //var throwaway2 = typeof(GraphicalUiElement);
            //var gue = new GraphicalUiElement();
            //gue.XUnits = GeneralUnitType.PixelsFromMiddle;
            //var throwaway3 = typeof(GeneralUnitType);
            

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        protected override void StartAllPlugins(List<string> pluginsToIgnore = null)
        {


            var allPlugins = new List<IEnumerable<IPlugin>>
            {
                MenuStripPlugins,
                CodeGeneratorPlugins,
                CurrentElementPlugins
            };

            foreach (var pluginList in allPlugins)
            {
                foreach (var plugin in pluginList)
                    StartupPlugin(plugin);
            }

            var embedded = ImportedPlugins
                .Where(item => item is EmbeddedPlugin)
                .Select(item => item as EmbeddedPlugin);
            var sortedEmbedded = embedded
                .OrderBy(item => item.DesiredOrder)
                .ToArray();
            foreach (var plugin in sortedEmbedded)
            {
                StartupPlugin(plugin);

                // did it fail?
                var container = mPluginContainers[plugin];
                if(container.FailureException != null)
                {
                    PluginManager.ReceiveError(container.FailureException.ToString());

                }
            }

            var normal = ImportedPlugins.Except(embedded);
            foreach (var plugin in normal)
            {
                StartupPlugin(plugin);

                // did it fail?
                var container = mPluginContainers[plugin];
                if (container.FailureException != null)
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
            MenuStripPlugins = new List<IMenuStripPlugin>();
            CurrentElementPlugins = new List<ICurrentElement>();
            CodeGeneratorPlugins = new List<ICodeGeneratorPlugin>();
        }

        internal static void Initialize(bool isStartup, List<string> pluginsToIgnore = null)
        {
            if (isStartup)
            {
                MoveInstalledPluginsToPluginDirectory();
                UninstallPlugins();

                EditorObjects.IoC.Container.Get<GlueErrorManager>()
                    .Add(new PluginErrors.PluginErrorReporter());
            }

            if (mGlobalInstance == null)
            {
                mGlobalInstance = new PluginManager(true)
                {
                    // global assemblies must not be collectible for now (2nd argument == false).  This is because
                    // some plugins (like Tiled) use the `XmlSerializer`, but as of now the XmlSerializer class
                    // loads dynamic assemblies in the default assembly context, not the current/relevant one.  This
                    // means code in a collectible assembly loads non-collectible assemblies, which isn't allowed and
                    // causes a crash.  Right now the fix for this has been pushed back to .net 6, and it's not for
                    // sure that it will actually be prioritized.  See https://github.com/dotnet/runtime/issues/1388.
                    //
                    // In the mean time, it is rare that global assemblies actually need to be unloaded without a 
                    // Glue restart.  So to solve this for now we are making global plugins loaded in a non-collectible
                    // context, so at least these plugins can still be used by glue, just not in a project specific
                    // case.
                    //AssemblyContext = new AssemblyLoadContext(null, false),
                };

                mGlobalInstance.AssemblyContext = new AssemblyLoadContext(null, false);

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
                


                // Assembly loading only happens asynchronously when `Unload()` is called and all references
                // to the context have been removed.  This means that they won't be fully cleaned up until the garbage
                // collector runs.  Since we might end up re-loading a new version of the plugins that were previously
                // loaded to be safe we want to pause for GC to occur to make sure the old project specific plugins
                // are fully unloaded before we retry to load the new ones.
                mProjectInstance.AssemblyContext.Unload();
                mProjectInstance.AssemblyContext = null;
                mProjectInstance = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            mProjectInstance = new PluginManager(false)
            {
                // Project specific plugins should always be loaded in a collectible assembly, so they can be
                // unloaded
                AssemblyContext = new AssemblyLoadContext(null, true),
            };

            if(FlatRedBall.Glue.Plugins.ExportedImplementations.GlueState.Self.CurrentGlueProject != null)
            {
                var gameDirectory = FlatRedBall.Glue.Plugins.ExportedImplementations.GlueState.Self.CurrentGlueProjectDirectory;

                FilePath installedDirectory = gameDirectory + @"InstalledPlugins\";
                FilePath pluginDirectory = gameDirectory + @"Plugins\";
                MoveInstalledPluginsToPluginDirectory(installedDirectory, pluginDirectory);

                var runnablePluginDirectory = CopyProjectPluginsToRunnableDirectory(pluginDirectory);
                if (runnablePluginDirectory != null)
                {
                    mProjectInstance.LoadPlugins(runnablePluginDirectory.FullPath);
                }

                if(mProjectInstance.CompileOutput != null)
                {
                    foreach (var output in mProjectInstance.CompileOutput)
                    {
                        ReceiveOutput(output);
                    }
                }

                if(mProjectInstance.CompileErrors != null)
                {
                    foreach (var output in mProjectInstance.CompileErrors)
                    {
                        ReceiveError(output);
                    }
                }
            }

            mInstances.Clear();
            mInstances.Add(mGlobalInstance);
            mInstances.Add(mProjectInstance);
        }

        public static bool InstallPlugin(InstallationType installationType, string localPlugFile)
        {
            bool succeeded = true;
            if (!File.Exists(localPlugFile))
            {
                MessageBox.Show(@"Please select a valid *.plug file to install.");
                succeeded = false;
            }
            
            FilePath installPath = null;
            if (succeeded)
            {
                //Validate install path

                var glueState = ExportedImplementations.GlueState.Self;
                switch (installationType)
                {
                    case InstallationType.ForUser:
                        // We're now going to install to a temporary location and copy those files
                        // to their final location on a restart.

                        //installPath = FileManager.UserApplicationData + @"\FRBDK\Plugins\";
                        //installPath = FileManager.UserApplicationDataForThisApplication + "InstalledPlugins\\";
                        // Update Jan 21, 2020
                        // Now we install to the current exe location instead of shared, so different installs can coexist:
                        installPath = AppDomain.CurrentDomain.BaseDirectory + @"InstalledPlugins\";

                        break;
                    case InstallationType.ForCurrentProject:
                        if (glueState.CurrentGlueProject == null)
                        {
                            MessageBox.Show(@"Can not select For Current Project because no project is currently open.");
                            succeeded = false;
                        }

                        if (succeeded)
                        {
                            Directory.CreateDirectory(glueState.CurrentGlueProjectDirectory + "InstalledPlugins\\");

                            installPath = glueState.CurrentGlueProjectDirectory + "InstalledPlugins\\";
                        }
                        break;
                    default:
                        MessageBox.Show(@"Unknown install type.  Please select a valid install type.");
                        succeeded = false;
                        break;
                }

            }

            if (succeeded)
            {
                //Do install
                using (var zip = new ZipFile(localPlugFile))
                {
                    var rootDirectory = GetRootDirectory(zip.EntryFileNames);

                    //Only allow one folder in zip
                    if (String.IsNullOrEmpty(rootDirectory))
                    {
                        MessageBox.Show(@"Unexpected *.plug format (No root directory found in plugin archive)");
                        succeeded = false;
                    }

                    if (succeeded)
                    {

                        //Delete existing folder
                        if (Directory.Exists(installPath + rootDirectory))
                        {
                            Plugins.PluginManager.ReceiveOutput("Plugin file already exists: " + installPath + @"\" + rootDirectory);
                            DialogResult result = MessageBox.Show(@"Existing plugin already exists!  Do you want to replace it?", @"Confirm delete", MessageBoxButtons.YesNo);

                            if (result == DialogResult.Yes)
                            {
                                try
                                {
                                    FileManager.DeleteDirectory(installPath + rootDirectory);
                                }
                                catch (Exception exc)
                                {
                                    MessageBox.Show("Error trying to delete " + installPath + @"\" + rootDirectory + "\n\n" + exc.ToString());
                                    succeeded = false;
                                }
                            }
                            else
                            {
                                succeeded = false;
                            }
                        }

                        if (succeeded)
                        {
                            //Extract into install path
                            zip.ExtractAll(installPath.FullPath);

                            Plugins.PluginManager.ReceiveOutput("Installed to " + installPath);

                            // This plugin may be installed in a secondary location, but the same plugin may be installed in a primary location overriding this
                            // plugin. If so, we should warn the user.

                            List<FilePath> existingInstallLocations = new List<FilePath>();
                            foreach(var instance in mInstances)
                            {
                                existingInstallLocations.AddRange(instance.PluginContainers
                                    .Select(item => new FilePath(item.Value.AssemblyLocation)
                                        .GetDirectoryContainingThis()
                                        ));
                            }

                            existingInstallLocations = existingInstallLocations.Distinct((first, second) => first == second).ToList();

                            // see if any are in the same folder name in a different location.

                            var endResultDirectory = FileManager.UserApplicationData + @"FRBDK\Plugins\" + rootDirectory;

                            var firstMatching = existingInstallLocations.FirstOrDefault(item => item.StandardizedNoPathNoExtension == rootDirectory &&
                                item != endResultDirectory);
                            //var existingPlugins = 

                            var message =
                                $"On restart plugin will be installed to\n{installPath + rootDirectory}\nRestart Glue to use the new plugin.";

                            if(firstMatching != null)
                            {
                                message += $"\n\nNote that Glue also has a plugin installed at \n{firstMatching.FullPath}";
                            }
                            MessageBox.Show(message);
                        }
                        else
                        {
                            MessageBox.Show("Failed to install plugin.");

                        }
                    }
                }
            }

            return succeeded;
        }

        private static string GetRootDirectory(IEnumerable<string> entryFileNames)
        {
            string currentRootDirectory = null;

            foreach (var entryFileName in entryFileNames)
            {
                if (currentRootDirectory == null && !String.IsNullOrEmpty(GetBaseFolder(entryFileName)))
                {
                    currentRootDirectory = GetBaseFolder(entryFileName);
                }
            }

            return currentRootDirectory;
        }


        private static string GetBaseFolder(string fileName)
        {
            var dirInfo = FileManager.GetDirectory(fileName, RelativeType.Relative);

            while (!String.IsNullOrEmpty(FileManager.GetDirectory(dirInfo, RelativeType.Relative)))
            {
                dirInfo = FileManager.GetDirectory(dirInfo, RelativeType.Relative);
            }

            return dirInfo;
        }

        private static void MoveInstalledPluginsToPluginDirectory()
        {
            string installedDirectory = AppDomain.CurrentDomain.BaseDirectory + @"InstalledPlugins\";
            string pluginDirectory = AppDomain.CurrentDomain.BaseDirectory + @"Plugins\";
            MoveInstalledPluginsToPluginDirectory(installedDirectory, pluginDirectory);
        }

        private static void MoveInstalledPluginsToPluginDirectory(FilePath installedDirectory, FilePath pluginDirectory)
        { 
            // Making Glue startup not so verbose:
            //PluginManager.ReceiveOutput("Looking to copy plugins from " + installedDirectory);

            if (Directory.Exists(installedDirectory.FullPath))
            {
                //PluginManager.ReceiveOutput("Install directory found");
                var directories = Directory.GetDirectories(installedDirectory.FullPath);

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

        /// <summary>
        /// Project specific plugins should be updatable while Glue has the project active.  Since loading the
        /// assemblies locks the dlls we can't just load project specific plugins from the plugin directory root,
        /// instead we have to copy them to a temporary directory that they will be loaded and run from.
        /// </summary>
        /// <param name="pluginDirectory">The path to the project's plugin directory</param>
        /// <returns>The path to the directory containing the copied and loadable plugins</returns>
        private static FilePath CopyProjectPluginsToRunnableDirectory(FilePath pluginDirectory)
        {
            const string directoryPrefix = ".running";

            if (!Directory.Exists(pluginDirectory.FullPath)) // Directory.Exists as FilePath.Exits is only for files
            {
                // No plugin directory, so do nothing
                return null;
            }
            
            // Delete all previously created runnable directories to try and prevent a pileup
            var prevFolders = Directory.GetDirectories(pluginDirectory.FullPath)
                .Where(x => Path.GetFileName(x).StartsWith(directoryPrefix))
                .ToArray();
            
            foreach (var folder in prevFolders)
            {
                try
                {
                    Directory.Delete(folder, true);
                }
                catch
                {
                    // Files may be locked from a previous load, so ignore it.  It'll get deleted later
                }
            }
            
            // Create a temporary directory for the runnable plugins instead of a single static directory.  
            // This is required because something is keeping an assembly load context from being fully unloaded
            // immediately, which is causing the existing plugin assemblies to be locked.  This means they cannot be
            // cleaned up or updated.  This also provides the advantage is that every project load has a fresh copy
            // of all plugins in, so if a plugin is removed we don't need logic to detect that and delete those dlls.

            var tempDirectory = Path.Combine(pluginDirectory.FullPath, $"{directoryPrefix}-{Path.GetRandomFileName()}");
            var runnablePath = new FilePath(tempDirectory);

            Directory.CreateDirectory(runnablePath.FullPath);
            
            var folders = Directory.GetDirectories(pluginDirectory.FullPath)
                .Where(x => !Path.GetFileName(x).StartsWith("."))
                .ToArray();

            foreach (var source in folders)
            {
                var destination = Path.Combine(runnablePath.FullPath, Path.GetFileName(source));
                
                FileManager.CopyDirectory(source, destination, false);
            }

            return runnablePath;
        }

        protected override void AddDirectoriesForInstance(List<string> pluginDirectories, string searchPath)
        {
            var directories = Directory.GetDirectories(searchPath);
            pluginDirectories.AddRange(directories);
        }

        #endregion

        #region General Methods

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
                        EditorObjects.IoC.Container.Get<IGlueCommands>().PrintOutput($"Uninstalled plugin at {line}");
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

        public static object CallPluginMethod(string pluginFriendlyName, string methodName, params object[] parameters)
        {
            object toReturn = null;
            CallMethodOnPlugin((plugin) =>
            {
                var method = plugin.GetType().GetMethod(methodName);
                if (method != null)
                {
                    if(toReturn is Task)
                    {
                        throw new InvalidOperationException("Need to call CallPluginMethodAsync");
                    }
                    toReturn = method.Invoke(plugin, parameters: parameters);
                }
            }, (plugin) => plugin.FriendlyName == pluginFriendlyName,
            $"CallPluginMethod {methodName}");

            return toReturn;
        }

        public static async Task CallPluginMethodAsync(string pluginFriendlyName, string methodName, params object[] parameters)
        {
            await CallMethodOnPluginAsync(async (plugin) =>
            {
                var method = plugin.GetType().GetMethod(methodName);
                if (method != null)
                {
                    var task = method.Invoke(plugin, parameters: parameters) as Task;
                    await task;
                }
            }, (plugin) => plugin.FriendlyName == pluginFriendlyName,
            $"CallPluginMethod {methodName}");
        }

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

        internal static GeneralResponse GetFilesReferencedBy(string absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive, List<FilePath> listToFill)
        {
            GeneralResponse generalResponse =  GeneralResponse.SuccessfulResponse;
            SaveRelativeDirectory();

            CallMethodOnPluginNotUiThread(
                delegate(PluginBase plugin)
                {
                    if(plugin.FillWithReferencedFiles != null)
                    {
                        var response = plugin.FillWithReferencedFiles(absoluteName, listToFill);

                        if(!response.Succeeded)
                        {
                            generalResponse = response;
                        }
                    }
                },
                nameof(GetFilesReferencedBy));

            ResumeRelativeDirectory($"GetFilesReferencedBy for {absoluteName}");

            return generalResponse;
        }

        internal static void GetFilesNeededOnDiskBy(string absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive, List<FilePath> listToFill)
        {
            SaveRelativeDirectory();
            CallMethodOnPluginNotUiThread(
                delegate (PluginBase plugin)
                {
                    if (plugin.GetFilesNeededOnDiskBy != null)
                    {
                        plugin.GetFilesNeededOnDiskBy(absoluteName, listToFill);
                    }
                    else if (plugin.FillWithReferencedFiles != null)
                    {
                        List<FilePath> innerList = new List<FilePath>();
                        plugin.FillWithReferencedFiles(absoluteName, innerList);
                        listToFill.AddRange(innerList);

                    }
                },
                nameof(GetFilesNeededOnDiskBy));

            ResumeRelativeDirectory(nameof(GetFilesNeededOnDiskBy));
        }

        #endregion

        #region Methods

        internal static void AddNewFileOptions(CustomizableNewFileWindow newFileWindow)
        {
            CallMethodOnPlugin(
                (plugin) => plugin.AddNewFileOptionsHandler(newFileWindow),
                (plugin => plugin.AddNewFileOptionsHandler != null),
                nameof(AddNewFileOptions));
        }

        internal static string CreateNewFile(AssetTypeInfo assetTypeInfo, object extraData, string directory, string name)
        {
            string createdFile = null;
            bool created = false;

            foreach (PluginManager pluginManager in mInstances)
            {
                // Execute the new style plugins
                if (!created)
                {
                    CallMethodOnPlugin((plugin) =>
                        {
                            string createdInternal = null;
                            if (plugin.CreateNewFileHandler(assetTypeInfo, extraData, directory, name, out createdInternal))
                            {
                                createdFile = createdInternal;
                                created = true;
                            }
                        },
                        (plugin) => plugin.CreateNewFileHandler != null,
                        nameof(CreateNewFile));
                }
            }

            return createdFile;
        }

        internal static List<AssetTypeInfo> GetAvailableAssetTypes(ReferencedFileSave referencedFileSave)
        {
            List<AssetTypeInfo> listToReturn = new List<AssetTypeInfo>();
            CallMethodOnPlugin(plugin =>
            {
                var assetTypes = plugin.GetAvailableAssetTypes(referencedFileSave);
                if (assetTypes != null)
                {
                    listToReturn.AddRange(assetTypes);
                }
            },
            plugin => plugin.GetAvailableAssetTypes != null,
            nameof(GetAvailableAssetTypes));

            return listToReturn;
        }

        internal static void HandleFileReadError(FilePath filePath, GeneralResponse response)
        {
            CallMethodOnPluginNotUiThread(
                delegate (PluginBase plugin)
                {
                    if (plugin.ReactToFileReadError != null)
                    {
                        plugin.ReactToFileReadError(filePath, response);
                    }
                },
                "HandleFileReadError");

            ResumeRelativeDirectory("HandleFileReadError");
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

        internal static void SetTabs(TabControlViewModel tabControlViewModel)
        {
            TabControlViewModel = tabControlViewModel;
        }

        internal static void SetToolbarTray(ToolbarControl toolbar)
        {
            ToolBarTray = toolbar.ToolBarTray;

        }

        internal static void ReactToTreeViewRightClick(ITreeNode rightClickedTreeNode, List<GeneralToolStripMenuItem> menuToModify)
        {
            SaveRelativeDirectory();

            CallMethodOnPlugin(
                plugin => plugin.ReactToTreeViewRightClickHandler(rightClickedTreeNode, menuToModify),
                plugin => plugin.ReactToTreeViewRightClickHandler != null);

            ResumeRelativeDirectory("ReactToTreeViewRightClick");
        }

        public static void ReactToStateCreated(StateSave newState, StateSaveCategory category)
        {
            CallMethodOnPlugin(
                plugin => plugin.ReactToStateCreated(newState, category),
                plugin => plugin.ReactToStateCreated != null,
                nameof(ReactToStateCreated));
        }

        public static void ReactToStateVariableChanged(StateSave newState, StateSaveCategory category, string variableName)
        {
            CallMethodOnPlugin(
                plugin => plugin.ReactToStateVariableChanged(newState, category, variableName),
                plugin => plugin.ReactToStateVariableChanged != null,
                nameof(ReactToStateVariableChanged));
        }

        internal static void ReactToStateNameChange(IElement element, string oldName, string newName)
        {
            CallMethodOnPlugin(
                plugin => plugin.ReactToStateNameChangeHandler(element, oldName, newName),
                plugin => plugin.ReactToStateNameChangeHandler != null,
                nameof(ReactToStateNameChange));

        }

        internal static void ReactToStateRemoved(IElement element, string stateName)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
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
            CallMethodOnPlugin(
                (plugin) => plugin.ReactToFileRemoved(element, file),
                (plugin) => plugin.ReactToFileRemoved != null,
                nameof(ReactToFileRemoved));
        }

        internal static void ReactToEntityRemoved(EntitySave entity, List<string> filesToRemove)
        {
            CallMethodOnPlugin(
                (plugin) => plugin.ReactToEntityRemoved(entity, filesToRemove),
                (plugin) => plugin.ReactToEntityRemoved != null,
                nameof(ReactToEntityRemoved));
        }

        internal static void ReactToScreenRemoved(ScreenSave screenSave, List<string> filesToRemove)
        {
            CallMethodOnPlugin(
                (plugin) => plugin.ReactToScreenRemoved(screenSave, filesToRemove),
                plugin => plugin.ReactToScreenRemoved != null,
                nameof(ReactToScreenRemoved));
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

        internal static void ReactToElementRenamed(IElement elementToRename, string oldName)
        {
            CallMethodOnPlugin(
                plugin => plugin.ReactToElementRenamed(elementToRename, oldName),
                plugin => plugin.ReactToElementRenamed != null);
        }

        public static void ReactToNewObject(NamedObjectSave newObject)
        {
            CallMethodOnPlugin(
                plugin => plugin.ReactToNewObjectHandler(newObject),
                plugin => plugin.ReactToNewObjectHandler != null);
        }

        public static Task ReactToNewObjectListAsync(List<NamedObjectSave> newObjectList)
        {
            return CallMethodOnPluginAsync(async (plugin) =>
            {
                var handledByList = false;
                if (plugin.ReactToNewObjectList != null)
                {
                    plugin.ReactToNewObjectList(newObjectList);
                    handledByList = true;
                }

                if (plugin.ReactToNewObjectListAsync != null)
                {
                    await plugin.ReactToNewObjectListAsync(newObjectList);
                    handledByList = true;
                }

                if(!handledByList)
                {
                    foreach(var nos in newObjectList)
                    {
                        plugin.ReactToNewObjectHandler(nos);
                    }
                }
            },
            plugin => plugin.ReactToNewObjectHandler != null || plugin.ReactToNewObjectList != null || plugin.ReactToNewObjectListAsync != null);
        }

        internal static void ReactToObjectRemoved(IElement element, NamedObjectSave removedObject)
        {
            CallMethodOnPlugin(
                plugin => plugin.ReactToObjectRemoved(element, removedObject),
                plugin => plugin.ReactToObjectRemoved != null);
        }

        internal static Task ReactToObjectListRemovedAsync(List<GlueElement> ownerList, List<NamedObjectSave> removedObjects)
        {
            return CallMethodOnPluginAsync(plugin =>
            {
                if(plugin.ReactToObjectListRemoved != null)
                {
                    plugin.ReactToObjectListRemoved(ownerList, removedObjects);
                }
                else
                {
                    
                    // fall back to the single item removed if the entire list can't be handled at once
                    for(int i = 0; i < removedObjects.Count; i++)
                    {
                        var removedObject = removedObjects[i];
                        var owner = ownerList[i];
                        plugin.ReactToObjectRemoved(owner, removedObject);
                    }
                }
            },
            plugin => plugin.ReactToObjectRemoved != null || plugin.ReactToObjectListRemoved != null);
        }

        internal static async Task ReactToNewScreenCreated(ScreenSave screen)
        {
            await CallMethodOnPluginAsync(
                plugin => plugin.NewScreenCreated(screen),
                plugin => plugin.NewScreenCreated != null);
        }

        /// <summary>
        /// Called any time an entity is created. ReactToNewEntityCreatedWithUi may also get called.
        /// </summary>
        /// <param name="entitySave"></param>
        internal static void ReactToNewEntityCreated(EntitySave entitySave)
        {
            CallMethodOnPlugin(
                plugin => plugin.NewEntityCreated(entitySave),
                plugin => plugin.NewEntityCreated != null);
        }

        internal static void ReactToNewEntityCreatedWithUi(EntitySave entitySave, AddEntityWindow window)
        {
            CallMethodOnPlugin(
                plugin => plugin.NewEntityCreatedWithUi(entitySave, window),
                plugin => plugin.NewEntityCreatedWithUi != null);
        }

        internal static Task ReactToNewScreenCreatedWithUiAsync(ScreenSave screen, AddScreenWindow addScreenWindow)
        {
            return CallMethodOnPluginAsync(
                plugin => plugin.NewScreenCreatedWithUi(screen, addScreenWindow),
                plugin => plugin.NewScreenCreatedWithUi != null);
        }

        internal static void ReactToResolutionChanged()
        {
            CallMethodOnPlugin((plugin) =>
            {
                plugin.ResolutionChanged();
            },
            plugin => plugin.ResolutionChanged != null,
            nameof(ReactToResolutionChanged));
        }

        public static async Task<NamedObjectSave> ReactToCreateCollisionRelationshipsBetween(NamedObjectSave firstNos, NamedObjectSave secondNos)
        {
            NamedObjectSave nos = null;
            await CallMethodOnPluginAsync(
                async plugin =>
                {
                    var innerNos = await plugin.ReactToCreateCollisionRelationshipsBetween(firstNos, secondNos);
                    nos = innerNos ?? nos;
                },
                plugin => plugin.ReactToCreateCollisionRelationshipsBetween != null);

            return nos;
        }

        internal static bool OpenSolution(string solutionName)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
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
            CallMethodOnPlugin(
                plugin => plugin.ReactToNewFileHandler(newRfs),
                plugin => plugin.ReactToNewFileHandler != null);
        }

        internal static void ReactToItemSelect(ITreeNode selectedTreeNode)
        {
            TabControlViewModel.IsRecordingSelection = false;
            // Tabs will be added and removed here, and that can cause the selection to change.
            // We don't want the selection change to cause the TabControlViewModel to consider these
            // clicks, so let's tell it to ignore these for now...

            CallMethodOnPlugin(
                plugin => plugin.ReactToItemSelectHandler(selectedTreeNode),
                plugin => plugin.ReactToItemSelectHandler != null);

            TabControlViewModel.UpdateToSelection(selectedTreeNode);
            TabControlViewModel.IsRecordingSelection = true;
        }

        internal static void ReactToItemsSelected(List<ITreeNode> selectedTreeNodes)
        {
            TabControlViewModel.IsRecordingSelection = false;
            // Tabs will be added and removed here, and that can cause the selection to change.
            // We don't want the selection change to cause the TabControlViewModel to consider these
            // clicks, so let's tell it to ignore these for now...

            var first = selectedTreeNodes.FirstOrDefault();

            CallMethodOnPlugin(
                plugin =>
                {
                    if(plugin.ReactToItemsSelected != null)
                    {
                        plugin.ReactToItemsSelected(selectedTreeNodes);
                    }
                    else
                    {
                        plugin.ReactToItemSelectHandler(first);
                    }
                },
                plugin => plugin.ReactToItemSelectHandler != null || plugin.ReactToItemsSelected != null);

            TabControlViewModel.UpdateToSelection(first);
            TabControlViewModel.IsRecordingSelection = true;
        }

        internal static void ReactToPropertyGridRightClick(System.Windows.Forms.PropertyGrid rightClickedPropertyGrid, ContextMenuStrip menuToModify)
        {
            CallMethodOnPlugin(
                plugin => plugin.ReactToRightClickHandler(rightClickedPropertyGrid, menuToModify),
                plugin => plugin.ReactToRightClickHandler != null);
        }

        internal static void ReactToChangedCodeFile(FilePath filePath)
        {
            CallMethodOnPlugin(
                plugin => plugin.ReactToCodeFileChange(filePath),
                plugin => plugin.ReactToCodeFileChange != null);
        }

        internal static void ReactToChangedFile(FilePath fileName, FileChangeType changeType)
        {
            SaveRelativeDirectory();

            CallMethodOnPlugin(
                plugin =>
                {
                    if(changeType == FileChangeType.Created)
                    {
                        // The "Create" type is new. If it is reported to all plugins, then
                        // files may get re-loaded unnecessarily, and we don't want that, so only 
                        // push Add to the new 
                        plugin.ReactToFileChange?.Invoke(fileName, changeType);
                    }
                    else
                    {
                        if (plugin.ReactToFileChange != null)
                        {
                            plugin.ReactToFileChange(fileName, changeType);
                        }
                        else
                        {
                            plugin.ReactToFileChangeHandler(fileName.FullPath);
                        }

                    }
                },
                plugin => plugin.ReactToFileChangeHandler != null || plugin.ReactToFileChange != null);


            ResumeRelativeDirectory(nameof(ReactToChangedFile));
        }

        internal static void ReactToChangedBuiltFile(string fileName)
        {
            CallMethodOnPlugin(
                plugin => plugin.ReactToBuiltFileChangeHandler(fileName),
                plugin => plugin.ReactToBuiltFileChangeHandler != null);
        }

        internal static void ReactToChangedStartupScreen()
        {
            CallMethodOnPlugin(
                plugin => plugin.ReactToChangedStartupScreen(),
                plugin => plugin.ReactToChangedStartupScreen != null);
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
            if (ProjectManager.WantsToCloseProject == false)
            {
                var time = System.DateTime.Now;
                var msDigit = (time.Millisecond / 100).ToString();
                output = $"{time.ToString("h:mm:ss")}.{msDigit} - {output}";

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
                    var instances = mInstances.ToList();

                    foreach (PluginManager pluginManager in instances)
                    {
                        PrintError(output, pluginManager);
                    }
                }
            }
        }

        private static void PrintError(string output, PluginManager pluginManager)
        {
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

        /// <summary>
        /// Method called after a project is loaded, but before any code has been generated
        /// </summary>
        /// <param name="glueProjectSave">The newly-loaded project.</param>
        /// <param name="fileName">The file name of the plugin.</param>
        /// <param name="displayCurrentStatusMethod">The method to call to update the status</param>
        internal static void ReactToLoadedGlux(GlueProjectSave glueProjectSave, string fileName, Action<string> displayCurrentStatusMethod)
        {

            SaveRelativeDirectory();

            foreach (PluginManager pluginManager in mInstances)
            {
                

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

        internal static void ReactToVariableAdded(CustomVariable newVariable)
        {
            CallMethodOnPlugin(
                plugin => plugin.ReactToVariableAdded(newVariable),
                plugin => plugin.ReactToVariableAdded != null);
        }

        internal static void ReactToVariableRemoved(CustomVariable removedVariable)
        {
            foreach (PluginManager pluginManager in mInstances)
            {
                var plugins = pluginManager.ImportedPlugins.Where(x => x.ReactToVariableRemoved != null);

                foreach (var plugin in plugins)
                {
                    var container = pluginManager.mPluginContainers[plugin];
                    if (container.IsEnabled)
                    {
                        PluginCommand(() =>
                        {
                            plugin.ReactToVariableRemoved(removedVariable);
                        }, container, "Failed in ReactToVariableRemoved");
                    }
                }
            }
        }

        /// <summary>
        /// Event raised when either a NamedObject property or variable has changed.
        /// </summary>
        /// <param name="changedMember">The name of the variable or property.</param>
        /// <param name="oldValue">The old value of the variable or property.</param>
        /// <param name="namedObject">The new value of the vairable or property.</param>
        public static void ReactToNamedObjectChangedValue(string changedMember, object oldValue, NamedObjectSave namedObject)
        {
            CallMethodOnPlugin(
                plugin => plugin.ReactToNamedObjectChangedValue(changedMember, oldValue, namedObject),
                plugin => plugin.ReactToNamedObjectChangedValue != null);
        }

        public static void ReactToNamedObjectChangedValueList(List<VariableChangeArguments> changes)
        {
            CallMethodOnPlugin(
                plugin =>
                {
                    if(plugin.ReactToNamedObjectChangedValueList != null)
                    {
                        plugin.ReactToNamedObjectChangedValueList(changes);
                    }
                    else
                    {
                        foreach(var change in changes)
                        {
                            plugin.ReactToNamedObjectChangedValue(change.ChangedMember, change.OldValue, change.NamedObject);
                        }
                    }
                },
                plugin => plugin.ReactToNamedObjectChangedValueList != null || plugin.ReactToNamedObjectChangedValue != null);
        }

        internal static void ReactToReferencedFileChangedValue(string changedMember, object oldValue)
        {
            CallMethodOnPlugin((plugin) =>
                {
                    plugin.ReactToReferencedFileChangedValueHandler(changedMember, oldValue);
                },
                (plugin) => plugin.ReactToReferencedFileChangedValueHandler != null,
                nameof(PluginBase.ReactToReferencedFileChangedValueHandler));
        }

        /// <summary>
        /// Notifies all contained plugins that an property on an element or variable has changed. 
        /// Properties are values which control how Glue generates the code of
        /// an element or variable. 
        /// </summary>
        /// <remarks>Although this has the word "Property" in the name, it applies to both properties and variables.</remarks>
        /// <param name="changedMember">The member that has changed</param>
        /// <param name="oldValue">The value of the member before the change</param>
        public static void ReactToChangedProperty(string changedMember, object oldValue, GlueElement owner, NamedObjectSavePropertyChange nosVariableChange)
        {

            CallMethodOnPlugin(
                plugin => plugin.ReactToChangedPropertyHandler(changedMember, oldValue, owner),
                plugin => plugin.ReactToChangedPropertyHandler != null);

            if (nosVariableChange != null)
            {
                var list = new List<NamedObjectSavePropertyChange>();
                list.Add(nosVariableChange);
                ReactToPropertyListChanged(list);
            }

        }

        public static void ReactToPropertyListChanged(List<NamedObjectSavePropertyChange> namedObjectSavePropertyChangeList) =>
            CallMethodOnPlugin(
                plugin => plugin.ReactToChangedNamedObjectPropertyList(namedObjectSavePropertyChangeList),
                plugin => plugin.ReactToChangedNamedObjectPropertyList != null);

        public class NamedObjectSavePropertyChange
        {
            public NamedObjectSave NamedObjectSave { get; set; }
            public string ChangedPropertyName { get; set; }
            public object OldValue { get; set; }

            public bool RecordUndo { get; set; }

            public override string ToString()
            {
                return $"{NamedObjectSave} {ChangedPropertyName}";
            }
        }


        internal static void ReactToGluxUnload(bool isExiting)
        {
            CallMethodOnPlugin(
                plugin => plugin.ReactToUnloadedGlux(),
                plugin => plugin.ReactToUnloadedGlux != null,
                nameof(ReactToGluxUnload));

            // now we need to unregister code generators and other things automatically registered for plugins which are project-specific
            var projectSpecificPlugins = PluginManagerBase.GetProjectPluginManager().PluginContainers.Values;
            foreach(var pluginContainer in projectSpecificPlugins)
            {
                if(pluginContainer.IsEnabled && pluginContainer.Plugin is PluginBase asPluginBase)
                {
                    asPluginBase.UnregisterAllCodeGenerators();
                    asPluginBase.UnregisterAssetTypeInfos();
                    asPluginBase.ShutDown(PluginShutDownReason.GluxUnload);
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

                if (plugin.InitializeMenuHandler != null)
                    plugin.InitializeMenuHandler(mMenuStrip);
            }

            if (pluginToReenable is IMenuStripPlugin)
            {
                ((IMenuStripPlugin)pluginToReenable).InitializeMenu(mMenuStrip);
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
            CallMethodOnPlugin(plugin =>
            {
                plugin.AdjustDisplayedScreen(screenSave, screenSaveDisplayer);
            },
            plugin => plugin.AdjustDisplayedScreen != null,
            nameof(AdjustDisplayedScreen));
        }

        internal static void ModifyAddEntityWindow(AddEntityWindow addEntityWindow)
        {
            CallMethodOnPlugin(
                plugin => plugin.ModifyAddEntityWindow(addEntityWindow),
                plugin => plugin.ModifyAddEntityWindow != null);
        }

        internal static void ModifyAddScreenWindow(AddScreenWindow addScreenWindow)
        {

            CallMethodOnPlugin(
                plugin => plugin.ModifyAddScreenWindow(addScreenWindow),
                plugin => plugin.ModifyAddScreenWindow != null);
        }

        internal static void AdjustDisplayedEntity(EntitySave entitySave, EntitySavePropertyGridDisplayer entitySaveDisplayer) => CallMethodOnPlugin(
                plugin => plugin.AdjustDisplayedEntity(entitySave, entitySaveDisplayer),
                plugin => plugin.AdjustDisplayedEntity != null);

        internal static void AdjustDisplayedNamedObject(NamedObjectSave namedObject, NamedObjectPropertyGridDisplayer displayer)
        {
            CallMethodOnPlugin(
                delegate (PluginBase plugin)
                {
                    plugin.AdjustDisplayedNamedObject(namedObject, displayer);
                },
                plugin => plugin.AdjustDisplayedNamedObject != null,
                nameof(AdjustDisplayedNamedObject));
        }

        internal static void AdjustDisplayedReferencedFile(ReferencedFileSave referencedFileSave, ReferencedFileSavePropertyGridDisplayer displayer)
        {
            CallMethodOnPlugin(
                delegate (PluginBase plugin)
                {
                    plugin.AdjustDisplayedReferencedFile(referencedFileSave, displayer);
                },
                plugin => plugin.AdjustDisplayedReferencedFile != null,
                nameof(AdjustDisplayedReferencedFile));
        }

        static void CallMethodOnPluginNotUiThread(Action<PluginBase> methodToCall, string methodName)
        {
            var instances = mInstances.ToList();
            foreach (PluginManager manager in instances)
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



        static void CallMethodOnPlugin(Action<PluginBase> methodToCall, Predicate<PluginBase> predicate, [CallerMemberName] string methodName = null, bool doOnUiThread = true)
        {
            var instances = mInstances.ToArray();
            foreach (PluginManager manager in instances)
            {
                var plugins = manager.PluginContainers.Keys.Where(plugin => plugin is PluginBase)
                    .Select(item => item as PluginBase);
                if(predicate != null)
                {
                    plugins = plugins.Where(item => predicate(item));
                }

                PluginBase[] pluginArray = plugins.ToArray();

                for (int i = 0; i < pluginArray.Length; i++)
                {
                    PluginBase plugin = pluginArray[i];
                    PluginContainer container = manager.PluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        PluginCommand(() =>
                            {
                                methodToCall(plugin);
                            },container, "Failed in " + methodName, doOnUiThread);
                    }
                }
            }
        }

        /// <summary>
        /// Calls the plugin method in a Task.AddAsync, which will run in a task if not already in a task.
        /// </summary>
        /// <param name="methodToCall">The method to call</param>
        /// <param name="predicate">A predicate determining if the plugin should try to run - usually a null check</param>
        /// <param name="methodName">The name of the method making this call, optional, usually left empty and filled by CallerMemberName</param>
        /// <returns>A task which can be awaited for the task to finish.</returns>
        static Task CallMethodOnPluginAsync(Action<PluginBase> methodToCall, Predicate<PluginBase> predicate, [CallerMemberName] string methodName = null)
        {
            var task = TaskManager.Self.AddAsync(() =>
            {
                foreach (PluginManager manager in mInstances)
                {
                    var plugins = manager.PluginContainers.Keys.Where(plugin => plugin is PluginBase)
                        .Select(item => item as PluginBase);
                    if (predicate != null)
                    {
                        plugins = plugins.Where(item => predicate(item));
                    }

                    PluginBase[] pluginArray = plugins.ToArray();

                    foreach (var plugin in pluginArray)
                    {
                        PluginContainer container = manager.PluginContainers[plugin];

                        if (container.IsEnabled)
                        {
                            PluginCommand(() =>
                            {
                                methodToCall(plugin);
                            }, container, "Failed in " + methodName);
                        }
                    }
                }
            }, methodName + " for all plugins");

            return task;
        }

        static Task CallMethodOnPluginAsync(Func<PluginBase, Task> methodToCall, Predicate<PluginBase> predicate, [CallerMemberName] string methodName = null)
        {
            var task = TaskManager.Self.AddAsync(async () =>
            {
                foreach (PluginManager manager in mInstances)
                {
                    var plugins = manager.PluginContainers.Keys.Where(plugin => plugin is PluginBase)
                        .Select(item => item as PluginBase);
                    if (predicate != null)
                    {
                        plugins = plugins.Where(item => predicate(item));
                    }

                    PluginBase[] pluginArray = plugins.ToArray();

                    foreach (var plugin in pluginArray)
                    {
                        PluginContainer container = manager.PluginContainers[plugin];

                        if (container.IsEnabled)
                        {
                            await PluginCommand(() =>
                            {
                                return methodToCall(plugin);
                            }, container, "Failed in " + methodName);
                        }
                    }
                }
            }, methodName);

            return task;
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

        private static void PluginCommand(Action action, PluginContainer container, string message, bool doOnUiThread = true)
        {
            if (HandleExceptions)
            {
                // November 16, 2022
                // Before today, PluginCommand
                // always ran on a UI thread (if
                // possible). However, some commands
                // do not require UI thread, and they can 
                if (mMenuStrip.IsDisposed || !doOnUiThread)
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        var version = container.Plugin.Version;

                        message = $"{container.Name} Version {version} {message}";

                        container.Fail(e, message);

                        ReceiveError(message + "\n" + e.ToString());


                    }
                }
                else
                {
                    mMenuStrip.Invoke((MethodInvoker)delegate
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception e)
                        {
                            var version = container.Plugin.Version;

                            message = $"{container.Name} Version {version} {message}";

                            container.Fail(e, message);

                            ReceiveError(message + "\n" + e.ToString());


                        }
                    });
                }
            }
            else
            {
                action();
            }
        }

        private static async Task PluginCommand(Func<Task> func, PluginContainer container, string message)
        {
            if (HandleExceptions)
            {

                try
                {
                    await func();
                }
                catch (Exception e)
                {
                    var version = container.Plugin.Version;

                    message = $"{container.Name} Version {version} {message}";

                    container.Fail(e, message);

                    ReceiveError(message + "\n" + e.ToString());


                }
            }
            else
            {
                await func();
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

        internal static bool? GetIfUsesContentPipeline(string fileAbsolute)
        {
            bool? toReturn = null;

            CallMethodOnPlugin(
                plugin => toReturn = plugin.GetIfUsesContentPipeline(fileAbsolute),
                plugin => plugin.GetIfUsesContentPipeline != null,
                // no need for UI thread on this call:
                doOnUiThread:false);

            return toReturn;
        }

        private static PluginContainer GetContainerFor(PluginBase plugin)
        {
            for(int i = 0; i < mInstances.Count; i++)
            {
                if(mInstances[i].PluginContainers.ContainsKey(plugin))
                {
                    return mInstances[i].PluginContainers[plugin];
                }

            }
            return null;
        }

        public static List<VariableDefinition> GetVariableDefinitionsFor(IElement element)
        {
            List<VariableDefinition> toReturn = new List<VariableDefinition>();
            CallMethodOnPlugin(
                plugin => toReturn.AddRange(plugin.GetVariableDefinitionsForElement(element)),
                plugin => plugin.GetVariableDefinitionsForElement != null);
            return toReturn;
        }

        public static bool TryHandleTreeNodeDoubleClicked(ITreeNode treeNode)
        {
            bool handled = false;

            CallMethodOnPlugin(
                plugin =>
                {
                    handled = plugin.TryHandleTreeNodeDoubleClicked(treeNode) || handled;
                },
                plugin => plugin.TryHandleTreeNodeDoubleClicked != null);


            return handled;
        }

        public static void ReactToFileBuildCommand(ReferencedFileSave rfs)
        {
            CallMethodOnPlugin(
                plugin => plugin.ReactToFileBuildCommand(rfs),
                plugin => plugin.ReactToFileBuildCommand != null);
        }

        public static void ReactToImportedElement(GlueElement newElement)
        {
            CallMethodOnPlugin(
                plugin => plugin.ReactToImportedElement(newElement),
                plugin => plugin.ReactToImportedElement != null);
        }

        public class ObjectContainerChange
        {
            public NamedObjectSave ObjectMoved { get; set; }
            public NamedObjectSave NewContainer { get; set; }

        }
        public static Task ReactToObjectListContainerChanged(List<ObjectContainerChange> objectContainerChanges)
        {
            return CallMethodOnPluginAsync(async (plugin) =>
            {
                var handledByList = false;
                if (plugin.ReactToObjectListContainerChanged != null)
                {
                    await plugin.ReactToObjectListContainerChanged(objectContainerChanges);
                    handledByList = true;
                }

                if (!handledByList)
                {
                    foreach (var change in objectContainerChanges)
                    {
                        plugin.ReactToObjectContainerChanged(change.ObjectMoved, change.NewContainer);
                    }
                }
            },
            plugin => plugin.ReactToObjectListContainerChanged != null || plugin.ReactToObjectContainerChanged != null);
        }
            


        public static Task ReactToObjectContainerChanged(NamedObjectSave objectMoved, NamedObjectSave newContainer)
        {
            var wrapper = new List<ObjectContainerChange>();

            var item = new ObjectContainerChange();
            item.ObjectMoved = objectMoved;
            item.NewContainer = newContainer;

            wrapper.Add(item);

            return ReactToObjectListContainerChanged(wrapper);

        }

        public static void ReactToMainWindowMoved()
        {
            CallMethodOnPlugin(
                plugin => plugin.ReactToMainWindowMoved(),
                plugin => plugin.ReactToMainWindowMoved != null);
        }

        public static void ReactToMainWindowResizeEnd()
        {
            CallMethodOnPlugin(
                plugin => plugin.ReactToMainWindowResizeEnd(),
                plugin => plugin.ReactToMainWindowResizeEnd != null);
        }

        public static void RefreshTreeNodeFor(GlueElement element, TreeNodeRefreshType treeNodeRefreshType) => CallMethodOnPlugin(
                plugin => plugin.RefreshTreeNodeFor(element, treeNodeRefreshType),
                plugin => plugin.RefreshTreeNodeFor != null);

        public static void RefreshGlobalContentTreeNode() => CallMethodOnPlugin(
                plugin => plugin.RefreshGlobalContentTreeNode(),
                plugin => plugin.RefreshGlobalContentTreeNode != null);

        public static void RefreshDirectoryTreeNodes() => CallMethodOnPlugin(
                plugin => plugin.RefreshDirectoryTreeNodes(),
                plugin => plugin.RefreshDirectoryTreeNodes != null);

        public static void ReactToFocusOnTreeView() => CallMethodOnPlugin(
                plugin => plugin.FocusOnTreeView(),
                plugin => plugin.FocusOnTreeView != null);

        public static void ReactToCtrlF() => CallMethodOnPlugin(
                plugin => plugin.ReactToCtrlF(),
                plugin => plugin.ReactToCtrlF != null);

        public static void ReactToCtrlKey(System.Windows.Input.Key key) => CallMethodOnPlugin(
                plugin => plugin.ReactToCtrlKey(key),
                plugin => plugin.ReactToCtrlKey != null);

        public static void ReactToGrabbedTreeNodeChanged(ITreeNode treeNode, TreeNodeAction treeNodeAction) => CallMethodOnPlugin(
                plugin => plugin.GrabbedTreeNodeChanged(treeNode, treeNodeAction),
                plugin => plugin.GrabbedTreeNodeChanged != null);

        public static void ReactToGlobalTimer()
        {
            try
            {
                CallMethodOnPlugin(
                    plugin => plugin.ReactToGlobalTimer(),
                    plugin => plugin.ReactToGlobalTimer != null);

            }
            catch(InvalidOperationException)
            {
                // no biggie, this means the plugins changed when this was called. it is called so frequently,
                // so we don't want to make a copy of the list.
            }
        }

        public static Task ReactToStateCategoryExcludedVariablesChangedAsync(StateSaveCategory category, string variableName, StateCategoryVariableAction action) => 
            CallMethodOnPluginAsync(
                plugin => plugin.ReactToStateCategoryExcludedVariablesChanged(category, variableName, action),
                plugin => plugin.ReactToStateCategoryExcludedVariablesChanged != null);

        public static Task ReactToScreenJsonSaveAsync(string screenName, string json) =>
            CallMethodOnPluginAsync(
                plugin => plugin.ReactToScreenJsonSave(screenName, json),
                plugin => plugin.ReactToScreenJsonSave != null,
                nameof(ReactToEntityJsonSaveAsync) + $"for {screenName}");

        public static Task ReactToEntityJsonSaveAsync(string entityName, string json) =>
            CallMethodOnPluginAsync(
                plugin => plugin.ReactToEntityJsonSave(entityName, json),
                plugin => plugin.ReactToEntityJsonSave != null, 
                nameof(ReactToEntityJsonSaveAsync) + $"for {entityName}");

        public static Task ReactToGlueJsonSaveAsync(string json) =>
            CallMethodOnPluginAsync(
                plugin => plugin.ReactToGlueJsonSave(json),
                plugin => plugin.ReactToGlueJsonSave != null);

        public static Task ReactToScreenJsonLoadAsync(string screenName, string json) =>
            CallMethodOnPluginAsync(
                plugin => plugin.ReactToScreenJsonLoad(screenName, json),
                plugin => plugin.ReactToScreenJsonLoad != null);

        public static Task ReactToEntityJsonLoadAsync(string screenName, string json) =>
            CallMethodOnPluginAsync(
                plugin => plugin.ReactToEntityJsonLoad(screenName, json),
                plugin => plugin.ReactToEntityJsonLoad != null);

        public static Task ReactToGlueJsonLoadAsync(string json) =>
            CallMethodOnPluginAsync(
                plugin => plugin.ReactToGlueJsonLoad(json),
                plugin => plugin.ReactToGlueJsonLoad != null);

        #endregion

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
                        MessageBox.Show($"A plugin has had an error.\n" +
                            $"Shutting down the plugin {plugin.Name} version {plugin.Plugin.Version} at file location\n{plugin.AssemblyLocation}\n\n" +
                            $"Additional information:\n\n" + 
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

            CallMethodOnPlugin(plugin =>
            {
                if (!wasAddHandled)
                {
                    wasAddHandled = plugin.TryAddContainedObjects(sourceFile, listToAddTo);
                }
            },
            plugin => plugin.TryAddContainedObjects != null);

            ResumeRelativeDirectory("TryAddContainedObjects");
            return wasAddHandled;
        }

        internal static void ReactToSyncedProjectLoad(ProjectBase projectBase)
        {
            SaveRelativeDirectory();

            CallMethodOnPlugin(
                plugin => plugin.ReactToLoadedSyncedProject(projectBase),
                plugin => plugin.ReactToLoadedSyncedProject != null);

            ResumeRelativeDirectory(nameof(ReactToSyncedProjectLoad));
        }

        public static TypeConverter GetTypeConverter(IElement container, NamedObjectSave instance, Type memberType, string memberName, string customTypeName)
        {
            TypeConverter toReturn = null;

            SaveRelativeDirectory();

            CallMethodOnPlugin(
                plugin =>
                {
                    var foundValue = plugin.GetTypeConverter(container, instance, memberType, memberName, customTypeName);
                    if (foundValue != null)
                    {
                        toReturn = foundValue;
                    }

                },
                plugin => plugin.GetTypeConverter != null);

            ResumeRelativeDirectory(nameof(GetTypeConverter));

            return toReturn;
        }

        public static void GetEventSignatureArgs(NamedObjectSave namedObjectSave, EventResponseSave eventResponseSave, out string type, out string args)
        {
            string foundType = null;
            string foundArgs = null;

            CallMethodOnPlugin(
                plugin =>
                {
                    plugin.GetEventSignatureArgs(namedObjectSave, eventResponseSave, out string tempFoundType, out string tempFoundArgs);
                    if (tempFoundType != null)
                    {
                        foundType = tempFoundType;
                        foundArgs = tempFoundArgs;
                    }
                },
                plugin => plugin.GetEventSignatureArgs != null);

            type = foundType;
            args = foundArgs;
        }

        public static void WriteInstanceVariableAssignment(NamedObjectSave namedObject, ICodeBlock codeBlock, InstructionSave instructionSave)
        {
            TypeConverter toReturn = null;

            SaveRelativeDirectory();

            CallMethodOnPlugin(
                plugin => plugin.WriteInstanceVariableAssignment(namedObject, codeBlock, instructionSave),
                plugin => plugin.WriteInstanceVariableAssignment != null);

            ResumeRelativeDirectory(nameof(WriteInstanceVariableAssignment));
        }

        internal static void AddEventsForObject(NamedObjectSave namedObjectSave, List<ExposableEvent> listToFill)
        {
            SaveRelativeDirectory();

            CallMethodOnPlugin(
                plugin => plugin.AddEventsForObject(namedObjectSave, listToFill),
                plugin => plugin.AddEventsForObject != null);

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
                    FlatRedBall.Glue.Plugins.ExportedImplementations.GlueState.Self.CurrentCodeProjectFileName.FullPath);
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
