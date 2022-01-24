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
        public bool EnableGameEditMode
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool EmbedGameInGameTab
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

        [DependsOn(nameof(EnableGameEditMode))]
        public Visibility ShowWindowDefenderUi => EnableGameEditMode.ToVisibility();

        internal void SetFrom(CompilerSettingsModel model)
        {
            this.PortNumber = model.PortNumber;
            this.ShowScreenBoundsWhenViewingEntities = model.ShowScreenBoundsWhenViewingEntities;
            this.GridSize = model.GridSize;
            this.EnableGameEditMode = model.GenerateGlueControlManagerCode;
            this.EmbedGameInGameTab = model.EmbedGameInGameTab;

            }

        internal void SetModel(CompilerSettingsModel compilerSettings)
        {
            compilerSettings.PortNumber = this.PortNumber;
            compilerSettings.ShowScreenBoundsWhenViewingEntities = this.ShowScreenBoundsWhenViewingEntities;
            compilerSettings.GridSize = this.GridSize;
            compilerSettings.GenerateGlueControlManagerCode = this.EnableGameEditMode;
            compilerSettings.EmbedGameInGameTab = this.EmbedGameInGameTab;

        }
    }
}
