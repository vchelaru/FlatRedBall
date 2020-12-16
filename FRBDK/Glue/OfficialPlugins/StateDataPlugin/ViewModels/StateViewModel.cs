using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPluginsCore.StateDataPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace OfficialPlugins.StateDataPlugin.ViewModels
{
    class StateVariableViewModel : ViewModel
    {
        public object Value
        {
            get { return Get<object>(); }
            set { Set(value); }
        }

        public object DefaultValue
        {
            get { return Get<object>(); }
            set { Set(value); }
        }

        public bool HasState
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        [DependsOn(nameof(Value))]
        [DependsOn(nameof(DefaultValue))]
        [DependsOn(nameof(HasState))]
        [DependsOn(nameof(IsFocused))]
        public object UiValue
        {
            get
            {
                if(Value != null)
                {
                    return Value;
                }
                else if(HasState && IsFocused == false)
                {
                    return DefaultValue;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if(value is string && string.IsNullOrEmpty(value as string))
                {
                    Value = null;
                }
                else
                {
                    Value = value;
                }
            }
        }

        [DependsOn(nameof(Value))]
        [DependsOn(nameof(DefaultValue))]
        public bool IsDefaultValue
        {
            get
            {
                return Value == null && DefaultValue != null;
            }
        }

        static Brush GreenBrush;

        [DependsOn(nameof(IsDefaultValue))]
        [DependsOn(nameof(IsFocused))]
        public Brush TextColor
        {
            get
            {
                if(IsDefaultValue && IsFocused == false)
                {
                    if(GreenBrush == null)
                    {
                        GreenBrush =
                            new SolidColorBrush(Color.FromRgb(0, 200, 0));
                    }

                    return GreenBrush;
                }
                else
                {
                    return Brushes.Black;
                }
            }
        }

        public string VariableName { get; set; }

        public bool IsFocused
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public override string ToString()
        {
            return $"{VariableName} = {Value}";
        }
    }

    class StateViewModel : ViewModel
    {
        IElement element;
        StateSaveCategory category;

        public string Name
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        [DependsOn(nameof(Name))]
        [DependsOn(nameof(BackingData))]
        public bool IsNameInvalid
        {
            get
            {
                string whyItIsInvalid = null;

                if(BackingData == null && string.IsNullOrEmpty(Name))
                {
                    return false; // allow blanks
                }
                else
                {
                    NameVerifier.IsStateNameValid(Name, element, category, 
                        BackingData, out whyItIsInvalid);
                }


                return !string.IsNullOrEmpty(whyItIsInvalid);
            }
        }

        public ObservableCollection<StateVariableViewModel> Variables
        {
            get { return Get<ObservableCollection<StateVariableViewModel>>(); }
            set { Set(value); }
        }

        public StateSave BackingData
        {
            get { return Get<StateSave>(); }
            set { Set(value); }
        }

        public event Action<StateViewModel, StateVariableViewModel> ValueChanged;

        public StateViewModel(StateSave state, StateSaveCategory category, IElement element)
        {
            this.category = category;
            this.element = element;

            BackingData = state;

            Variables = new ObservableCollection<StateVariableViewModel>();
            Name = state?.Name;

            for(int i = 0; i < element.CustomVariables.Count; i++)
            {
                var variable = element.CustomVariables[i];

                var shouldInclude = VariableInclusionManager.ShouldIncludeVariable(variable, category);

                if(shouldInclude)
                {

                    // see if there is a value for this variable
                    var instruction = 
                        state?.InstructionSaves.FirstOrDefault(item => item.Member == variable.Name);

                    var viewModel = new StateVariableViewModel();
                    viewModel.VariableName = variable.Name;
                    viewModel.Value = instruction?.Value;
                    viewModel.DefaultValue = element.GetCustomVariable(variable.Name)?.DefaultValue;
                    viewModel.PropertyChanged += HandleStateVariablePropertyChanged;
                    viewModel.HasState = state != null;
                    this.Variables.Add(viewModel);
                }
            }

        }

        private void HandleStateVariablePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // if the view model is null, the view model may not be instantiated yet
            ValueChanged?.Invoke(this, sender as StateVariableViewModel);
            var viewModel = sender as StateVariableViewModel;
            switch(e.PropertyName)
            {
                case nameof(StateVariableViewModel.Value):
                    if(viewModel.Value == null)
                    {
                        BackingData.RemoveVariable(viewModel.VariableName);
                    }
                    else
                    {
                        BackingData.SetValue(viewModel.VariableName, viewModel.Value);
                    }
                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCodeTask();
                    GlueCommands.Self.GluxCommands.SaveGlux();
                    break;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
