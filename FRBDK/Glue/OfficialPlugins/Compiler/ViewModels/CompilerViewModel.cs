using FlatRedBall.Glue.MVVM;
using OfficialPlugins.Compiler.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OfficialPlugins.Compiler.ViewModels
{
    public class CompilerViewModel : ViewModel
    {

        public bool AutoBuildContent { get; set; }

        public bool IsRunning
        {
            get => Get<bool>();
            set
            {
                if (Set(value))
                {
                    // If the game either stops or restarts, no longer paused
                    IsPaused = false;
                    CurrentGameSpeed = "100%";
                }
            }
        }

        [DependsOn(nameof(IsRunning))]
        public Visibility WhileRunningViewVisibility => IsRunning ? 
            Visibility.Visible : Visibility.Collapsed;

        [DependsOn(nameof(IsRunning))]
        public Visibility WhileStoppedViewVisibility => IsRunning ?
            Visibility.Collapsed : Visibility.Visible;

        public bool IsPaused
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsPaused))]
        public Visibility PauseButtonVisibility => IsPaused ?
            Visibility.Collapsed : Visibility.Visible;

        [DependsOn(nameof(IsPaused))]
        public Visibility UnpauseButtonVisibility => IsPaused ?
            Visibility.Visible : Visibility.Collapsed;

        public bool IsGenerateGlueControlManagerInGame1Checked
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsGenerateGlueControlManagerInGame1Checked))]
        public Visibility PortUiVisibility => IsGenerateGlueControlManagerInGame1Checked ?
            Visibility.Visible : Visibility.Collapsed;

        public int PortNumber
        {
            get => Get<int>();
            set => Set(value);
        }

        public string Configuration { get; set; }

        Visibility compileContentButtonVisibility;
        public Visibility CompileContentButtonVisibility
        {
            get { return compileContentButtonVisibility; }
            set { base.ChangeAndNotify(ref compileContentButtonVisibility, value); }
        }

        public List<string> GameSpeedList { get; set; } =
            new List<string>
            {
                "500%",
                "200%",
                "100%",
                "50%",
                "25%",
                "10%"
            };

        public string CurrentGameSpeed
        {
            get => Get<string>();
            set => Set(value);
        }

        public CompilerViewModel()
        {
            CurrentGameSpeed = "100%";
        }

        internal void SetFrom(CompilerSettingsModel model)
        {
            this.IsGenerateGlueControlManagerInGame1Checked = model.GenerateGlueControlManagerCode;
            this.PortNumber = model.PortNumber;
        }

        public CompilerSettingsModel ToModel()
        {
            CompilerSettingsModel toReturn = new CompilerSettingsModel();
            toReturn.GenerateGlueControlManagerCode = this.IsGenerateGlueControlManagerInGame1Checked;
            toReturn.PortNumber = this.PortNumber;
            return toReturn;
        }
    }
}
