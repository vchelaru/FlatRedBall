using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl;
using OfficialPluginsCore.Compiler.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace OfficialPluginsCore.Compiler.Managers
{
    class ToolbarController : Singleton<ToolbarController>
    {
        #region Fields/Properties

        RunnerToolbarViewModel toolbarViewModel;
        RunnerToolbar toolbar;
        #endregion

        public void Initialize(RunnerToolbar toolbar)
        {
            this.toolbar = toolbar;
        }

        public RunnerToolbarViewModel GetViewModel()
        {
            if(toolbarViewModel == null)
            {
                toolbarViewModel = new RunnerToolbarViewModel();
                toolbarViewModel.PropertyChanged += HandleViewModelPropertyChanged;
            }
            return toolbarViewModel;
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(toolbarViewModel.StartupScreenName):
                    if(!string.IsNullOrWhiteSpace(toolbarViewModel.StartupScreenName))
                    {
                        GlueCommands.Self.GluxCommands.StartUpScreenName = "Screens\\" + toolbarViewModel.StartupScreenName;

                        GlueCommands.Self.DoOnUiThread(() =>
                        {
                            if(toolbar.IsOpen)
                            {
                                toolbar.IsOpen = false;
                            }
                        });
                    }



                    break;
            }
        }

        internal void HandleNewScreenCreated(ScreenSave screen)
        {
            RefreshToolbarScreens();
        }

        internal void HandleScreenRemoved(ScreenSave screen, List<string> arg2)
        {
            RefreshToolbarScreens();
        }

        internal void HandleGluxLoaded()
        {
            RefreshToolbarScreens();
        }


        internal void HandleGluxUnloaded()
        {
            RefreshToolbarScreens();
            toolbarViewModel.StartupScreenName = null;
        }


        #region External DllImport
        [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string x, string y);

        #endregion

        void RefreshToolbarScreens()
        {
            toolbarViewModel.AllScreens.Clear();

            if (GlueState.Self.CurrentGlueProject != null)
            {
                var sortedScreens = GlueState.Self.CurrentGlueProject.Screens
                    .Select(item =>
                    {
                        return new ScreenReferenceViewModel()
                        {
                            ScreenName = ScreenName(item)
                        };
                    })
                    .ToList();

                sortedScreens.Sort((first, second) => StrCmpLogicalW(first.ScreenName, second.ScreenName));

                toolbarViewModel.AllScreens.AddRange(sortedScreens);
                toolbarViewModel.RefreshAvailableScreens();

                var startupScreen =
                    GlueState.Self.CurrentGlueProject.StartUpScreen;

                if(!string.IsNullOrEmpty(startupScreen) && startupScreen.Length > "Screens\\".Length)
                {

                    toolbarViewModel.StartupScreenName = 
                        startupScreen.Substring("Screens\\".Length);
                }
            }
            else
            {
                toolbarViewModel.RefreshAvailableScreens();
            }
        }

        string ScreenName(ScreenSave screen) => screen.Name.Substring("Screens\\".Length);

        internal void ReactToChangedStartupScreen()
        {
            if(toolbarViewModel != null)
            {
                var startupScreen =
                    GlueState.Self.CurrentGlueProject.StartUpScreen;

                if(string.IsNullOrEmpty(startupScreen))
                {
                    toolbarViewModel.StartupScreenName = null;
                }
                else
                {
                    var substring =
                        GlueState.Self.CurrentGlueProject.StartUpScreen?.Substring("Screens\\".Length);
                    toolbarViewModel.StartupScreenName = substring;
                }
            }
        }

        internal void SetEnabled(bool isToolbarPlayButtonEnabled)
        {
            toolbarViewModel.IsPlayVisible = isToolbarPlayButtonEnabled;
        }
    }
}
