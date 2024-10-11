using FlatRedBall.Glue.MVVM;
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
    }
}
