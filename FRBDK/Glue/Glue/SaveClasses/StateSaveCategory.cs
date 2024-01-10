using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.SaveClasses
{
    public class StateSaveCategory
    {
        public string Name
        {
            get;
            set;
        }

        public List<StateSave> States { get; set; } = new List<StateSave>();

        public List<string> ExcludedVariables { get; set; } = new List<string>();

        public StateSaveCategory Clone()
        {
            var toReturn = new StateSaveCategory();

            toReturn.Name = this.Name;

            foreach(var state in States)
            {
                toReturn.States.Add(state.Clone());
            }

            toReturn.ExcludedVariables.AddRange(ExcludedVariables);

            return toReturn;
        }

        public StateSaveCategory()
        {
        }

        public StateSave GetState(string stateName)
        {
            foreach (StateSave stateSave in States)
            {
                if (stateSave.Name == stateName)
                {
                    return stateSave;
                }
            }
            return null;
        }

        public override string ToString()
        {
            return Name + " (State Category)";
        }
    }
}
