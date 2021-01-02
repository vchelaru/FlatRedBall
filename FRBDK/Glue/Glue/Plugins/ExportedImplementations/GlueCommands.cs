using System.Diagnostics;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using Glue;
using System;
using FlatRedBall.Glue.Errors;
using System.Threading.Tasks;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations
{
    public class GlueCommands : IGlueCommands
    {
        #region Fields

        static GlueCommands mSelf;
        public static GlueCommands Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new GlueCommands();
                }
                return mSelf;
            }
        }

        #endregion

        public IGenerateCodeCommands GenerateCodeCommands{ get; private set; }

        public IGluxCommands GluxCommands { get; private set; }

        public IOpenCommands OpenCommands { get; private set; }

        public IProjectCommands ProjectCommands { get; private set; }

        public IRefreshCommands RefreshCommands { get; private set; }

        public ITreeNodeCommands TreeNodeCommands { get; private set; }

        public IUpdateCommands UpdateCommands { get; private set; }

        public IDialogCommands DialogCommands { get; private set; }

        //public GlueViewCommands GlueViewCommands { get; private set; }

        public IFileCommands FileCommands { get; private set; }

        public ISelectCommands SelectCommands { get; private set; }

        public void PrintOutput(string output)
        {
            PluginManager.ReceiveOutput(output);
        }

        public void PrintError(string output)
        {
            PluginManager.ReceiveError(output);
        }
        
        public void DoOnUiThread(Action action)
        {
            MainGlueWindow.Self.Invoke(action);
        }

        public void CloseGlue()
        {
            MainGlueWindow.Self.Close();
            Process.GetCurrentProcess().Kill();
        }

        public async void LoadProject(string fileName)
        {
            await IO.ProjectLoader.Self.LoadProject(fileName);
        }

        public async Task LoadProjectAsync(string fileName)
        {
            await IO.ProjectLoader.Self.LoadProject(fileName);
        }

        /// <summary>
        /// Tries an action multiple time, sleeping and repeating if an exception is thrown.
        /// If the number of times is exceeded, the exception is rethrown and needs to be caught
        /// by the caller.
        /// </summary>
        /// <param name="action">The action to invoke</param>
        /// <param name="numberOfTimesToTry">The number of times to try</param>
        public void TryMultipleTimes(Action action, int numberOfTimesToTry = 5)
        {
            const int msSleep = 200;
            int failureCount = 0;

            while (failureCount < numberOfTimesToTry)
            {
                try
                {
                    action();
                    break;
                }


                catch (Exception e)
                {
                    failureCount++;
                    System.Threading.Thread.Sleep(msSleep);
                    if (failureCount >= numberOfTimesToTry)
                    {
                        throw e;
                    }
                }
            }
        }

        public GlueCommands()
        {
            mSelf = this;
            GenerateCodeCommands = new GenerateCodeCommands();
            GluxCommands = new GluxCommands();
            OpenCommands = new OpenCommands();
            ProjectCommands = new ProjectCommands();
            RefreshCommands = new RefreshCommands();
            TreeNodeCommands = new TreeNodeCommands();
            UpdateCommands = new UpdateCommands();
            DialogCommands = new DialogCommands();
            //GlueViewCommands = new GlueViewCommands();
            FileCommands = new FileCommands();
            SelectCommands = new SelectCommands();
        }

        public string GetAbsoluteFileName(SaveClasses.ReferencedFileSave rfs)
        {
            if(rfs == null)
            {
                throw new ArgumentNullException("rfs", "The argument ReferencedFileSave should not be null");
            }
            return ProjectManager.MakeAbsolute(rfs.Name, true);
        }

        public FilePath GetAbsoluteFilePath(SaveClasses.ReferencedFileSave rfs)
        {
            return GetAbsoluteFileName(rfs);
        }

        public string GetAbsoluteFileName(string relativeFileName, bool isContent)
        {
            return ProjectManager.MakeAbsolute(relativeFileName, isContent);
        }
    }
}
