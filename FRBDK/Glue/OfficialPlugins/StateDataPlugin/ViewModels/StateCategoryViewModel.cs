using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.StateDataPlugin.ViewModels
{
    class StateCategoryViewModel : ViewModel
    {
        IElement element;
        StateSaveCategory category;

        public ObservableCollection<StateViewModel> States
        {
            get { return Get<ObservableCollection<StateViewModel>>(); }
            set { Set(value); }
        }

        public StateViewModel SelectedState
        {
            get { return Get<StateViewModel>(); }
            set { Set(value); }
        }

        public List<string> Columns
        {
            get { return Get<List<string>>(); }
            set { Set(value); }
        }

        public StateCategoryViewModel(StateSaveCategory category, IElement element)
        {
            this.element = element;
            this.category = category;

            Columns = new List<string>();

            States = new ObservableCollection<StateViewModel>();

            foreach (var state in category.States)
            {
                var stateViewModel = new StateViewModel(state, element);
                AssignEventsOn(stateViewModel);
                States.Add(stateViewModel);
            }

            CreateBlankViewModelAtEnd(element);
        }

        private void CreateBlankViewModelAtEnd(IElement element)
        {
            // add the blank one:
            var blankRowViewModel = new StateViewModel(null, element);
            AssignEventsOn(blankRowViewModel);
            States.Add(blankRowViewModel);
        }

        private void AssignEventsOn(StateViewModel stateViewModel)
        {
            stateViewModel.PropertyChanged += HandlePropertyChanged;
            stateViewModel.Variables.CollectionChanged += HandleVariableChanged;
        }

        private void HandleVariableChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Replace)
            {
                var index = e.NewStartingIndex;

                ApplyViewModelVariableToStateAtIndex(e.NewItems[0], index);

                GlueCommands.Self.GenerateCodeCommands.GenerateAllCodeTask();
                GlueCommands.Self.GluxCommands.SaveGluxTask();
            }
        }

        private void ApplyViewModelVariableToStateAtIndex(object value, int index)
        {
            if (index >= 0 && index < element.CustomVariables.Count)
            {
                var stateSave = SelectedState?.BackingData;

                if (stateSave != null)
                {
                    // If it's null, that means the user is editing the bottom-most row which doesn't yet have a state
                    // This will be handled in the HandlePropertyChanged
                    var variable = element.CustomVariables[index];

                    // need to cast the type, because newItems[0] will be a string


                    if(value == null || value as string == String.Empty)
                    {
                        var existingInstruction = stateSave.InstructionSaves.FirstOrDefault(item => item.TargetName == variable.Name);

                        if(existingInstruction != null)
                        {
                            stateSave.InstructionSaves.Remove(existingInstruction);
                        }
                    }
                    else
                    {
                        var convertedValue = Convert(value, variable);
                        stateSave.SetValue(variable.Name, convertedValue);
                    }

                }

            }
        }

        private object Convert(object whatToConvert, CustomVariable variable)
        {
            // early out:
            if(whatToConvert == null)
            {
                return null;
            }

            switch(variable.Type)
            {
                case "float":
                    return System.Convert.ToSingle(whatToConvert.ToString());
                case "int":
                    return System.Convert.ToInt32(whatToConvert.ToString());
                case "bool":
                    return System.Convert.ToBoolean(whatToConvert.ToString());
                case "long":
                    return System.Convert.ToInt64(whatToConvert.ToString());
                case "double":
                    return System.Convert.ToDouble(whatToConvert.ToString());
                default:
                    return whatToConvert;
            }
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(StateViewModel.Name))
            {
                StateSave stateSave = SelectedState.BackingData;
                if (SelectedState.BackingData == null && !string.IsNullOrEmpty(SelectedState.Name))
                {
                    stateSave = new StateSave();
                    stateSave.Name = SelectedState.Name;
                    var category = GlueState.Self.CurrentStateSaveCategory;
                    category.States.Add(stateSave);
                    SelectedState.BackingData = stateSave;

                    // Add a new entry so the user can create more states easily:
                    CreateBlankViewModelAtEnd(GlueState.Self.CurrentElement);

                    CopyViewModelValuesToState(SelectedState, stateSave);

                    GlueCommands.Self.RefreshCommands.RefreshUi(this.category);
                }
                stateSave.Name = SelectedState.Name;

                GlueCommands.Self.GenerateCodeCommands.GenerateAllCodeTask();
                GlueCommands.Self.GluxCommands.SaveGluxTask();
            }
        }

        private void CopyViewModelValuesToState(StateViewModel selectedViewModel, StateSave stateSave)
        {
            for (int i = 0; i < selectedViewModel.Variables.Count; i++)
            {
                ApplyViewModelVariableToStateAtIndex(selectedViewModel.Variables[i], i);
            }
        }
    }
}
