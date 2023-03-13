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
using FlatRedBall.Glue.VSHelpers.Projects;
using System.Runtime.InteropServices;
using GlueFormsCore.Controls;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations
{
    public class GlueCommands : IGlueCommands
    {
        #region Fields/Properties

        public static GlueCommands Self { get; private set; } = new GlueCommands();

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

        #endregion

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

        public Task DoOnUiThread(Func<Task> func) => MainGlueWindow.Self.Invoke(func);

        public T DoOnUiThread<T>(Func<T> func) => MainGlueWindow.Self.Invoke(func);

        public void CloseGlueProject(bool shouldSave = true, bool isExiting = false, GlueFormsCore.Controls.InitializationWindowWpf initWindow = null)
        {
            GlueState.Self.CurrentTreeNode = null;
            GlueFormsCore.Controls.MainPanelControl.Self.ReactToCloseProject(shouldSave, isExiting, initWindow);
        }

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
            return GetAbsoluteFilePath(rfs).FullPath;
        }

        public FilePath GetAbsoluteFilePath(SaveClasses.ReferencedFileSave rfs)
        {
            if(rfs.FilePath == null)
            {
                var relativePath = rfs.Name;
                // We can reduce some branching by not calling the shared code:
                if (ProjectManager.ContentProject != null)
                {
                    rfs.FilePath = !relativePath.StartsWith(ProjectManager.ContentDirectoryRelative)
                                ? ProjectManager.ContentProject.MakeAbsolute(ProjectManager.ContentDirectoryRelative + relativePath)
                                : ProjectManager.ContentProject.MakeAbsolute(relativePath);
                }
                else
                {
                    rfs.FilePath = ProjectManager.ProjectBase.MakeAbsolute(relativePath);
                }

            }

            return rfs.FilePath;
        }

        public FilePath GetAbsoluteFilePath(string relativeFilePath, bool forceAsContent=true)
        {
            return MakeAbsolute(relativeFilePath, forceAsContent);

        }

        public FilePath GetAbsoluteFilePath(GlueElement element)
        {
            var extension = element is ScreenSave
                ? GlueProjectSave.ScreenExtension
                : GlueProjectSave.EntityExtension;

            var glueDirectory = GlueState.Self.CurrentGlueProjectDirectory;

            return glueDirectory + element.Name + "." + extension;
        }

        public string GetAbsoluteFileName(string relativeFileName, bool isContent)
        {
            return MakeAbsolute(relativeFileName, isContent);
        }

        /// <summary>
        /// Converts a relative path to an absolute path assuming that the relative path
        /// is relative to the base Project's directory.  This determines whether to use
        /// the base project or the content project according to the extension of the file or whether forceAsContent is true.
        /// </summary>
        /// <param name="relativePath">The path to make absolute.</param>
        /// <param name="forceAsContent">Whether to force as content - can be passed as true if the file should be treated as content despite its extension.</param>
        /// <returns>The absolute file name.</returns>
        string MakeAbsolute(string relativePath, bool forceAsContent)
        {
            if (FileManager.IsRelative(relativePath))
            {
                if ((forceAsContent || GlueCommands.Self.FileCommands.IsContent(relativePath)) && ProjectManager.ContentProject != null)
                {
                    return !relativePath.StartsWith(ProjectManager.ContentDirectoryRelative)
                               ? ProjectManager.ContentProject.MakeAbsolute(ProjectManager.ContentDirectoryRelative + relativePath)
                               : ProjectManager.ContentProject.MakeAbsolute(relativePath);
                }
                else
                {
                    return ProjectManager.ProjectBase.MakeAbsolute(relativePath);
                }
            }

            return relativePath;
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
            var save = GlueState.Self.GlueSettingsSave;

            string lastFileName = null;

            if (ProjectManager.ProjectBase != null)
            {
                lastFileName = ProjectManager.ProjectBase.FullFileName.FullPath;
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

            List<List<string>> AllTabs = new List<List<string>>
            {
                save.TopTabs,
                save.BottomTabs,
                save.LeftTabs,
                save.RightTabs,
                save.CenterTabs
            };

            void SetTabs(List<string> tabNames, TabContainerViewModel tabs)
            {
                // If we clear, that means we only end up saving whatever is currently visible. Invisible tabs would get removed.
                // Instead, we want to amke sure that any tab added should get removed from other tabs
                //tabNames.Clear();
                var tabTitles = tabs.Tabs.Select(item => item.Title).ToArray();
                foreach (var title in tabTitles)
                {
                    RemoveFromAllBut(title, tabNames);
                    if(!tabNames.Contains(title))
                    {
                        tabNames.Add(title);
                    }
                }
            }

            void RemoveFromAllBut(string title, List<string> except)
            {
                foreach(var stringList in AllTabs)
                {
                    if(stringList != except && stringList.Contains(title))
                    {
                        stringList.Remove(title);
                    }
                }
            }



            SetTabs(save.TopTabs, PluginManager.TabControlViewModel.TopTabItems);
            SetTabs(save.LeftTabs, PluginManager.TabControlViewModel.LeftTabItems);
            SetTabs(save.CenterTabs, PluginManager.TabControlViewModel.CenterTabItems);
            SetTabs(save.RightTabs, PluginManager.TabControlViewModel.RightTabItems);
            SetTabs(save.BottomTabs, PluginManager.TabControlViewModel.BottomTabItems);

            var panel = MainPanelControl.Self;

            if(MainPanelControl.ViewModel.LeftPanelWidth.GridUnitType == System.Windows.GridUnitType.Pixel)
            {
                save.LeftTabWidthPixels = MainPanelControl.ViewModel.LeftPanelWidth.Value;
            }
            else
            {
                save.LeftTabWidthPixels = null;
            }
            // do we care about the other panels?


            if (saveToDisk)
            {
                GlueCommands.Self.GluxCommands.SaveSettings();
            }
        }

        [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string x, string y);
        public int CompareFileSort(string first, string second) => StrCmpLogicalW(first, second);

        public void Undo()
        {
            PluginManager.CallPluginMethod(pluginFriendlyName: "Undo Plugin", methodName: "Undo");
        }
    }
}
