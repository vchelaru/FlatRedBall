using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.StateDataPlugin.ViewModels
{
    class StateViewModel : ViewModel
    {
        public string Name
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public ObservableCollection<object> Variables
        {
            get { return Get<ObservableCollection<object>>(); }
            set { Set(value); }
        }

        public StateSave BackingData
        {
            get; set;
        }

        public StateViewModel(StateSave state, IElement element)
        {
            BackingData = state;

            Variables = new ObservableCollection<object>();
            Name = state?.Name;

            for(int i = 0; i < element.CustomVariables.Count; i++)
            {
                var variable = element.CustomVariables[i];

                // see if there is a value for this variable
                var instruction = 
                    state?.InstructionSaves.FirstOrDefault(item => item.Member == variable.Name);

                this.Variables.Add(instruction?.Value);
            }

        }
    }
}
