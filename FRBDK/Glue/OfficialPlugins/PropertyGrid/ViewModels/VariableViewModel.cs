using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace OfficialPlugins.PropertyGrid.ViewModels
{
    public class VariableViewModel : ViewModel
    {
        public bool CanAddVariable
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(CanAddVariable))]
        public Visibility AddVariableVisibility => CanAddVariable.ToVisibility();
    }
}
