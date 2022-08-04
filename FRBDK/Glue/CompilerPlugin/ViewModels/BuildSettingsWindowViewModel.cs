using FlatRedBall.Glue.MVVM;
using CompilerPlugin.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerPlugin.ViewModels
{
    internal class BuildSettingsWindowViewModel : ViewModel
    {
        public string CustomMsBuildLocation
        {
            get => Get<string>();
            set => Set(value);
        }

        public void SetFrom(BuildSettingsUser buildSettingsUser)
        {
            CustomMsBuildLocation = buildSettingsUser.CustomMsBuildLocation;
        }

        public void ApplyTo(BuildSettingsUser buildSettingsUser)
        {
            buildSettingsUser.CustomMsBuildLocation = CustomMsBuildLocation;
        }
    }
}
