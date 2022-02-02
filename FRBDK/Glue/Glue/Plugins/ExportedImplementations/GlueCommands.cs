using System.Diagnostics;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using Glue;
using System;
using FlatRedBall.Glue.Errors;
using System.Threading.Tasks;
using FlatRedBall.IO;
using FlatRedBall.Glue.IO;
using System.Linq;
using FlatRedBall.Glue.SaveClasses;
using System.Collections.Generic;
using GlueFormsCore.ViewModels;
using FlatRedBall.Glue.Managers;

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

        public T DoOnUiThread<T>(Func<T> func) => MainGlueWindow.Self.Invoke(func);

        public void CloseGlue()
        {
            MainGlueWindow.Self.Close();
            Process.GetCurrentProcess().Kill();
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
        /// <param name="msSleepBetweenAttempts">The number of milliseconds to sleep between each failed attempt.</param>
        public void TryMultipleTimes(Action action, int numberOfTimesToTry = 5, int msSleepBetweenAttempts = 200)
        {
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
                    System.Threading.Thread.Sleep(msSleepBetweenAttempts);
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

        public FilePath GetAbsoluteFilePath(string rfsName)
        {
            return ProjectManager.MakeAbsolute(rfsName, forceAsContent:true);

        }

        public string GetAbsoluteFileName(string relativeFileName, bool isContent)
        {
            return ProjectManager.MakeAbsolute(relativeFileName, isContent);
        }

        public Task UpdateGlueSettingsFromCurrentGlueStateAsync(bool saveToDisk = true)
        {
            return TaskManager.Self.AddAsync(() =>
            {
                UpdateGlueSettingsFromCurrentGlueStateImmediately(saveToDisk);

            }, "Saving Glue Settings", doOnUiThread:true);
        }

        public void UpdateGlueSettingsFromCurrentGlueStateImmediately(bool saveToDisk = true)
        {
            var save = ProjectManager.GlueSettingsSave;

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

            if (!alreadyIsListed)
            {
                foundItem = new ProjectFileGlueFilePair();
                save.GlueLocationSpecificLastProjectFiles.Add(foundItem);
            }
            foundItem.GlueFileName = glueExeFileName;
            foundItem.GameProjectFileName = lastFileName;

            // set up the positions of the window
            //save.WindowLeft = this.Left;
            //save.WindowTop = this.Top;
            //save.WindowHeight = this.Height;
            //save.WindowWidth = this.Width;
            save.StoredRecentFiles = MainGlueWindow.Self.NumberOfStoredRecentFiles;

            void SetTabs(List<string> tabNames, TabContainerViewModel tabs)
            {
                tabNames.Clear();
                tabNames.AddRange(tabs.Tabs.Select(item => item.Title));
            }

            SetTabs(save.TopTabs, PluginManager.TabControlViewModel.TopTabItems);
            SetTabs(save.LeftTabs, PluginManager.TabControlViewModel.LeftTabItems);
            SetTabs(save.CenterTabs, PluginManager.TabControlViewModel.CenterTabItems);
            SetTabs(save.RightTabs, PluginManager.TabControlViewModel.RightTabItems);
            SetTabs(save.BottomTabs, PluginManager.TabControlViewModel.BottomTabItems);

            if (saveToDisk)
            {
                GlueCommands.Self.GluxCommands.SaveSettings();
            }
        }
    }
}
