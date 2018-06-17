using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.AddVariablePlugin.ViewModels
{
    public class CreateNewVariableViewModel : ViewModel
    {
        public ObservableCollection<string> AvailableVariableTypes
        {
            get { return Get<ObservableCollection<string>>(); }
            set { Set(value); }
        }

        public bool IncludeStateCategories
        {
            get { return Get<bool>(); }
            set {Set(value);}
        }

        public CreateNewVariableViewModel()
        {
            AvailableVariableTypes = new ObservableCollection<string>();
        }
    }
}
