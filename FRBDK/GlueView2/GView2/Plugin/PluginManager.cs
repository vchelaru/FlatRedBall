using FlatRedBall.IO;
using GlueView2.Patterns;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GlueView2.Plugin
{
    class PluginManager : Singleton<PluginManager>
    {
        static PluginSettingsSave mPluginSettingsSave = new PluginSettingsSave();


        public static string PluginFolder
        {
            get
            {
                return FileManager.GetDirectory(Application.ExecutablePath) + "Plugins\\";
            }
        }

        private Dictionary<IPlugin, PluginContainer> mPluginContainers = new Dictionary<IPlugin, PluginContainer>();


        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<PluginBase> Plugins { get; set; }

        private List<string> mReferenceListInternal = new List<string>();
        private List<string> mReferenceListLoaded = new List<string>();
        private List<string> mReferenceListExternal = new List<string>();

        private List<Assembly> mExternalAssemblies = new List<Assembly>();

        private const String ReferenceFileName = "References.txt";
        private const String CompatibilityFileName = "Compatibility.txt";


        static string PluginSettingsSaveFileName
        {
            get
            {
                return FileManager.UserApplicationDataForThisApplication + "GlueView2PluginSettings.xml";
            }
        }


        public void Initialize()
        {
            LoadPluginSettings();
            LoadPlugins();
        }

        private void LoadPlugins()
        {
            #region Get the Catalog

            ResolveEventHandler reh = new ResolveEventHandler(currentDomain_AssemblyResolve);

            try
            {
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += reh;
                AggregateCatalog catalog = CreateCatalog();



                var container = new CompositionContainer(catalog);
                container.ComposeParts(this);
            }
            catch (Exception e)
            {
                string error = "Error loading plugins";
                if (e is ReflectionTypeLoadException)
                {
                    error += "Error is a reflection type load exception\n";
                    var loaderExceptions = (e as ReflectionTypeLoadException).LoaderExceptions;

                    foreach (var loaderException in loaderExceptions)
                    {
                        error += "\n" + loaderException.ToString();
                    }
                }
                else
                {
                    error += "\n" + e.Message;

                    if (e.InnerException != null)
                    {
                        error += "\n Inner Exception:\n" + e.InnerException.Message;
                    }
                }
                MessageBox.Show(error);

                Plugins = new List<PluginBase>();

                return;
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= reh;
            }

            #endregion

            #region Start all plugins

            foreach (PluginBase plugin in Plugins)
            {

                //// We used to do this all in an assign references method,
                //// but we now do it here so that the Startup function can have
                //// access to these references.
                //if (plugin is MainWindowPlugin)
                //{
                //    ((MainWindowPlugin)plugin).MainWindow = mainWindow;
                //}

                //plugin.MenuStrip = mainWindow.MainMenuStrip;

                StartupPlugin(plugin, this);
            }

            #endregion
        }


        internal static void StartupPlugin(IPlugin plugin, PluginManager instance)
        {

            // See if the plugin already exists - it may implement multiple interfaces
            if (!instance.mPluginContainers.ContainsKey(plugin))
            {
                PluginContainer pluginContainer = new PluginContainer(plugin);
                instance.mPluginContainers.Add(plugin, pluginContainer);

                try
                {
                    plugin.UniqueId = plugin.GetType().FullName;


                    if (!mPluginSettingsSave.DisabledPlugins.Contains(plugin.UniqueId))
                    {

                        plugin.StartUp();
                    }
                    else
                    {
                        pluginContainer.IsEnabled = false;
                    }
                }
                catch (Exception e)
                {
#if DEBUG
                    MessageBox.Show("Plugin failed to start up:\n\n" + e.ToString());
#endif
                    pluginContainer.Fail(e, "Plugin failed in StartUp");
                }
            }
        }


        private AggregateCatalog CreateCatalog()
        {
            mExternalAssemblies.Clear();
            LoadReferenceLists();

            var returnValue = new AggregateCatalog();

            var pluginDirectories = new List<string>();

            pluginDirectories.Add(PluginFolder);

            foreach (var directory in pluginDirectories)
            {
                List<string> dllFiles = FileManager.GetAllFilesInDirectory(directory, "dll");
                string executablePath = FileManager.GetDirectory(System.Windows.Forms.Application.ExecutablePath);

                foreach (string dll in dllFiles)
                {
                    Assembly loadedAssembly = Assembly.LoadFrom(dll);

                    returnValue.Catalogs.Add(new AssemblyCatalog(loadedAssembly));
                }
            }

            returnValue.Catalogs.Add(new AssemblyCatalog(System.Reflection.Assembly.GetExecutingAssembly()));

            return returnValue;
        }

        private void LoadPluginSettings()
        {

            if (System.IO.File.Exists(PluginSettingsSaveFileName))
            {
                mPluginSettingsSave = PluginSettingsSave.Load(PluginSettingsSaveFileName);
            }
            else
            {
                mPluginSettingsSave = new PluginSettingsSave();
            }
        }

        private void LoadReferenceLists()
        {
            // We use absolute paths for some of the .dlls and .exes
            // because if we don't, then Glue looks for them in the Startup
            // path, which could depend on whether Glue is launched from a shortcut
            // or not - this is really common for released versions.
            string executablePath = FileManager.GetDirectory(System.Windows.Forms.Application.ExecutablePath);

            //Load Internal List
            mReferenceListInternal.Add(executablePath + "Ionic.Zip.dll");
            //mReferenceListExternal.Add(executablePath + "CsvLibrary.dll");
            //mReferenceListExternal.Add(executablePath + "RenderingLibrary.dll");

            //mReferenceListExternal.Add(executablePath + "ToolsUtilities.dll");

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
        }

        private void LoadExternalReferenceList(string filePath)
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

        private Assembly currentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            foreach (Assembly item in mExternalAssemblies)
            {
                if (item.FullName == args.Name)
                {
                    return item;
                }
            }

            MessageBox.Show("Couldn't find assembly: " + args.Name + " for " + args.RequestingAssembly);

            return null;
        }

    }
}
