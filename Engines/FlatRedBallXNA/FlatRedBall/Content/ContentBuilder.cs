#region Using Statements
using System;

#if !XBOX360 && !WINDOWS_PHONE
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Build.BuildEngine;
#endif

using FlatRedBall;

using FlatRedBall.IO;
#endregion

namespace FlatRedBall.Content
{
#if !XBOX && !WINDOWS_PHONE
    /// <summary>
    /// This class wraps the MSBuild functionality needed to build XNA Framework
    /// content dynamically at runtime. It creates a temporary MSBuild project
    /// in memory, and adds whatever content files you choose to this project.
    /// It then builds the project, which will create compiled .xnb content files
    /// in a temporary directory. After the build finishes, you can use a regular
    /// ContentManager to load these temporary .xnb files in the usual way.
    /// </summary>
    public class ContentBuilder : IDisposable
    {
        #region Fields


        // What importers or processors should we load?
        const string xnaVersion = ", Version=3.0.0.0, PublicKeyToken=51c3bfb2db46012c";

        static string[] pipelineAssemblies =
        {
            "Microsoft.Xna.Framework.Content.Pipeline.FBXImporter" + xnaVersion,
            "Microsoft.Xna.Framework.Content.Pipeline.TextureImporter" + xnaVersion,
            "Microsoft.Xna.Framework.Content.Pipeline.EffectImporter" + xnaVersion,
            "Microsoft.Xna.Framework.Content.Pipeline.XImporter" + xnaVersion


        };

        // MSBuild objects used to dynamically build content.
        Engine msBuildEngine;
        Project msBuildProject;
        ErrorLogger errorLogger;


        // Temporary directories used by the content build.
        string mBuildDirectory;
        string mProcessDirectory;
        string mBaseDirectory;


        // Generate unique directory names if there is more than one ContentBuilder.
        static int directorySalt;


        // Have we been disposed?
        bool isDisposed;

        bool mHaveFilesBeenCreated = false;

        #endregion

        #region Properties

        public string OutputDirectory
        {
            get{ return mBuildDirectory +  @"\bin\Content\"; }
        }

        #endregion

        #region Methods

        #region Initialization


        /// <summary>
        /// Creates a new content builder.
        /// </summary>
        public ContentBuilder()
        {
            CreateBuildProject();
        }


        /// <summary>
        /// Finalizes the content builder.
        /// </summary>
        ~ContentBuilder()
        {
            Dispose(false);
        }


        /// <summary>
        /// Disposes the content builder when it is no longer required.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Implements the standard .NET IDisposable pattern.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;

                DeleteTempDirectory();
            }
        }


        #endregion

        #region MSBuild


        /// <summary>
        /// Creates a temporary MSBuild content project in memory.
        /// </summary>
        void CreateBuildProject()
        {

            // Create the build engine.

            msBuildEngine = new Engine(RuntimeEnvironment.GetRuntimeDirectory());
            msBuildEngine.DefaultToolsVersion = "3.5";

            
            // Hook up our custom error logger.
            errorLogger = new ErrorLogger();

            msBuildEngine.RegisterLogger(errorLogger);

            // Create the build project.
            msBuildProject = new Project(msBuildEngine);


            msBuildProject.SetProperty("XnaPlatform", "Windows");
            msBuildProject.SetProperty("XnaFrameworkVersion", "v3.0");
            msBuildProject.SetProperty("Configuration", "Release");


            // Register any custom importers or processors.
            foreach (string pipelineAssembly in pipelineAssemblies)
            {
                msBuildProject.AddNewItem("Reference", pipelineAssembly);
            }


            // Include the standard targets file that defines
            // how to build XNA Framework content.
            msBuildProject.AddNewImport("$(MSBuildExtensionsPath)\\Microsoft\\XNA " +
                                        "Game Studio\\v3.0\\Microsoft.Xna.GameStudio.ContentPipeline.targets", null);
            
        }


        /// <summary>
        /// Adds a new content file to the MSBuild project. The importer and
        /// processor are optional: if you leave the importer null, it will
        /// be autodetected based on the file extension, and if you leave the
        /// processor null, data will be passed through without any processing.
        /// </summary>
        public void Add(string filename, string name, string importer, string processor)
        {
            BuildItem buildItem = msBuildProject.AddNewItem("Compile", filename);

            buildItem.SetMetadata("Link", Path.GetFileName(filename));
            buildItem.SetMetadata("Name", name);

            if (!string.IsNullOrEmpty(importer))
                buildItem.SetMetadata("Importer", importer);

            if (!string.IsNullOrEmpty(processor))
                buildItem.SetMetadata("Processor", processor);
        }


        public bool IsReferencingAssembly(string assemblyName)
        {
            BuildItemGroup buildItemGroup = msBuildProject.GetEvaluatedItemsByName("Reference");
            
            foreach(BuildItem buildItem in buildItemGroup)
            {
                if(buildItem.Include == assemblyName)
                    return true;
            }
            
            return false;
        }


        /// <summary>
        /// Removes all content files from the MSBuild project.
        /// </summary>
        public void Clear()
        {
            msBuildProject.RemoveItemsByName("Compile");
        }


        /// <summary>
        /// Builds all the content files which have been added to the project,
        /// dynamically creating .xnb files in the OutputDirectory.
        /// Returns an error message if the build fails.
        /// </summary>
        public string Build()
        {
            if (mHaveFilesBeenCreated == false)
            {
                CreateTempDirectory();
            }

            // Clear any previous errors.
            errorLogger.Errors.Clear();

            // Build the project.
            if (!msBuildProject.Build())
            {
                //return "Error";
                // If the build failed, return an error string.
                return string.Join("\n", errorLogger.Errors.ToArray());
            }

            return null;
        }

        public void Save()
        {
            msBuildProject.Save("test.csproj");
        }


        #endregion

        #region Temp Directories


        /// <summary>
        /// Creates a temporary directory in which to build content.
        /// </summary>
        void CreateTempDirectory()
        {
            // Start with a standard base name:
            //
            //  %temp%\WinFormsContentLoading.ContentBuilder

            mBaseDirectory = FileManager.RelativeDirectory;;

            // Include our process ID, in case there is more than
            // one copy of the program running at the same time:
            //
            //  %temp%\WinFormsContentLoading.ContentBuilder\<ProcessId>

            int processId = Process.GetCurrentProcess().Id;

            mProcessDirectory = Path.Combine(FileManager.RelativeDirectory, processId.ToString());

            // Include a salt value, in case the program
            // creates more than one ContentBuilder instance:
            //
            //  %temp%\WinFormsContentLoading.ContentBuilder\<ProcessId>\<Salt>

            directorySalt++;

            mBuildDirectory = Path.Combine(mProcessDirectory, directorySalt.ToString());

            // Create our temporary directory.
            Directory.CreateDirectory(mBuildDirectory);

            PurgeStaleTempDirectories();

            string projectPath = Path.Combine(mBuildDirectory, "content.contentproj");
            msBuildProject.FullFileName = projectPath;

            string outputPath = Path.Combine(mBuildDirectory, "bin");
            msBuildProject.SetProperty("OutputPath", outputPath);
        }


        /// <summary>
        /// Deletes our temporary directory when we are finished with it.
        /// </summary>
        void DeleteTempDirectory()
        {
            if (mHaveFilesBeenCreated)
            {
                Directory.Delete(mBuildDirectory, true);

                // If there are no other instances of ContentBuilder still using their
                // own temp directories, we can delete the process directory as well.
                if (Directory.GetDirectories(mProcessDirectory).Length == 0)
                {
                    Directory.Delete(mProcessDirectory);

                    // If there are no other copies of the program still using their
                    // own temp directories, we can delete the base directory as well.
                    if (Directory.GetDirectories(mBaseDirectory).Length == 0)
                    {
                        Directory.Delete(mBaseDirectory);
                    }
                }
            }
        }


        /// <summary>
        /// Ideally, we want to delete our temp directory when we are finished using
        /// it. The DeleteTempDirectory method (called by whichever happens first out
        /// of Dispose or our finalizer) does exactly that. Trouble is, sometimes
        /// these cleanup methods may never execute. For instance if the program
        /// crashes, or is halted using the debugger, we never get a chance to do
        /// our deleting. The next time we start up, this method checks for any temp
        /// directories that were left over by previous runs which failed to shut
        /// down cleanly. This makes sure these orphaned directories will not just
        /// be left lying around forever.
        /// </summary>
        void PurgeStaleTempDirectories()
        {
            // Check all subdirectories of our base location.
            foreach (string directory in Directory.GetDirectories(mBaseDirectory))
            {
                // The subdirectory name is the ID of the process which created it.
                int processId;

                if (int.TryParse(Path.GetFileName(directory), out processId))
                {
                    try
                    {
                        // Is the creator process still running?
                        Process.GetProcessById(processId);
                    }
                    catch (ArgumentException)
                    {
                        // If the process is gone, we can delete its temp directory.
                        Directory.Delete(directory, true);
                    }
                }
            }
        }

        
        #endregion

        public void ReferenceAssembly(string assemblyName)
        {
            msBuildProject.AddNewItem("Reference", assemblyName);
        }

        public void RemoveReferencedAssembly(string assemblyName)
        {
            if (IsReferencingAssembly(assemblyName))
            {
                msBuildProject.RemoveItemsByName(assemblyName);

            }
        }

        #endregion
    }
#else
    /// <summary>
    /// This is just a stub in order to avoid build issues ... none of this functionality is 
    /// available for the 360.
    /// </summary>
    public class ContentBuilder : IDisposable
    {
        public void Dispose()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public string OutputDirectory
        {
            get { return null; }
            set {  }
        }
        public void ReferenceAssembly(string s)
        { }

        public void Clear()
        { }

        public void Add(string a, string b, string c, string d)
        { }

        public string Build()
        {
            return null;
        }
    }
#endif
}
