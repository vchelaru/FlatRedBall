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

        public ObservableCollection<object> Variables
        {
            get { return Get<ObservableCollection<object>>(); }
            set { Set(value); }
        }

        public StateSave BackingData
        {
            get { return Get<StateSave>(); }
            set { Set(value); }
        }

        public StateViewModel(StateSave state, StateSaveCategory category, IElement element)
        {
            this.category = category;
            this.element = element;

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

        public override string ToString()
        {
            return Name;
        }
    }
}
