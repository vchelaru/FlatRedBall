using System;
using System.Collections.Generic;
using System.Text;

namespace GlueControl.Models
{
    public class StateSaveCategory
    {
        public List<StateSave> States { get; set; } = new List<StateSave>();

        public List<string> ExcludedVariables { get; set; } = new List<string>();

        public string Name
        {
            get;
            set;
        }

        public bool SharesVariablesWithOtherCategories
        {
            get;
            set;
        }

        public StateSaveCategory()
        {
            SharesVariablesWithOtherCategories = true;
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
