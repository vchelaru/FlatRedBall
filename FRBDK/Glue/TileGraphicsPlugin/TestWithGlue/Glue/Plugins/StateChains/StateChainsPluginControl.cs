using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.SaveClasses;

namespace PluginTestbed.StateChains
{
    public partial class StateChainsPluginControl : UserControl
    {
        private EntitySave _currentEntitySave;
        private StateChain _currentStateChain;
        private StateChainState _currentStateChainState;
        private StateChainCollection _currentStateChainCollection;
        private readonly IGlueCommands _glueCommands;
        private readonly BindingSource _stateChainsSource = new BindingSource();
        private readonly BindingSource _stateChainStatesSource = new BindingSource();

        public StateChainsPluginControl(IGlueCommands glueCommands)
        {
            InitializeComponent();
            _glueCommands = glueCommands;

            lbStateChains.DataSource = _stateChainsSource;

            lbStates.DataSource = _stateChainStatesSource;
        }

        public EntitySave CurrentEntitySave
        {
            get { return _currentEntitySave; }
            set 
            { 
                _currentEntitySave = value;

                epMain.Clear();

                if (_currentEntitySave != null)
                {
                    var stateChainCollection =
                        _glueCommands.TreeNodeCommands.GetProperty<StateChainCollection>(StateChainsPlugin.PropertyName);

                    if (stateChainCollection == null)
                    {
                        CurrentStateChainCollection = new StateChainCollection();

                        SaveToProperty();
                    }
                    else
                    {
                        CurrentStateChainCollection = stateChainCollection;
                    }
                }else
                {
                    CurrentStateChainCollection = null;
                }
            }
        }

        private StateChainCollection CurrentStateChainCollection
        {
            get { return _currentStateChainCollection; }
            set
            {
                _currentStateChainCollection = value;
                CurrentStateChain = null;
                UpdateToCurrentStateChainCollection();
            }
        }

        private StateChain CurrentStateChain
        {
            get { return _currentStateChain; }
            set
            {
                _currentStateChain = value;
                CurrentStateChainState = null;
                UpdateToCurrentStateChain();
            }
        }

        private StateChainState CurrentStateChainState
        {
            get { return _currentStateChainState; }
            set
            {
                _currentStateChainState = value;
                UpdateToCurrentStateChainState();
            }
        }

        private void UpdateToCurrentStateChainCollection()
        {
            if (CurrentStateChainCollection != null)
            {
                lbStateChains.SelectedItem = null;

                _stateChainsSource.DataSource = CurrentStateChainCollection.StateChains;
                _stateChainsSource.ResetBindings(false);

                lbStateChains.Enabled = true;
                btnAddStateChain.Enabled = true;
                btnDeleteStateChain.Enabled = false;
            }
            else
            {
                lbStateChains.SelectedItem = null;

                _stateChainsSource.DataSource = null;
                _stateChainsSource.ResetBindings(false);

                lbStateChains.Enabled = false;
                btnAddStateChain.Enabled = false;
                btnDeleteStateChain.Enabled = false;
            }
        }
        
        private void SaveToProperty()
        {
            _glueCommands.TreeNodeCommands.SetProperty(StateChainsPlugin.PropertyName, CurrentStateChainCollection);

            _glueCommands.GenerateCodeCommands.GenerateElementCode(CurrentEntitySave);

            _glueCommands.GluxCommands.SaveGlux();

            _stateChainsSource.ResetBindings(false);
            _stateChainStatesSource.ResetBindings(false);
        }

        private IEnumerable<string> GetStates()
        {
            return CurrentEntitySave.States.Select(stateSave => stateSave.Name).ToList();
        }

        private string GetStateChainNewName()
        {
            var i = 1;
            while (true)
            {
                var newName = "StateChain" + i;

                if (CurrentStateChainCollection.StateChains.Where(stateChain => stateChain.Name == newName).Count() == 0)
                {
                    return newName;
                }

                i++;
            }
        }

        private void UpdateToCurrentStateChain()
        {
            if(CurrentStateChain != null)
            {
                lbStates.SelectedItem = null;

                _stateChainStatesSource.DataSource = CurrentStateChain.StateChainStates;
                _stateChainStatesSource.ResetBindings(false);
                
                tbName.Text = CurrentStateChain.Name;

                lbStates.Enabled = true;
                tbName.Enabled = true;
                btnAddStateChainState.Enabled = true;
                btnDeleteStateChainState.Enabled = false;
            }else
            {
                lbStates.SelectedItem = null;

                _stateChainStatesSource.DataSource = null;
                _stateChainStatesSource.ResetBindings(false);

                tbName.Text = "";

                lbStates.Enabled = false;
                tbName.Enabled = false;
                btnAddStateChainState.Enabled = false;
                btnDeleteStateChainState.Enabled = false;
            }
        }

        private void UpdateToCurrentStateChainState()
        {
            if(CurrentStateChainState != null)
            {
                cbState.Items.Clear();
                foreach (var state in GetStates())
                {
                    cbState.Items.Add(state);
                }

                cbState.SelectedItem = CurrentStateChainState.State;

                tbTime.Text = CurrentStateChainState.Time.ToString();

                cbState.Enabled = true;
                tbTime.Enabled = true;
                btnMoveUpStateChainState.Enabled = true;
                btnMoveDownStateChainState.Enabled = true;
            }else
            {
                cbState.Items.Clear();
                cbState.SelectedItem = null;
                cbState.Text = "";
                tbTime.Text = "";

                cbState.Enabled = false;
                tbTime.Enabled = false;
                btnMoveUpStateChainState.Enabled = false;
                btnMoveDownStateChainState.Enabled = false;
            }
        }

        private void BtnAddStateChainClick(object sender, EventArgs e)
        {
            var newStateChain = new StateChain {Name = GetStateChainNewName()};

            CurrentStateChainCollection.StateChains.Add(newStateChain);

            UpdateToCurrentStateChainCollection();

            lbStateChains.SelectedItem = newStateChain;

            SaveToProperty();
        }

        private void BtnDeleteStateChainClick(object sender, EventArgs e)
        {
            CurrentStateChainCollection.StateChains.Remove(CurrentStateChain);

            CurrentStateChain = null;

            UpdateToCurrentStateChainCollection();

            SaveToProperty();
        }

        private void LbStateChainsSelectedValueChanged(object sender, EventArgs e)
        {
            if(lbStateChains.SelectedItem != null)
            {
                btnDeleteStateChain.Enabled = true;
                CurrentStateChain = (StateChain) lbStateChains.SelectedItem;
            }else
            {
                btnDeleteStateChain.Enabled = false;
                CurrentStateChain = null;
            }
        }

        private void TbNameValidating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (CurrentStateChain == null) return;
            if (!String.IsNullOrEmpty(tbName.Text.Trim())) return;

            e.Cancel = true;
            epMain.SetError(tbName, "Name can not be empty.");
        }

        private void TbNameValidated(object sender, EventArgs e)
        {
            epMain.Clear();
            if (CurrentStateChain == null) return;

            CurrentStateChain.Name = tbName.Text;

            SaveToProperty();
        }

        private void BtnAddStateChainStateClick(object sender, EventArgs e)
        {
            var newStateChainState = new StateChainState {State = CurrentEntitySave.States.Count > 0 ? CurrentEntitySave.States[0].Name : ""};

            CurrentStateChain.StateChainStates.Add(newStateChainState);

            UpdateToCurrentStateChain();

            lbStates.SelectedItem = newStateChainState;

            SaveToProperty();
        }

        private void BtnDeleteStateChainStateClick(object sender, EventArgs e)
        {
            CurrentStateChain.StateChainStates.Remove(CurrentStateChainState);

            CurrentStateChainState = null;

            UpdateToCurrentStateChainState();

            SaveToProperty();
        }

        private void LbStatesSelectedValueChanged(object sender, EventArgs e)
        {
            if (lbStates.SelectedItem != null)
            {
                btnDeleteStateChainState.Enabled = true;
                CurrentStateChainState = (StateChainState)lbStates.SelectedItem;
            }
            else
            {
                btnMoveUpStateChainState.Enabled = false;
                CurrentStateChainState = null;
            }
        }

        private void CbStateValidating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (CurrentStateChainState == null) return;
            if (String.IsNullOrEmpty(cbState.Text.Trim())) return;

            var found = false;

            if(CurrentEntitySave.States.Where(state => state.Name == cbState.Text).Count() > 0)
            {
                found = true;
            }

            if (found) return;

            e.Cancel = true;
            epMain.SetError(cbState, "Not a valid state.");
        }

        private void CbStateValidated(object sender, EventArgs e)
        {
            epMain.Clear();
            if (CurrentStateChainState == null) return;

            CurrentStateChainState.State = cbState.Text;

            SaveToProperty();
        }

        private void TbTimeValidating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (CurrentStateChainState == null) return;

            int timeValue;

            if (int.TryParse(tbTime.Text, out timeValue)) return;

            e.Cancel = true;
            epMain.SetError(tbName, "Time must be an integer.");
        }

        private void TbTimeValidated(object sender, EventArgs e)
        {
            epMain.Clear();
            if (CurrentStateChainState == null) return;

            CurrentStateChainState.Time = int.Parse(tbTime.Text);

            SaveToProperty();
        }

        private void BtnMoveUpStateChainStateClick(object sender, EventArgs e)
        {
            var index = CurrentStateChain.StateChainStates.IndexOf(CurrentStateChainState);

            if(index == 0) return;

            CurrentStateChain.StateChainStates.RemoveAt(index);
            CurrentStateChain.StateChainStates.Insert(index - 1, CurrentStateChainState);

            _stateChainStatesSource.ResetBindings(false);
            SaveToProperty();
        }

        private void BtnMoveDownStateChainStateClick(object sender, EventArgs e)
        {
            var index = CurrentStateChain.StateChainStates.IndexOf(CurrentStateChainState);

            if(index == CurrentStateChain.StateChainStates.Count - 1) return;

            CurrentStateChain.StateChainStates.RemoveAt(index);
            CurrentStateChain.StateChainStates.Insert(index + 1, CurrentStateChainState);

            _stateChainStatesSource.ResetBindings(false);
            SaveToProperty();
        }
    }
}
