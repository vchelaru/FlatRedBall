using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.IO;
using System.IO;
using System.Windows.Forms;
using System.CodeDom.Compiler;

using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins
{
    #region PluginCategories enum

    public enum PluginCategories
    {
        Global = 1,
        ProjectSpecific = 2,
        All = Global | ProjectSpecific
    }

    #endregion

    public class PluginManagerBase
    {
        protected List<Assembly> mExternalAssemblies = new List<Assembly>();
        protected List<string> mReferenceListInternal = new List<string>();
        protected List<string> mReferenceListLoaded = new List<string>();
        protected List<string> mReferenceListExternal = new List<string>();

        protected Dictionary<IPlugin, PluginContainer> mPluginContainers = new Dictionary<IPlugin, PluginContainer>();

        protected const String ReferenceFileName = "References.txt";
        protected const String CompatibilityFileName = "Compatibility.txt";
        protected bool mError = false;

        protected static PluginManagerBase mGlobalInstance;
        protected static PluginManagerBase mProjectInstance;

        protected static List<PluginManagerBase> mInstances = new List<PluginManagerBase>();
        protected bool mGlobal;

        public bool HasFinishedInitialization = false;

        private static readonly List<Assembly> mAddGlobalOnInitialize = new List<Assembly>();
        public static List<Assembly> AddGlobalOnInitialize
        {
            get { return mAddGlobalOnInitialize; }
        }

        public static PluginManagerBase GetGlobalPluginManager()
        {
            return mGlobalInstance;
        }

        public static PluginManagerBase GetProjectPluginManager()
        {
            return mProjectInstance;
        }


        public static List<PluginManagerBase> GetInstances()
        {
            return mInstances;
        }

        public static List<PluginContainer> AllPluginContainers
        {
            get
            {
                return mInstances.SelectMany(item => item.mPluginContainers.Values).ToList();
            }
        }

        public Dictionary<IPlugin, PluginContainer> PluginContainers
        {
            get { return mPluginContainers; }
        }

        public AssemblyLoadContext AssemblyContext { get; set; }

        protected virtual void LoadReferenceLists()
        {
            // We use absolute paths for some of the .dlls and .exes
            // because if we don't, then Glue looks for them in the Startup
            // path, which could depend on whether Glue is launched from a shortcut
            // or not - this is really common for released versions.
            string executablePath = FileManager.GetDirectory(System.Reflection.Assembly.GetAssembly(typeof(PluginManagerBase)).Location);

            //Load Internal List
            AddIfExists(executablePath + "EditorObjectsXna.dll");
            AddIfExists(executablePath + "FlatRedBall.dll");
            AddIfExists(executablePath + "FlatRedBall.Plugin.dll");

            AddIfExists(executablePath + "GlueSaveClasses.dll");
            AddIfExists(executablePath + "Glue.exe");
            AddIfExists(executablePath + "GlueView.exe");
            AddIfExists(executablePath + "GluxViewManager.dll");
            AddIfExists(executablePath + "Ionic.Zip.dll");
            // I think this is automatcially handled by Glue
            //AddIfExists(executablePath + "InteractiveInterface.dll");

            mReferenceListInternal.Add("Microsoft.CSharp.dll");
            mReferenceListInternal.Add("System.dll");
            mReferenceListInternal.Add("System.ComponentModel.Composition.dll");
            mReferenceListInternal.Add("System.Core.dll");
            mReferenceListInternal.Add("System.Data.dll");
            mReferenceListInternal.Add("System.Data.DataSetExtensions.dll");
            mReferenceListInternal.Add("System.Drawing.dll");
            mReferenceListInternal.Add("System.Windows.Forms.dll");
            mReferenceListInternal.Add("System.Xml.dll");
            mReferenceListInternal.Add("System.Xml.Linq.dll");

            mReferenceListLoaded.Add("Microsoft.Xna.Framework.dll");
            mReferenceListLoaded.Add("Microsoft.Xna.Framework.Graphics.dll");
            mReferenceListLoaded.Add("Microsoft.Xna.Framework.Game.dll");
            mReferenceListLoaded.Add("Microsoft.Xna.Framework.Content.Pipeline.dll");
        }

        protected void AddIfExists(string absoluteName)
        {
            if (File.Exists(absoluteName))
            {
                mReferenceListInternal.Add(absoluteName);
            }
        }

        protected void LoadExternalReferenceList(string filePath)
        {
            string ReferenceFilePath = filePath + "\\" + ReferenceFileName;
            mReferenceListExternal = new List<string>();

            if (File.Exists(ReferenceFilePath))
            {
                using (StreamReader file = new StreamReader(ReferenceFilePath))
                {
                    string line;

                    while ((line = file.ReadLine()) != null)
                    {
                        if (!String.IsNullOrEmpty(line) &&
                           !String.IsNullOrEmpty(line.Trim()))
                        {
                            if (FileManager.FileExists(line.Trim()))
                            {
                                string absolute = FileManager.MakeAbsolute(line.Trim());

                                if (!mReferenceListInternal.Contains(absolute))
                                {
                                    mReferenceListExternal.Add(absolute);
                                    mExternalAssemblies.Add(Assembly.LoadFrom(absolute));
                                }
                            }
                        }
                    }
                }
            }
        }

        public PluginManagerBase(bool global)
        {
            mGlobal = global;
        }

        protected virtual void InstantiateAllListsAsEmpty()
        {

        }

        protected static bool PopulateCatalog(PluginManagerBase instance, string absoluteFilePath, List<string> pluginsToIgnore = null)
        {
            instance.mError = false;

            ResolveEventHandler reh = new ResolveEventHandler(instance.currentDomain_AssemblyResolve);
            bool succeeded = true;
            try
            {
                AppDomain currentDomain = AppDomain.CurrentDomain;
                //currentDomain.AssemblyResolve += reh;

                AggregateCatalog catalog = instance.CreateCatalog(absoluteFilePath, pluginsToIgnore);

                var container = new CompositionContainer(catalog);
                container.ComposeParts(instance);

                succeeded = true;

            }
            catch (Exception e)
            {
                // If we get here then output won't even print out or even load.
                // This invalidates all plugins, so we should show ane rror.
                MessageBox.Show("Error in a plugin that shut down the plugin system: \n\n" + e.ToString());


                instance.CompileErrors.Add("Error trying to load plugins: \r\n\r\n" + e.ToString());

                instance.InstantiateAllListsAsEmpty();

                succeeded = false;
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= reh;
            }
            return succeeded;
        }
   

        //private static void CleanUnreferencedDlls(AggregateCatalog catalog, PluginManagerBase instance)
        //{
        //    List<string> allFiles = new List<string>();
        //    allFiles.AddRange(
        //        instance.mReferenceListExternal.Select(item => FileManager.RemoveExtension(FileManager.RemovePath(item))));
        //    allFiles.AddRange(
        //        instance.mReferenceListInternal.Select(item => FileManager.RemoveExtension(FileManager.RemovePath(item))));
        //    allFiles.AddRange(
        //        instance.mReferenceListLoaded.Select(item => FileManager.RemoveExtension(FileManager.RemovePath(item))));

        //    allFiles.AddRange(catalog.Catalogs.Select
        //        (item=>FileManager.RemovePath((item as AssemblyCatalog).Assembly.GetName().Name)));
        //    allFiles.Add("mscorlib");


        //    List<string> assembliesToSkip = new List<string>()
        //    {
        //        "Glue",
        //        "FlatRedBall.Plugin"

        //    };

        //    for (int i = catalog.Catalogs.Count - 1; i > -1; i--)
        //    {
        //        Assembly assembly = (catalog.Catalogs.ElementAt(i) as AssemblyCatalog).Assembly;

        //        if (assembliesToSkip.Contains(assembly.GetName().Name) == false)
        //        {
        //            foreach (var toFind in assembly.GetReferencedAssemblies())
        //            {

        //                string toFindName = toFind.Name;

        //                // We skip over stuff that we know is always there

        //                bool found = allFiles.Contains(toFindName);

        //                if (!found)
        //                {
        //                    int m = 3;
        //                }
        //            }
        //        }
        //    }
        //}

        private Assembly currentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            foreach (Assembly item in mExternalAssemblies)
            {
                if (item.FullName == args.Name)
                {
                    return item;
                }
            }

            return null;
        }

        private AggregateCatalog CreateCatalog(string absoluteFilePath, List<string> pluginsToIgnore = null)
        {

            mExternalAssemblies.Clear();
            LoadReferenceLists();

            var returnValue = new AggregateCatalog();

            var pluginDirectories = GetPluginDirectories(absoluteFilePath);

            foreach (var directory in pluginDirectories)
            {
                AddPluginsFromDirectory(pluginsToIgnore, returnValue, directory);
            }

            if (mGlobal)
            {
                Assembly executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                var newCatalog = new AssemblyCatalog(executingAssembly);
                returnValue.Catalogs.Add(newCatalog);



                Assembly entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
                if (entryAssembly != executingAssembly && entryAssembly != null)
                {
                    newCatalog = new AssemblyCatalog(entryAssembly);
                    returnValue.Catalogs.Add(newCatalog);


                }

                foreach (var assembly in AddGlobalOnInitialize)
                {
                    newCatalog = new AssemblyCatalog(assembly);

                    returnValue.Catalogs.Add(newCatalog);

                }
            }

            return returnValue;
        }

        private List<string> GetPluginDirectories(string absoluteFilePath)
        {
            var pluginDirectories = new List<string>();

            if (mGlobal)
            {
                var paths = new List<string>
                                         {
                                             FileManager.GetDirectory(Application.ExecutablePath) + "Plugins",
                                             absoluteFilePath
                                         };
                // Glue startup is super verbose, we can quite it down now that plugins seem to be working fine:
                //foreach (string path in paths)
                //{
                //    CompilePluginOutput("Looking for plugins in " + path);
                //}

                pluginDirectories.AddRange(paths.Where(Directory.Exists).SelectMany(Directory.GetDirectories).Select(item=>item + "/"));

                //foreach (string path in pluginDirectories)
                //{
                //    CompilePluginOutput("Found path for plugins " + path);
                //}
            }
            else
            {
                AddDirectoriesForInstance(pluginDirectories, absoluteFilePath);
            }
            return pluginDirectories;
        }

        private void AddPluginsFromDirectory(List<string> pluginsToIgnore, AggregateCatalog returnValue, string plugin)
        {
            try
            {
                bool shouldProcessPlugin = true;
                // GlueView is a special case, so we should skip that
                string pluginToLower = plugin.Replace("\\", "/");
                if (pluginToLower.EndsWith("/glueview", StringComparison.OrdinalIgnoreCase)
                    || pluginToLower.EndsWith("/glueview/", StringComparison.OrdinalIgnoreCase))
                {
                    shouldProcessPlugin = false;
                }

                if (shouldProcessPlugin)
                {
                    // We had a && false here, so I don't think we use this ignored check, do we?
                    bool isIgnored = pluginsToIgnore != null && pluginsToIgnore.Any(item => 
                        FileManager.Standardize(item).Equals(FileManager.Standardize(plugin), StringComparison.OrdinalIgnoreCase));

                    if (!isIgnored)
                    {
                        HandleAddingPluginInDirectory(returnValue, plugin);
                    }
                }
            }
            catch (Exception ex)
            {
                CompileErrors.Add("Error loading plugin at " + plugin + "\r\n\r\n" + ex);
            }
        }

        private void HandleAddingPluginInDirectory(AggregateCatalog returnValue, string pluginDirectory)
        {
            var assemblies = new HashSet<Assembly>();

            var assembliesFiles = Directory.GetFiles(pluginDirectory, "*.dll");

            foreach (string assemblyName in assembliesFiles)
            {
                try
                {
                    if (IsAssemblyAlreadyReferenced(assemblyName, out var assembly))
                    {
                        string message = $"Info: {pluginDirectory} - Skipping over assembly {assemblyName} because it is already loaded by a different plugin. If you are working on this plugin, you may not need to have this .dll copied. Otherwise, you can ignore this message.";

                        CompileOutput.Add(message);
                    }
                    else
                    {
                        assembly = AssemblyContext.LoadFromAssemblyPath(assemblyName);
                    }
                    
                    assemblies.Add(assembly);
                }
                catch (Exception ex)
                {
                    CompileErrors.Add(string.Format("Failed to load {0}: {1}", assemblyName, ex.Message));
                }
            }

            AggregateCatalog catalogToMakeSureStuffIsLinked = new AggregateCatalog();
            foreach (var assembly in assemblies)
            {
                var catalog = new AssemblyCatalog(assembly);
                catalogToMakeSureStuffIsLinked.Catalogs.Add(catalog);
            }

            bool failed = false;
            try
            {
                var container = new CompositionContainer(catalogToMakeSureStuffIsLinked);
                container.GetExports<object>();
            }
            catch (Exception e)
            {
                string message = "";
                message += "Error trying to load plugins from directory:     " + pluginDirectory;

                if (e is ReflectionTypeLoadException)
                {
                    foreach (var innerException in (e as ReflectionTypeLoadException).LoaderExceptions)
                    {
                        message += "\r\n" + innerException.ToString();
                    }
                }
                else
                {
                    message += "\r\n" + e.ToString();
                }
                CompileErrors.Add(message);

                failed = true;
            }

            if (!failed)
            {
                foreach (var assemblyCatalog in catalogToMakeSureStuffIsLinked.Catalogs)
                {
                    returnValue.Catalogs.Add(assemblyCatalog);
                }
            }

        }

        private void HandleAddingDynamicallyCompiledPlugin(AggregateCatalog returnValue, string plugin, CompilerResults compileResult)
        {
            if (compileResult.Errors != null && compileResult.Errors.HasErrors)
            {
                var errors = new StringBuilder();
                var filename = Path.GetFileName(plugin);
                foreach (CompilerError err in compileResult.Errors)
                {
                    errors.Append(string.Format("\r\n{0}({1},{2}): {3}: {4}",
                                filename, err.Line, err.Column,
                                err.ErrorNumber, err.ErrorText));
                }
                CompileErrors.Add("Error loading script " + plugin + "\r\n" + errors.ToString());
            }
            else
            {
                var newCatalog = new AssemblyCatalog(compileResult.CompiledAssembly);


                returnValue.Catalogs.Add(newCatalog);

                //foreach (string assm in mReferenceListExternal)
                //{
                //    Assembly ass = Assembly.LoadFrom(assm);
                //    var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).Select(a => a.Location);

                //    returnValue.Catalogs.Add(newCatalog);
                //}
            }
        }

        private bool IsAssemblyAlreadyReferenced(string assemblyName, out Assembly alreadyLoadedAssembly)
        {
            alreadyLoadedAssembly = null;

            string strippedArgumentName = FileManager.RemovePath(assemblyName);
            string fileName = FileManager.RemovePath(assemblyName);
            
            if(mReferenceListInternal.Contains(fileName))
            {
                return true;
            }

            alreadyLoadedAssembly = AssemblyContext.Assemblies
                .FirstOrDefault(x => FileManager.RemovePath(x.Location).Equals(strippedArgumentName));

            return alreadyLoadedAssembly != null;
        }

        protected virtual void AddDirectoriesForInstance(List<string> pluginDirectories, string searchPath)
        {

        }

        private CompilerResults CompilePlugin(string filepath)
        {
            bool hasCs = System.IO.Directory.GetFiles(filepath).Any(item => FileManager.GetDirectory(item) == "cs");

            if(!hasCs)
            {
                return null;
            }

            CompilePluginOutput("Attempting to compile plugin : " + filepath);

            string whyIsntCompatible = GetWhyIsntCompatible(filepath);

            if (string.IsNullOrEmpty(whyIsntCompatible))
            {
                LoadExternalReferenceList(filepath);

                string output;
                string name = Assembly.GetEntryAssembly().GetName().Name;
                string details;
                // need to fix this, or pass it a root folder or something.
                CompilerResults results = null;

                // Make sure a non-null result was returned
                if (results == null)
                {
                    return null;
                }

                string outputString = "";
                foreach (string s in results.Output)
                {
                    outputString += s + "   ";
                }

                string errorString = "";
                foreach (var s in results.Errors)
                {
                    errorString += s.ToString() + "   ";
                }

                if (!results.Errors.HasErrors && results.CompiledAssembly != null)
                {
                    Type[] types = results.CompiledAssembly.GetTypes();

                    bool foundAnExportedType = false;
                    foreach(Type type in types)
                    {
                        var attributes = type.GetCustomAttributes(typeof(ExportAttribute), true);

                        if (attributes != null && attributes.Length > 0)
                        {
                            foundAnExportedType = true;
                            break;
                        }
                    }

                    if (!foundAnExportedType)
                    {
                        CompilePluginError("Could not find any plugins with the [Export] tag for plugin than " + filepath);
                    }
                }

                string message = null;
                // I don't think we care about this anymore - it just clutters output a ton
                    //"Plugin compile output: " + outputString;
                if (!string.IsNullOrEmpty(errorString))
                {
                    message += "Compile errors: " + errorString;
                }

                CompilePluginError(message);
                return results;
            }
            else
            {
                CompilePluginOutput("Error on plugin in folder " + filepath + " : " + whyIsntCompatible);
            }

            return null;
        }

        protected virtual void CompilePluginOutput(string problem)
        {

        }

        protected virtual void CompilePluginError(string problem)
        {

        }

        private static string GetWhyIsntCompatible(string filepath)
        {
            var compatibilityFilePath = filepath + @"\" + CompatibilityFileName;

            //Check for compatibility file
            if (File.Exists(compatibilityFilePath))
            {
                string value;

                //Get compatibility timestamp
                using (var file = new StreamReader(compatibilityFilePath))
                {
                    value = file.ReadToEnd();
                }

                DateTime compatibilityTime;

                if (DateTime.TryParse(value, out compatibilityTime))
                {
                    DateTime glueTimeStamp = new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime;
                    //If compatibility timestamp is newer than current Glue's timestamp, then don't compile plugin
                    if (glueTimeStamp < compatibilityTime)
                    {
                        return "Glue time stamp is " + glueTimeStamp + " which is not newer than the plugin's time " + compatibilityTime;
                    }
                }
            }

            return null;
        }

        protected void StartupPlugin(IPlugin plugin)
        {
            // See if the plugin already exists - it may implement multiple interfaces
            if (!mPluginContainers.ContainsKey(plugin))
            {
                PluginContainer pluginContainer = new PluginContainer(plugin);
                mPluginContainers.Add(plugin, pluginContainer);

                try
                {
                    plugin.StartUp();
                    plugin.ReactToPluginEventAction += HandlePluginEvent;
                    plugin.ReactToPluginEventWithReturnAction += HandlePluginEventWithReturn;
                }
                catch (Exception e)
                {
                    pluginContainer.Fail(e, "Plugin failed in StartUp");
                }
            }
        }

        private void HandlePluginEvent(IPlugin callingPlugin, string eventName, string payload)
        {
            // Vic asks - why is this on Task.Run? It kills the call stack and offers no benefit...
            //Task.Run(() =>
            {
                if (mPluginContainers.ContainsKey(callingPlugin) && mPluginContainers[callingPlugin].IsEnabled)
                {
                    foreach (var pluginContainer in mPluginContainers.Values)
                    {
                        if (pluginContainer.IsEnabled)
                        {
                            try
                            {
                                pluginContainer.Plugin.HandleEvent(eventName, payload);
                            }
                            catch (Exception e)
                            {
                                pluginContainer.Fail(e, $"Plugin {pluginContainer.Name} failed while handling event: {eventName}");
                            }
                        }
                    }
                }
            }
            //);
        }

        private void HandlePluginEventWithReturn(IPlugin callingPlugin, string eventName, string payload)
        {
            Task.Run(async () =>
            {
                if (mPluginContainers.ContainsKey(callingPlugin) && mPluginContainers[callingPlugin].IsEnabled)
                {
                    foreach (var pluginContainer in mPluginContainers.Values)
                    {
                        if (pluginContainer.IsEnabled && pluginContainer.Plugin != callingPlugin)
                        {
                            try
                            {
                                var returnValue = await pluginContainer.Plugin.HandleEventWithReturn(eventName, payload);

                                if(returnValue != null)
                                {
                                    callingPlugin.HandleEventResponseWithReturn(returnValue);
                                    return;
                                }
                            }
                            catch (Exception e)
                            {
                                pluginContainer.Fail(e, $"Plugin {pluginContainer.Name} failed while handling event: {eventName}");
                            }
                        }
                    }
                }
            });
        }

        protected void AddDisabledPlugin(string folder)
        {
            NotLoadedPlugin plugin = new NotLoadedPlugin();
            plugin.FriendlyName = FileManager.RemovePath(folder);
            if(plugin.FriendlyName.EndsWith("/") || plugin.FriendlyName.EndsWith("\\"))
            {
                plugin.FriendlyName = plugin.FriendlyName.Substring(0, plugin.FriendlyName.Length - 1);
            }

            PluginContainer container = new PluginContainer(plugin);

            container.AssemblyLocation = folder + "/unknown.dll";

            mPluginContainers.Add(plugin, container);
            container.IsEnabled = false;

            // don't do any startup or anything
        }
        
        protected static bool ShouldProcessPluginManager(PluginCategories pluginCategories, PluginManagerBase pluginManager)
        {
            return (pluginManager.mGlobal && (pluginCategories & PluginCategories.Global) == PluginCategories.Global) ||
                                (!pluginManager.mGlobal && (pluginCategories & PluginCategories.ProjectSpecific) == PluginCategories.ProjectSpecific);
        }

        public static bool ShutDownPlugin(IPlugin pluginToShutDown)
        {
            return ShutDownPlugin(pluginToShutDown, PluginShutDownReason.PluginInitiated);
        }

        public static bool ShutDownPlugin(IPlugin pluginToShutDown,
            PluginShutDownReason shutDownReason)
        {
            bool doesPluginWantToShutDown = true;
            PluginContainer container;

            if (mGlobalInstance.mPluginContainers.ContainsKey(pluginToShutDown))
            {
                container = mGlobalInstance.mPluginContainers[pluginToShutDown];
            }
            else
            {
                container = mProjectInstance.mPluginContainers[pluginToShutDown];
            }

            try
            {
                doesPluginWantToShutDown =
                    container.Plugin.ShutDown(shutDownReason);
            }
            catch (Exception exception)
            {
                doesPluginWantToShutDown = true;
            }



            if (doesPluginWantToShutDown)
            {
                container.IsEnabled = false;

                foreach (Control control in container.Controls)
                {
                    // If the plugin hasn't removed the containers then we
                    // need to do the removal here - we don't want old UI floating
                    // around.
                    control.Parent.Controls.Remove(control);
                }
            }

            return doesPluginWantToShutDown;

        }

        protected virtual void StartAllPlugins(List<string> pluginsToIgnore = null)
        {

        }

        public bool LoadPlugins(string absoluteFilePath, List<string> pluginsToIgnore = null)
        {
            CompileOutput = new List<string>();
            CompileErrors = new List<string>();

            bool succeeded = PopulateCatalog (this, absoluteFilePath, pluginsToIgnore);

            if (succeeded)
            {
                StartAllPlugins(pluginsToIgnore);

            }

            HasFinishedInitialization = true;
            return succeeded;
        }



        public List<string> CompileOutput { get; private set; }
        public List<string> CompileErrors { get; private set; }
        public void RegisterControlForPlugin(Control control, IPlugin owner)
        {
            this.mPluginContainers[owner].Controls.Add(control);
        }
    }
}
