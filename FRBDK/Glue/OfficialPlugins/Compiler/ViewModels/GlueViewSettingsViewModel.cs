using FlatRedBall.Glue.MVVM;
using OfficialPlugins.Compiler.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.Compiler.ViewModels
{
    public class GlueViewSettingsViewModel : ViewModel
    {
        public bool EnableGlueViewEdit
        {
            get => Get<bool>();
            set => Set(value);
        }

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
            set
            {
                const decimal minValue = 4;
                value = Math.Max(value, minValue);
                Set(value);
            }
        }


        internal void SetFrom(CompilerSettingsModel model)
        {
            this.PortNumber = model.PortNumber;
            this.ShowScreenBoundsWhenViewingEntities = model.ShowScreenBoundsWhenViewingEntities;
            this.GridSize = model.GridSize;
            this.EnableGlueViewEdit = model.GenerateGlueControlManagerCode;

        }

        internal void SetModel(CompilerSettingsModel compilerSettings)
        {
            compilerSettings.PortNumber = this.PortNumber;
            compilerSettings.ShowScreenBoundsWhenViewingEntities = this.ShowScreenBoundsWhenViewingEntities;
            compilerSettings.GridSize = this.GridSize;
            compilerSettings.GenerateGlueControlManagerCode = this.EnableGlueViewEdit;

        }
    }
}
