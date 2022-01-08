using FlatRedBall.Glue.MVVM;
using OfficialPlugins.Compiler.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace OfficialPlugins.Compiler.ViewModels
{
    public class GlueViewSettingsViewModel : ViewModel
    {
        public bool EnableGameEmbedAndEdit
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

        [DependsOn(nameof(EnableGameEmbedAndEdit))]
        public Visibility ShowWindowDefenderUi => EnableGameEmbedAndEdit.ToVisibility();

        internal void SetFrom(CompilerSettingsModel model)
        {
            this.PortNumber = model.PortNumber;
            this.ShowScreenBoundsWhenViewingEntities = model.ShowScreenBoundsWhenViewingEntities;
            this.GridSize = model.GridSize;
            this.EnableGameEmbedAndEdit = model.GenerateGlueControlManagerCode;

        }

        internal void SetModel(CompilerSettingsModel compilerSettings)
        {
            compilerSettings.PortNumber = this.PortNumber;
            compilerSettings.ShowScreenBoundsWhenViewingEntities = this.ShowScreenBoundsWhenViewingEntities;
            compilerSettings.GridSize = this.GridSize;
            compilerSettings.GenerateGlueControlManagerCode = this.EnableGameEmbedAndEdit;

        }
    }
}
