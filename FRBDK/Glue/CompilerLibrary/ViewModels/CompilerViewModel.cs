using FlatRedBall.Glue.MVVM;
//using GameCommunicationPlugin.GlueControl.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CompilerLibrary.ViewModels
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

        static CompilerViewModel self;
        public static CompilerViewModel Self
        {
            get
            {
                if (self == null) self = new CompilerViewModel();
                return self;
            }
        }

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
                    // This can cause timing values, so let's avoid setting this here, and instead have it set intentionally where it's needed
                    //if(value)
                    //{
                    //    // no need to push this if we stopped the game from running
                    //    PlayOrEdit = PlayOrEdit.Play;
                    //}
                }
            }
        }

        public bool IsCompiling
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsCompiling))]
        public Visibility BuildingActivitySpinnerVisibility => IsCompiling.ToVisibility();

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
        [DependsOn(nameof(IsWindowEmbedded))]
        public Visibility WhileRunningEmbeddedVisibility => (IsRunning && IsWindowEmbedded).ToVisibility();

        public bool HasWindowPointer
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsWindowEmbedded
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(HasWindowPointer))]
        [DependsOn(nameof(IsWindowEmbedded))]
        [DependsOn(nameof(IsRunning))]
        public string GameRunningNotInWindowDisplay
        {
            get
            {
                if(!IsRunning)
                {
                    return string.Empty;
                }
                else if(!HasWindowPointer)
                {
                    return "The game is running in the background (no window is visible)";
                }
                else if(!IsWindowEmbedded)
                {
                    return "The game is running in an external window";
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        [DependsOn(nameof(IsEditChecked))]
        [DependsOn(nameof(IsRunning))]
        public Visibility EditingToolsVisibility => (IsRunning && IsEditChecked).ToVisibility();

        [DependsOn(nameof(IsEditChecked))]
        [DependsOn(nameof(IsRunning))]
        public bool EditingToolEnabled => IsRunning && IsEditChecked;

        [DependsOn(nameof(IsEditChecked))]
        [DependsOn(nameof(IsRunning))] 
        public double ToolBarOpacity => (IsRunning && IsEditChecked) ? 1 : 0.3;

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

        [DependsOn(nameof(IsCompiling))]
        public Visibility WhileBuildingVisibility => IsCompiling.ToVisibility();

        [DependsOn(nameof(WhileStoppedViewVisibility))]
        [DependsOn(nameof(IsGenerateGlueControlManagerInGame1Checked))]
        public Visibility RunInEditModeButtonVisibility =>
            (WhileStoppedViewVisibility == Visibility.Visible && IsGenerateGlueControlManagerInGame1Checked).ToVisibility();

        public bool IsPaused
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsGluxVersionNewEnoughForGlueControlGeneration))]
        public Visibility GenerateGlueViewCheckboxVisibility =>
            // formerly:
            //GlueControlDependentVisibility => 
            (IsGluxVersionNewEnoughForGlueControlGeneration).ToVisibility();

        [DependsOn(nameof(IsPaused))]
        [DependsOn(nameof(IsGluxVersionNewEnoughForGlueControlGeneration))]
        [DependsOn(nameof(PlayOrEdit))]
        [DependsOn(nameof(IsGenerateGlueControlManagerInGame1Checked))]
        public Visibility PauseButtonVisibility => (
            !IsPaused && 
            IsGluxVersionNewEnoughForGlueControlGeneration &&
            PlayOrEdit == PlayOrEdit.Play &&
            IsGenerateGlueControlManagerInGame1Checked
            ).ToVisibility();

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
        [DependsOn(nameof(IsGenerateGlueControlManagerInGame1Checked))]
        public Visibility UnpauseButtonVisibility => (
            IsPaused &&
            IsGluxVersionNewEnoughForGlueControlGeneration &&
            IsGenerateGlueControlManagerInGame1Checked ).ToVisibility();

        public bool IsGenerateGlueControlManagerInGame1Checked
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsGenerateGlueControlManagerInGame1Checked))]
        public Visibility GlueViewCommandUiVisibility => IsGenerateGlueControlManagerInGame1Checked.ToVisibility();

        [DependsOn(nameof(IsGenerateGlueControlManagerInGame1Checked))]
        public Visibility MessageAboutEditModeDisabledVisibility => (IsGenerateGlueControlManagerInGame1Checked == false).ToVisibility();

        public string Configuration { get; set; }

        public bool IsPrintMsBuildCommandChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public List<string> GameSpeedList { get; set; } =
            new List<string>
            {
                "4000%",
                "2000%",
                "1000%",
                "500%",
                "200%",
                "100%",
                "50%",
                "25%",
                "10%",
                "5%"
            };

        public string CurrentGameSpeed
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CurrentZoomLevelDisplay
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ResolutionDisplayText
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

        [DependsOn(nameof(PlayOrEdit))]
        public Visibility FocusButtonVisibility => (PlayOrEdit == PlayOrEdit.Edit).ToVisibility();

        public double LastWaitTimeInSeconds
        {
            get => Get<double>();
            set => Set(value);
        }

        [DependsOn(nameof(PlayOrEdit))]
        [DependsOn(nameof(IsRunning))]
        public Visibility ConnectedFrameVisibility
        {
            get => (PlayOrEdit == PlayOrEdit.Edit && IsRunning).ToVisibility();
        }

        [DependsOn(nameof(LastWaitTimeInSeconds))]
        public string ConnectedString
        {
            get
            {
                if(LastWaitTimeInSeconds < 1)
                {
                    return $"Connected {LastWaitTimeInSeconds:0.0}";
                }
                else
                {
                    return $"Waiting for game {LastWaitTimeInSeconds:0.0}";
                }
            }
        }

        [DependsOn(nameof(LastWaitTimeInSeconds))]
        public Brush ConnectedFrameBackgroundColor
        {
            get
            {
                if(LastWaitTimeInSeconds < 1)
                {
                    return Brushes.Green;
                }
                else if(LastWaitTimeInSeconds < 3)
                {
                    return Brushes.Yellow;
                }
                else
                {
                    return Brushes.Red;
                }
            }
        }

        public bool HasDraggedTreeNodeOverView
        {
            get => Get<bool>();
            set => Set(value);
        }


        [DependsOn(nameof(HasDraggedTreeNodeOverView))]
        [DependsOn(nameof(IsRunning))]
        public bool ShowDragDrop => IsRunning && HasDraggedTreeNodeOverView;

        //[DependsOn(nameof(ShowDragDrop))]
        //public GridLength DragDropHeight => ShowDragDrop
        //    ? new GridLength(1, GridUnitType.Star)
        //    : new GridLength(0);

        //[DependsOn(nameof(ShowDragDrop))]
        //public GridLength GameWindowHeight => ShowDragDrop
        //    ? new GridLength(0)
        //    : new GridLength(1, GridUnitType.Star);

        [DependsOn(nameof(IsRunning))]
        [DependsOn(nameof(IsGenerateGlueControlManagerInGame1Checked))]
        public Visibility ShowCommandsCheckboxVisibility => (IsRunning && IsGenerateGlueControlManagerInGame1Checked).ToVisibility();

        public bool IsPrintEditorToGameCheckboxChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsPrintGameToEditorCheckboxChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(ShowCommandsCheckboxVisibility))]
        [DependsOn(nameof(IsPrintEditorToGameCheckboxChecked))]
        public Visibility CommandParameterCheckboxVisibility =>
            (ShowCommandsCheckboxVisibility == Visibility.Visible && IsPrintEditorToGameCheckboxChecked).ToVisibility();

        public bool IsShowParametersChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public ObservableCollection<ToolbarEntityAndStateViewModel> ToolbarEntitiesAndStates
        {
            get => Get<ObservableCollection<ToolbarEntityAndStateViewModel>>();
            set => Set(value);
        }


        #endregion

        #region Constructor

        CompilerViewModel()
        {
            CurrentGameSpeed = "100%";
            ToolbarEntitiesAndStates = new ObservableCollection<ToolbarEntityAndStateViewModel>();
        }

        #endregion

        #region Commands

        public void DecreaseGameSpeed()
        {
            var index = GameSpeedList.IndexOf(CurrentGameSpeed);
            if (index < GameSpeedList.Count-1)
            {
                CurrentGameSpeed = GameSpeedList[index + 1];
            }
        }

        public void IncreaseGameSpeed()
        {
            var index = GameSpeedList.IndexOf(CurrentGameSpeed);
            if(index > 0)
            {
                CurrentGameSpeed = GameSpeedList[index - 1];
            }
        }

        #endregion

    }
}
