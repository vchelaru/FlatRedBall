using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.Compiler;
using OfficialPluginsCore.Compiler.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

                        if(toolbar.IsOpen)
                        {
                            toolbar.IsOpen = false;
                        }
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
        
        void RefreshToolbarScreens()
        {
            toolbarViewModel.AvailableScreens.Clear();

            if(GlueState.Self.CurrentGlueProject != null)
            {
                var sortedScreens = GlueState.Self.CurrentGlueProject.Screens
                    .Select(item => ScreenName(item))
                    .OrderBy(item => item)
                    .ToArray();

                foreach(var item in sortedScreens)
                {
                    toolbarViewModel.AvailableScreens.Add(item);
                }

                var startupScreen =
                    GlueState.Self.CurrentGlueProject.StartUpScreen;

                if(!string.IsNullOrEmpty(startupScreen) && startupScreen.Length > "Screens\\".Length)
                {

                    toolbarViewModel.StartupScreenName = 
                        startupScreen.Substring("Screens\\".Length);
                }
            }
        }

        string ScreenName(ScreenSave screen) => screen.Name.Substring("Screens\\".Length);

        internal void ReactToChangedStartupScreen()
        {
            if(toolbarViewModel != null)
            {
                toolbarViewModel.StartupScreenName =
                    GlueState.Self.CurrentGlueProject.StartUpScreen?.Substring("Screens\\".Length);
            }
        }

        internal void SetEnabled(bool isToolbarPlayButtonEnabled)
        {
            toolbarViewModel.IsPlayVisible = isToolbarPlayButtonEnabled;
        }
    }
}
