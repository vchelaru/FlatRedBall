using FlatRedBall.Glue.MVVM;
using Newtonsoft.Json;
using System;
using System.Windows;

namespace CompilerPlugin.ViewModels
{
    public class CompilerViewModel : ViewModel
    {
        private Action<string, string> _eventCaller;

        public CompilerViewModel(Action<string, string> eventCaller)
        {
            _eventCaller = eventCaller;
        }

        #region Fields/Properties

        public bool HasLoadedGlux
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(HasLoadedGlux))]
        public Visibility EntireUiVisibility => HasLoadedGlux.ToVisibility();

        public bool IsRunning
        {
            get => Get<bool>();
            set {
                Set(value);
                _eventCaller("Compiler_Prop_IsRunning", JsonConvert.SerializeObject(new
                {
                    value = value
                }));
            }
        }

        public bool IsCompiling
        {
            get => Get<bool>();
            set
            {
                Set(value);
                _eventCaller("Compiler_Prop_IsCompiling", JsonConvert.SerializeObject(new
                {
                    value = value
                }));
            }
        }

        [DependsOn(nameof(IsCompiling))]
        public Visibility BuildingActivitySpinnerVisibility => IsCompiling.ToVisibility();

        public bool IsWaitingForGameToStart
        {
            get => Get<bool>();
            set => Set(value);
        }

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

        public bool DidRunnerStartProcess
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsGenerateGlueControlManagerInGame1Checked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string Configuration { get; set; }

        public bool IsPrintMsBuildCommandChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        #endregion

    }
}
