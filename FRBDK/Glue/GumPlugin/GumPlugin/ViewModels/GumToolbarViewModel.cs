using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GumPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace GumPlugin.ViewModels
{
    public class GumToolbarViewModel : ViewModel
    {
        public bool HasGumProject
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(HasGumProject))]
        public Visibility AddGumProjectVisibility => (!HasGumProject).ToVisibility();

        [DependsOn(nameof(HasGumProject))]
        public Visibility OpenGumVisibility => HasGumProject.ToVisibility();

        NewGumProjectCreationLogic _newGumProjectCreationLogic;
        public GumToolbarViewModel(NewGumProjectCreationLogic newGumProjectCreationLogic)
        {
            _newGumProjectCreationLogic = newGumProjectCreationLogic;
        }

        public async void OnToolbarClicked()
        {
            var alreadyHasGumProject = AppState.Self.GumProjectSave != null;

            if (alreadyHasGumProject == false)
            {
                await _newGumProjectCreationLogic.AskToCreateGumProject();
            }
            else
            {
                GlueCommands.Self.FileCommands.OpenFileInDefaultProgram(AppState.Self.GumProjectSave.FullFileName);
            }

            HasGumProject = AppState.Self.GumProjectSave != null;
        }
    }
}
