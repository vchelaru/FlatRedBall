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
    #region Enums
    public enum PlayOrEdit
    {
        Play,
        Edit
    }
    #endregion

    public class CompilerViewModel : ViewModel
    {
        #region Fields/Properties

        public bool HasLoadedGlux
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(HasLoadedGlux))]
        public Visibility EntireUiVisibility => HasLoadedGlux.ToVisibility();

        public bool AutoBuildContent { get; set; }

        public bool IsGluxVersionNewEnoughForGlueControlGeneration
        {
            get => Get<bool>();
            set => Set(value);
        }

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
                    PlayOrEdit = PlayOrEdit.Play;
                }
            }
        }

        public bool IsCompiling
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsHotReloadAvailable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsWaitingForGameToStart
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsRunning))]
        [DependsOn(nameof(IsCompiling))]
        [DependsOn(nameof(IsWaitingForGameToStart))]
        [DependsOn(nameof(HasLoadedGlux))]
        public bool IsToolbarPlayButtonEnabled => !IsRunning && !IsCompiling && !IsWaitingForGameToStart &&
            HasLoadedGlux;

        [DependsOn(nameof(IsHotReloadAvailable))]
        public Visibility ReloadVisibility => IsHotReloadAvailable.ToVisibility();

        [DependsOn(nameof(IsRunning))]
        public Visibility WhileRunningViewVisibility => IsRunning.ToVisibility();

        [DependsOn(nameof(IsRunning))]
        [DependsOn(nameof(IsCompiling))]
        [DependsOn(nameof(IsWaitingForGameToStart))]
        [DependsOn(nameof(HasLoadedGlux))]
        public Visibility WhileStoppedViewVisibility
        {
            get
            {
                if (IsRunning || IsCompiling || IsWaitingForGameToStart || HasLoadedGlux == false)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }
        public bool IsPaused
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsGluxVersionNewEnoughForGlueControlGeneration))]
        public Visibility GlueControlDependentVisibility => IsGluxVersionNewEnoughForGlueControlGeneration.ToVisibility();

        [DependsOn(nameof(IsPaused))]
        [DependsOn(nameof(IsGluxVersionNewEnoughForGlueControlGeneration))]
        public Visibility PauseButtonVisibility => (!IsPaused && IsGluxVersionNewEnoughForGlueControlGeneration).ToVisibility();

        public bool DidRunnerStartProcess
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool EffectiveIsRebuildAndRestartEnabled => true;
            //&&            DidRunnerStartProcess;

        [DependsOn(nameof(DidRunnerStartProcess))]
        [DependsOn(nameof(IsGluxVersionNewEnoughForGlueControlGeneration))]
        public Visibility RebuildRestartCheckBoxVisiblity => 
            // We need this for debugging
            //DidRunnerStartProcess && 
            IsGluxVersionNewEnoughForGlueControlGeneration ?
            Visibility.Visible : Visibility.Collapsed;


        [DependsOn(nameof(IsPaused))]
        [DependsOn(nameof(IsGluxVersionNewEnoughForGlueControlGeneration))]
        public Visibility UnpauseButtonVisibility => IsPaused && IsGluxVersionNewEnoughForGlueControlGeneration ?
            Visibility.Visible : Visibility.Collapsed;

        public bool IsGenerateGlueControlManagerInGame1Checked
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsGenerateGlueControlManagerInGame1Checked))]
        public Visibility RunningModeVisibility => IsGenerateGlueControlManagerInGame1Checked.ToVisibility();

        public string Configuration { get; set; }

        public Visibility CompileContentButtonVisibility
        {
            get => Get<Visibility>();
            set => Set(value);
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

        public PlayOrEdit PlayOrEdit
        {
            get => Get<PlayOrEdit>();
            set => Set(value);
        }

        [DependsOn(nameof(PlayOrEdit))]
        public bool IsPlayChecked
        {
            get => PlayOrEdit == PlayOrEdit.Play;
            set
            {
                if(value)
                {
                    PlayOrEdit = PlayOrEdit.Play;
                }
            }
        }

        [DependsOn(nameof(IsPlayChecked))]
        public Visibility RunIconVisibility => IsPlayChecked.ToVisibility();
        [DependsOn(nameof(IsPlayChecked))]
        public Visibility RunDisabledIconVisibility => (!IsPlayChecked).ToVisibility();

        [DependsOn(nameof(PlayOrEdit))]
        public bool IsEditChecked
        {
            get => PlayOrEdit == PlayOrEdit.Edit;
            set
            {
                if (value)
                {
                    PlayOrEdit = PlayOrEdit.Edit;
                }
            }
        }

        [DependsOn(nameof(IsEditChecked))]
        public Visibility EditIconVisibility => IsEditChecked.ToVisibility();
        [DependsOn(nameof(IsEditChecked))]
        public Visibility EditDisabledIconVisibility => (!IsEditChecked).ToVisibility();

        #endregion

        #region Constructor

        public CompilerViewModel()
        {
            CurrentGameSpeed = "100%";
        }

        #endregion

        #region Commands

        internal void DecreaseGameSpeed()
        {
            var index = GameSpeedList.IndexOf(CurrentGameSpeed);
            if (index < GameSpeedList.Count-1)
            {
                CurrentGameSpeed = GameSpeedList[index + 1];
            }
        }

        internal void IncreaseGameSpeed()
        {
            var index = GameSpeedList.IndexOf(CurrentGameSpeed);
            if(index > 0)
            {
                CurrentGameSpeed = GameSpeedList[index - 1];
            }
        }

        #endregion

        #region To/From Model

        internal void SetFrom(CompilerSettingsModel model)
        {
            this.IsGenerateGlueControlManagerInGame1Checked = model.GenerateGlueControlManagerCode;
        }

        public void SetModel(CompilerSettingsModel compilerSettings)
        {
            compilerSettings.GenerateGlueControlManagerCode = this.IsGenerateGlueControlManagerInGame1Checked;
        }

        #endregion
    }
}
