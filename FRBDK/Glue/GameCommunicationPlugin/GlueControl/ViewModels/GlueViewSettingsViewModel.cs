using CompilerLibrary.Models;
using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace GameCommunicationPlugin.GlueControl.ViewModels
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

        public bool SetBackgroundColor
        {
            get => Get<bool>();
            set => Set(value);
        }

        public int BackgroundRed
        {
            get => Get<int>();
            set => Set(value);
        }

        public int BackgroundGreen
        {
            get => Get<int>();
            set => Set(value);
        }

        public int BackgroundBlue
        {
            get => Get<int>();
            set => Set(value);
        }

        public bool ShowGrid
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

        public bool RestartScreenOnLevelContentChange
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool EnableSnapping
        {
            get => Get<bool>();
            set => Set(value);
        }


        public decimal SnapSize
        {
            get => Get<decimal>();
            set => Set(value);
        }

        public decimal PolygonPointSnapSize
        {
            get => Get<decimal>();
            set => Set(value);
        }

        [DependsOn(nameof(EnableGameEditMode))]
        public Visibility ShowWindowDefenderUi => EnableGameEditMode.ToVisibility();



        internal void SetFrom(CompilerSettingsModel model)
        {
            this.PortNumber = model.PortNumber;
            this.ShowScreenBoundsWhenViewingEntities = model.ShowScreenBoundsWhenViewingEntities;

            this.ShowGrid = model.ShowGrid;
            this.GridSize = model.GridSize;
            this.SetBackgroundColor = model.SetBackgroundColor;
            this.BackgroundRed = model.BackgroundRed;
            this.BackgroundGreen = model.BackgroundGreen;
            this.BackgroundBlue = model.BackgroundBlue;
            this.EnableGameEditMode = model.GenerateGlueControlManagerCode;
            this.EmbedGameInGameTab = model.EmbedGameInGameTab;
            this.RestartScreenOnLevelContentChange = model.RestartScreenOnLevelContentChange;

            this.EnableSnapping = model.EnableSnapping;
            this.SnapSize = model.SnapSize;
            this.PolygonPointSnapSize = model.PolygonPointSnapSize;
        }

        internal void SetModel(CompilerSettingsModel compilerSettings)
        {
            compilerSettings.PortNumber = this.PortNumber;
            compilerSettings.ShowScreenBoundsWhenViewingEntities = this.ShowScreenBoundsWhenViewingEntities;

            compilerSettings.ShowGrid = this.ShowGrid;
            compilerSettings.GridSize = this.GridSize;

            compilerSettings.SetBackgroundColor = this.SetBackgroundColor;
            compilerSettings.BackgroundRed = this.BackgroundRed;
            compilerSettings.BackgroundGreen = this.BackgroundGreen;
            compilerSettings.BackgroundBlue = this.BackgroundBlue;


            compilerSettings.GenerateGlueControlManagerCode = this.EnableGameEditMode;
            compilerSettings.EmbedGameInGameTab = this.EmbedGameInGameTab;
            compilerSettings.RestartScreenOnLevelContentChange = this.RestartScreenOnLevelContentChange;

            compilerSettings.EnableSnapping = this.EnableSnapping ;
            compilerSettings.SnapSize = this.SnapSize;
            compilerSettings.PolygonPointSnapSize = this.PolygonPointSnapSize;

            compilerSettings.ToolbarObjects.Clear();


        }
    }
}
