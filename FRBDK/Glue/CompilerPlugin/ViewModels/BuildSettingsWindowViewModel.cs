using FlatRedBall.Glue.MVVM;
using CompilerLibrary.ViewModels;
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

        public bool UseMsBuildServer
        {
            get => Get<bool>();
            set => Set(value);
        }

        // Backed by CompilerViewModel.Self rather than BuildSettingsUser - this setting is session-only
        // (not persisted to BuildSettings.user.json), same as before it moved into this dialog from the
        // Build tab's toolbar.
        public bool IsPrintMsBuildCommandChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public void SetFrom(BuildSettingsUser buildSettingsUser)
        {
            CustomMsBuildLocation = buildSettingsUser.CustomMsBuildLocation;
            UseMsBuildServer = buildSettingsUser.UseMsBuildServer;
            IsPrintMsBuildCommandChecked = CompilerViewModel.Self.IsPrintMsBuildCommandChecked;
        }

        public void ApplyTo(BuildSettingsUser buildSettingsUser)
        {
            buildSettingsUser.CustomMsBuildLocation = CustomMsBuildLocation;
            buildSettingsUser.UseMsBuildServer = UseMsBuildServer;
            CompilerViewModel.Self.IsPrintMsBuildCommandChecked = IsPrintMsBuildCommandChecked;
        }
    }
}
