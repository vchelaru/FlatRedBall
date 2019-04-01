using FlatRedBall;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedGrinPlugin.ViewModels
{
    public class NetworkEntityViewModel : ViewModel
    {
        public bool IsNetworkEntity
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public ObservableCollection<NetworkVariableViewModel> VariableList
        {
            get { return Get<ObservableCollection<NetworkVariableViewModel>>(); }
            set { Set(value); }
        }

        public const string IsNetworkVariableProperty = "IsNetworkVariable";

        internal void SetFrom(EntitySave entity)
        {
            VariableList = new ObservableCollection<NetworkVariableViewModel>();
            IsNetworkEntity = entity.Properties.GetValue<bool>(nameof(IsNetworkEntity));

            var variables = entity
                .CustomVariables
                .ToArray();

            string[] variablesToAlwaysInclude = new string[]
            {
                // Don't include Name because this isn't something Glue understands as a variable
                //nameof(PositionedObject.Name),
                nameof(PositionedObject.X),
                nameof(PositionedObject.Y),
                nameof(PositionedObject.XVelocity),
                nameof(PositionedObject.YVelocity),
                nameof(PositionedObject.XAcceleration),
                nameof(PositionedObject.YAcceleration),
                nameof(PositionedObject.Drag),
                nameof(PositionedObject.RotationZ),
                nameof(PositionedObject.RotationZVelocity),
            };

            List<NetworkVariableViewModel> temporaryList = new List<NetworkVariableViewModel>();

            foreach(var variable in variables)
            {
                var variableVm = new NetworkVariableViewModel();
                variableVm.Name = variable.Name;
                variableVm.IsChecked = IsNetworked(variable);
                variableVm.PropertyChanged += HandleVariableVmPropertyChanged;
                temporaryList.Add(variableVm);
            }

            foreach(var variableName in variablesToAlwaysInclude)
            {
                var alreadyIncluded = temporaryList.Any(item => item.Name == variableName);

                if(!alreadyIncluded)
                {
                    var variableVm = new NetworkVariableViewModel();
                    variableVm.Name = variableName;
                    variableVm.IsChecked = false;
                    variableVm.PropertyChanged += HandleVariableVmPropertyChanged;
                    temporaryList.Add(variableVm);
                }
            }

            foreach(var vm in temporaryList.OrderBy(item =>item.Name))
            {
                VariableList.Add(vm);
            }
        }

        private void HandleVariableVmPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged("VariableIsChecked");
        }

        internal bool ApplyTo(EntitySave entity)
        {
            bool createdNewVariable = false;

            entity.Properties.SetValue(nameof(IsNetworkEntity), IsNetworkEntity);

            foreach(var variableVm in this.VariableList)
            {
                var matchingVariable = entity.GetCustomVariable(variableVm.Name);

                var isNetworkEntity = variableVm.IsChecked;

                // The network screen will show additional variables that may not be exposed as
                // variables in Glue, like velocity. In this case the user may enable these without 
                // actually having variables. We'll add them...hopefully that doesn't cause problems...
                if(isNetworkEntity && matchingVariable == null)
                {
                    matchingVariable = new CustomVariable
                    {
                        Name = variableVm.Name,
                        Type = "float", // do we always assume this? It is the case for all PositionedObject values
                    };
                    createdNewVariable = true;
                    entity.CustomVariables.Add(matchingVariable);
                }

                if(matchingVariable != null)
                {
                    matchingVariable.Properties.SetValue(IsNetworkVariableProperty, isNetworkEntity);
                }
            }

            return createdNewVariable;
        }

        public static bool IsNetworked(EntitySave entity)
        {
            return entity.Properties
               .GetValue<bool>(nameof(NetworkEntityViewModel.IsNetworkEntity));
        }
        public static bool IsNetworked(CustomVariable variable)
        {
            return variable.Properties
               .GetValue<bool>(IsNetworkVariableProperty);
        }
    }
}
