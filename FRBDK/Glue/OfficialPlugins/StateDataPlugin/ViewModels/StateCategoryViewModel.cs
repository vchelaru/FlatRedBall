using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.StateDataPlugin.ViewModels
{
    class StateCategoryViewModel : ViewModel
    {
        public ObservableCollection<StateViewModel> States
        {
            get { return Get<ObservableCollection<StateViewModel>>(); }
            set { Set(value); }
        }

        public List<string> Columns
        {
            get { return Get<List<string>>(); }
            set { Set(value); }
        }

        public StateCategoryViewModel()
        {
            States = new ObservableCollection<StateViewModel>();

            States.Add(new StateViewModel
            {
                Name = "Hello"
            });

            States.Add(new StateViewModel
            {
                Name = "Hello2"
            });
        }
    }
}
