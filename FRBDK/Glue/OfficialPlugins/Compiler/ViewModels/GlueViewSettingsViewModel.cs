using FlatRedBall.Glue.MVVM;
using OfficialPlugins.Compiler.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.Compiler.ViewModels
{
    public class GlueViewSettingsViewModel : ViewModel
    {
        public int PortNumber
        {
            get => Get<int>();
            set => Set(value);
        }

        public bool ShowScreenBoundsWhenViewingEntities
        {
            get => Get<bool>();
            set => Set(value);
        }

        public decimal GridSize
        {
            get => Get<decimal>();
            set => Set(value);
        }

        internal void SetFrom(CompilerSettingsModel model)
        {
            this.PortNumber = model.PortNumber;
            this.ShowScreenBoundsWhenViewingEntities = model.ShowScreenBoundsWhenViewingEntities;
            this.GridSize = model.GridSize;

        }

        internal void SetModel(CompilerSettingsModel compilerSettings)
        {
            compilerSettings.PortNumber = this.PortNumber;
            compilerSettings.ShowScreenBoundsWhenViewingEntities = this.ShowScreenBoundsWhenViewingEntities;
            compilerSettings.GridSize = this.GridSize;
        }
    }
}
