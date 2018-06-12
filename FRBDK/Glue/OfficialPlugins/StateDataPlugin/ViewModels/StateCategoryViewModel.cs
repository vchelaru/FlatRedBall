using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OfficialPlugins.StateDataPlugin.ViewModels
{
    class StateCategoryViewModel : ViewModel
    {
        #region Fields/Properties

        /// <summary>
        ///  Allows this class to make changes to the ViewModel ignored variable list without
        ///  pushing those changes to the Glue project - such as initial population of the ViewModel
        /// </summary>
        bool ignoreExcludedVariableChanges;

        public IElement Element { get; private set; }
        StateSaveCategory category;

        public ObservableCollection<StateViewModel> States
        {
            get { return Get<ObservableCollection<StateViewModel>>(); }
            set { Set(value); }
        }

        public string Name
        {
            get { return Get<string>();}
            set { Set(value); }
        }

        public ObservableCollection<string> IncludedVariables
        {
            get { return Get<ObservableCollection<string>>(); }
            set { Set(value); }
        }
        public string SelectedIncludedVariable
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public ObservableCollection<string> ExcludedVariables
        {
            get { return Get<ObservableCollection<string>>(); }
            set { Set(value); }
        }
        public string SelectedExcludedVariable
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public int SelectedIndex
        {
            get { return Get<int>(); }
            set { Set(value); }
        }

        public List<string> Columns
        {
            get { return Get<List<string>>(); }
            set { Set(value); }
        }

        public Visibility VariableManagementVisibility
        {
            get { return Get<Visibility>(); }
            set { Set(value); }
        }

        [DependsOn(nameof(VariableManagementVisibility))]
        public Visibility ExpandVariableManagementButtonVisibility
        {
            get
            {
                if(VariableManagementVisibility == Visibility.Collapsed)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }


        #endregion

        #region Initialize

        public StateCategoryViewModel(StateSaveCategory category, IElement element)
        {
            this.VariableManagementVisibility = Visibility.Collapsed;
            this.Element = element;
            this.category = category;

            Columns = new List<string>();

            States = new ObservableCollection<StateViewModel>();

            foreach (var state in category.States)
            {
                var stateViewModel = new StateViewModel(state, category, element);
                AssignEventsOn(stateViewModel);
                States.Add(stateViewModel);
            }

            CreateBlankViewModelAtEnd(element);


            IncludedVariables = new ObservableCollection<string>();
            ExcludedVariables = new ObservableCollection<string>();
            ExcludedVariables.CollectionChanged += HandleExcludedVariablesChanged;

            RefreshExcludedAndIncludedVariables();

            this.Name = category.Name;

            this.PropertyChanged += HandlePropertyChanged;
        }

        private void RefreshExcludedAndIncludedVariables()
        {
            var allVariableNames = Element.CustomVariables
                .Select(item => item.Name).ToList();

            var excludedVariables = category.ExcludedVariables;
            var includedVariables = allVariableNames.Except(excludedVariables);

            IncludedVariables.Clear();
            foreach (var variable in includedVariables)
            {
                this.IncludedVariables.Add(variable);
            }

            ignoreExcludedVariableChanges = true;

            ExcludedVariables.Clear();
            foreach(var variable in excludedVariables)
            {
                this.ExcludedVariables.Add(variable);
            }
            ignoreExcludedVariableChanges = false;
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(Name):
                    this.category.Name = this.Name;

                    // todo - eventually may need to handle variables renaming here
                    GlueCommands.Self.RefreshCommands.RefreshUi(this.category);
                    GlueCommands.Self.GenerateCodeCommands.GenerateAllCodeTask();
                    GlueCommands.Self.GluxCommands.SaveGluxTask();
                    break;
            }
        }

        private void AssignEventsOn(StateViewModel stateViewModel)
        {
            stateViewModel.PropertyChanged += HandlePropertyOnStateChanged;
            stateViewModel.Variables.CollectionChanged += HandleVariableCollectionOnStateChanged;
        }

        private void CreateBlankViewModelAtEnd(IElement element)
        {
            // add the blank one:
            var blankRowViewModel = new StateViewModel(null, category, element);
            AssignEventsOn(blankRowViewModel);
            States.Add(blankRowViewModel);
        }

        #endregion

        private void HandleVariableCollectionOnStateChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Replace)
            {
                var index = e.NewStartingIndex;

                var stateSave = States.FirstOrDefault(item => item.Variables == sender);

                if(stateSave != null)
                {
                    ApplyViewModelVariableToStateAtIndex(e.NewItems[0], index, stateSave);

                    GlueCommands.Self.GenerateCodeCommands.GenerateAllCodeTask();
                    GlueCommands.Self.GluxCommands.SaveGluxTask();

                }
            }
        }

        private void ApplyViewModelVariableToStateAtIndex(object value, int index, StateViewModel stateViewModel)
        {
            if (index >= 0 && index < Element.CustomVariables.Count)
            {
                var stateSave = stateViewModel.BackingData;

                if (stateSave != null)
                {
                    // If it's null, that means the user is editing the bottom-most row which doesn't yet have a state
                    // This will be handled in the HandlePropertyChanged
                    var variable = Element.CustomVariables[index];

                    // need to cast the type, because newItems[0] will be a string


                    if(value == null || value as string == String.Empty)
                    {
                        var existingInstruction = stateSave.InstructionSaves.FirstOrDefault(item => item.Member == variable.Name);

                        if(existingInstruction != null)
                        {
                            stateSave.InstructionSaves.Remove(existingInstruction);
                        }

                        CheckForStateRemoval(stateViewModel);
                    }
                    else
                    {
                        try
                        {
                            var convertedValue = Convert(value, variable);
                            stateSave.SetValue(variable.Name, convertedValue);
                        }
                        catch(Exception e)
                        {
                            GlueCommands.Self.PrintError(
                                $"Could not assign variable {variable.Name} ({variable.Type}) to \"{value}\":\n{e.ToString()}");
                        }
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
                    return System.Convert.ToSingle(whatToConvert.ToString(), CultureInfo.InvariantCulture);
                case "int":
                    return System.Convert.ToInt32(whatToConvert.ToString());
                case "bool":
                    return System.Convert.ToBoolean(whatToConvert.ToString());
                case "long":
                    return System.Convert.ToInt64(whatToConvert.ToString());
                case "double":
                    return System.Convert.ToDouble(whatToConvert.ToString(), CultureInfo.InvariantCulture);
                case "byte":
                    return System.Convert.ToByte(whatToConvert.ToString());
                default:
                    return whatToConvert;
            }
        }

        internal void IncludedSelected()
        {
            if(!string.IsNullOrEmpty(SelectedExcludedVariable))
            {
                var oldSelectedIndex = ExcludedVariables.IndexOf(SelectedExcludedVariable);

                var whatToMove = SelectedExcludedVariable;
                IncludedVariables.Add(whatToMove);
                ExcludedVariables.Remove(whatToMove);
                SelectedIncludedVariable = whatToMove;

                if(oldSelectedIndex < ExcludedVariables.Count)
                {
                    SelectedExcludedVariable = ExcludedVariables[oldSelectedIndex];
                }
                else if(ExcludedVariables.Count > 0)
                {
                    SelectedExcludedVariable = ExcludedVariables[ExcludedVariables.Count - 1];
                }
            }
        }

        internal void ExcludeSelected()
        {
            if(!string.IsNullOrEmpty(SelectedIncludedVariable))
            {
                var oldSelectedIndex = IncludedVariables.IndexOf(SelectedIncludedVariable);

                var whatToMove = SelectedIncludedVariable;
                ExcludedVariables.Add(whatToMove);
                IncludedVariables.Remove(whatToMove);
                SelectedExcludedVariable = whatToMove;

                if(oldSelectedIndex < IncludedVariables.Count)
                {
                    SelectedIncludedVariable = IncludedVariables[oldSelectedIndex];
                }
                else if(IncludedVariables.Count > 0)
                {
                    SelectedIncludedVariable = IncludedVariables[IncludedVariables.Count -1];
                }
            }
        }

        private void HandlePropertyOnStateChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(StateViewModel.Name))
            {
                StateViewModel selectedViewModel = sender as StateViewModel;
                StateSave stateSave = selectedViewModel.BackingData;

                var needsToCreateNewState = selectedViewModel.BackingData == null && 
                    !string.IsNullOrEmpty(selectedViewModel.Name) &&
                    selectedViewModel.IsNameInvalid == false;

                if (needsToCreateNewState)
                {
                    stateSave = new StateSave();
                    stateSave.Name = selectedViewModel.Name;
                    var category = GlueState.Self.CurrentStateSaveCategory;
                    category.States.Add(stateSave);
                    selectedViewModel.BackingData = stateSave;

                    // Add a new entry so the user can create more states easily:
                    CreateBlankViewModelAtEnd(GlueState.Self.CurrentElement);

                    CopyViewModelValuesToState(selectedViewModel, stateSave);

                }

                if(selectedViewModel.IsNameInvalid == false)
                {
                    stateSave.Name = selectedViewModel.Name;

                    GlueCommands.Self.GenerateCodeCommands.GenerateAllCodeTask();
                    GlueCommands.Self.GluxCommands.SaveGluxTask();
                }
                GlueCommands.Self.RefreshCommands.RefreshUi(this.category);

                if(string.IsNullOrEmpty(selectedViewModel.Name))
                {
                    CheckForStateRemoval(selectedViewModel);
                }
            }
        }

        private void CheckForStateRemoval(StateViewModel selectedState)
        {
            var isLast = selectedState == this.States.Last();

            bool shouldRemove = false;

            if(!isLast)
            {
                var isEmpty = selectedState.Variables.All(item =>
                    item == null ||
                    (item is string && string.IsNullOrEmpty(item as string)));



                shouldRemove = isEmpty && string.IsNullOrEmpty(selectedState.Name);
            }

            if(shouldRemove)
            {
                this.category.States.Remove(selectedState.BackingData);
                int oldIndex = this.States.IndexOf(selectedState);


                this.States.RemoveAt(oldIndex);

                GlueCommands.Self.RefreshCommands.RefreshUi(this.category);
                GlueCommands.Self.GenerateCodeCommands.GenerateAllCodeTask();
                GlueCommands.Self.GluxCommands.SaveGluxTask();

                if(oldIndex > 0)
                {
                    // Victor Chelaru June 8, 2018
                    // I can't get this to select, not sure why.
                    // I've tried setting the selected state, the selected
                    // index, and moved the code to different places in this method...
                    // but the data grid always resets to 0. This seems to be a bug/
                    // peculiar behavior of the WPF datagrid according to StackOverflow posts

                    //this.SelectedState = this.States[oldIndex - 1];
                    this.SelectedIndex = oldIndex - 1;
                }
            }
        }

        private void CopyViewModelValuesToState(StateViewModel selectedViewModel, StateSave stateSave)
        {
            for (int i = 0; i < selectedViewModel.Variables.Count; i++)
            {
                ApplyViewModelVariableToStateAtIndex(selectedViewModel.Variables[i], i, selectedViewModel);
            }
        }

        private void HandleExcludedVariablesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Probably easier to just blast and refresh the list:
            var needToAdd = this.ExcludedVariables.Except(category.ExcludedVariables).Any();
            var needToRemove = category.ExcludedVariables.Except(this.ExcludedVariables).Any();

            if (!ignoreExcludedVariableChanges && ( needToAdd || needToRemove))
            {
                category.ExcludedVariables.Clear();
                category.ExcludedVariables.AddRange(this.ExcludedVariables);

                GlueCommands.Self.GenerateCodeCommands.GenerateAllCodeTask();
                GlueCommands.Self.GluxCommands.SaveGluxTask();
                // I guess this isn't needed because the states in the tree view didn't change
                //GlueCommands.Self.RefreshCommands.RefreshUi(this.category);
            }
        }
    }
}
