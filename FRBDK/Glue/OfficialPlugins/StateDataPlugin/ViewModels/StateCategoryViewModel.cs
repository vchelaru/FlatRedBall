using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPluginsCore.StateDataPlugin.Managers;
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
            get => Get<ObservableCollection<StateViewModel>>(); 
            set => Set(value); 
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

        /// <summary>
        /// The index of the state that is selected. This is only a valid number
        /// if an entire row is selected. Otherwise it's -1.
        /// </summary>
        public int SelectedIndex
        {
            get => Get<int>(); 
            set => Set(value); 
        }

        public StateViewModel SelectedState
        {
            get => Get<StateViewModel>();
            set => Set(value);
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

        public GridLength TopSectionHeight
        {
            get => Get<GridLength>();
            set => Set(value);
        }

        #endregion

        #region Initialize

        public StateCategoryViewModel(StateSaveCategory category, GlueElement element)
        {
            this.VariableManagementVisibility = Visibility.Collapsed;
            this.Element = element;
            this.category = category;

            Columns = new List<string>();

            States = new ObservableCollection<StateViewModel>();

            foreach (var state in category.States)
            {
                var stateViewModel = new StateViewModel(state, category, element);
                stateViewModel.ValueChanged += HandleStateViewModelValueChanged;
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
                .Where(item => VariableInclusionManager.ShouldIncludeVariable(item, this.category))
                .Select(item => item.Name)
                .ToList();

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
                    GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();
                    GlueCommands.Self.GluxCommands.SaveGlux();
                    break;
            }
        }

        private void AssignEventsOn(StateViewModel stateViewModel)
        {
            stateViewModel.PropertyChanged += HandlePropertyOnStateChanged;
            stateViewModel.Variables.CollectionChanged += HandleVariableCollectionOnStateChanged;
        }

        private void CreateBlankViewModelAtEnd(GlueElement element)
        {
            // add the blank one:
            var blankRowViewModel = new StateViewModel(null, category, element);
            blankRowViewModel.ValueChanged += HandleStateViewModelValueChanged;

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
                    var variableViewModel = e.NewItems[0] as StateVariableViewModel;
                    ApplyViewModelVariableToStateAtIndex(variableViewModel.Value, index, stateSave);

                    GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();
                    GlueCommands.Self.GluxCommands.SaveGlux();

                }
            }
        }

        public void ApplyViewModelVariableToStateAtIndex(object value, int index, StateViewModel stateViewModel)
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
                    ApplyValueToModel(value, stateViewModel, stateSave, variable);

                }

            }
        }

        private void HandleStateViewModelValueChanged(StateViewModel stateViewModel, StateVariableViewModel stateVariableViewModel)
        {
            var value = stateVariableViewModel.Value;
            var element = GlueState.Self.CurrentElement;
            var customVariable = element.GetCustomVariable(stateVariableViewModel.VariableName);
            ApplyValueToModel(value, stateViewModel, stateViewModel.BackingData, customVariable);
        }

        public void ApplyValueToModel(object value, StateViewModel stateViewModel, StateSave stateSave, CustomVariable variable)
        {
            if (value == null || value as string == String.Empty)
            {

                var existingInstruction = stateSave?.InstructionSaves.FirstOrDefault(item => item.Member == variable.Name);

                if (existingInstruction != null)
                {
                    stateSave.InstructionSaves.Remove(existingInstruction);
                }

                var wasRemoved = CheckForStateRemoval(stateViewModel);

                if(!wasRemoved)
                {
                    // The state wasn't removed, so a variable on the state was changed
                    PluginManager.ReactToStateVariableChanged(stateSave, category, variable.Name);
                }
            }
            else
            {
                try
                {
                    var convertedValue = Convert(value, variable);
                    // This could have converted incorrectly.
                    if(convertedValue != null)
                    {
                        stateSave.SetValue(variable.Name, convertedValue);
                    }
                    else
                    {
                        var existingInstruction = stateSave?.InstructionSaves.FirstOrDefault(item => item.Member == variable.Name);

                        if (existingInstruction != null)
                        {
                            stateSave.InstructionSaves.Remove(existingInstruction);
                        }

                    }

                    PluginManager.ReactToStateVariableChanged(stateSave, category, variable.Name);

                }
                catch (Exception e)
                {
                    GlueCommands.Self.PrintError(
                        $"Could not assign variable {variable.Name} ({variable.Type}) to \"{value}\":\n{e.ToString()}");
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

            try
            {
                var valueAsString = whatToConvert?.ToString();

                if(TypeManager.TryConvertStringValue(variable.Type, valueAsString, out object convertedValue))
                {
                    return convertedValue;
                }
                return whatToConvert;
            }
            catch
            {
                return null;
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

                var element = GlueState.Self.CurrentElement;

                if (needsToCreateNewState)
                {
                    stateSave = new StateSave();
                    stateSave.Name = selectedViewModel.Name;
                    var category = GlueState.Self.CurrentStateSaveCategory;
                    category.States.Add(stateSave);
                    selectedViewModel.BackingData = stateSave;

                    // Add a new entry so the user can create more states easily:
                    CreateBlankViewModelAtEnd(element);

                    CopyViewModelValuesToState(selectedViewModel, stateSave);

                }

                if(selectedViewModel.IsNameInvalid == false)
                {
                    stateSave.Name = selectedViewModel.Name;

                    if(needsToCreateNewState)
                    {
                        PluginManager.ReactToStateCreated(stateSave, category);
                    }

                    // Wow this is a heavy call! I don't think we need to re-generate the whole project. Just the current element should do:
                    // Also, even if it's on a small project (and not heavy) it causes a re-generation of the camera settings file, which restarts
                    // the current game.
                    //GlueCommands.Self.GenerateCodeCommands.GenerateAllCodeTask();
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);

                    GlueCommands.Self.GluxCommands.SaveGlux();
                }
                GlueCommands.Self.RefreshCommands.RefreshUi(this.category);

                if(string.IsNullOrEmpty(selectedViewModel.Name))
                {
                    CheckForStateRemoval(selectedViewModel);
                }
            }
        }

        /// <summary>
        /// Checks if a state should be removed based on the state of its ViewModel. If all columns are empty, then the state is removed from the Glue project.
        /// </summary>
        /// <param name="selectedState">The state view model which should be checked.</param>
        /// <returns>Whether a state was removed:</returns>
        private bool CheckForStateRemoval(StateViewModel selectedState)
        {
            var isLast = selectedState == this.States.Last();

            bool shouldRemove = false;

            if(!isLast)
            {
                var isEmpty = selectedState.Variables.All(item =>
                    item.Value == null ||
                    (item.Value is string && string.IsNullOrEmpty(item.Value as string)));



                shouldRemove = isEmpty && string.IsNullOrEmpty(selectedState.Name);
            }

            if(shouldRemove)
            {
                this.category.States.Remove(selectedState.BackingData);
                int oldSelectedIndex = this.States.IndexOf(selectedState);

                if(oldSelectedIndex > 0)
                {
                    this.States.RemoveAt(oldSelectedIndex);

                    GlueCommands.Self.RefreshCommands.RefreshUi(this.category);
                    GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();
                    GlueCommands.Self.GluxCommands.SaveGlux();
                    // Victor Chelaru June 8, 2018
                    // I can't get this to select, not sure why.
                    // I've tried setting the selected state, the selected
                    // index, and moved the code to different places in this method...
                    // but the data grid always resets to 0. This seems to be a bug/
                    // peculiar behavior of the WPF datagrid according to StackOverflow posts

                    //this.SelectedState = this.States[oldIndex - 1];
                    this.SelectedIndex = oldSelectedIndex - 1;
                }
            }

            return shouldRemove;
        }

        private void CopyViewModelValuesToState(StateViewModel selectedViewModel, StateSave stateSave)
        {
            for (int i = 0; i < selectedViewModel.Variables.Count; i++)
            {
                ApplyViewModelVariableToStateAtIndex(selectedViewModel.Variables[i].Value, i, selectedViewModel);
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

                GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();
                GlueCommands.Self.GluxCommands.SaveGlux();
                // I guess this isn't needed because the states in the tree view didn't change
                //GlueCommands.Self.RefreshCommands.RefreshUi(this.category);
            }
        }

        public void ExpandVariableManagement()
        {
            VariableManagementVisibility = Visibility.Visible;
            TopSectionHeight = new GridLength(100);
        }

        public void CollapseVariableManagement()
        {
            VariableManagementVisibility = Visibility.Collapsed;
            TopSectionHeight = new GridLength(1);

        }
    }
}
