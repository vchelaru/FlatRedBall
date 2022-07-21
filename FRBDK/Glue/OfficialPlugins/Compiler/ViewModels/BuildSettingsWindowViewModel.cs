using FlatRedBall.Glue.MVVM;
using OfficialPlugins.Compiler.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.Compiler.ViewModels
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
