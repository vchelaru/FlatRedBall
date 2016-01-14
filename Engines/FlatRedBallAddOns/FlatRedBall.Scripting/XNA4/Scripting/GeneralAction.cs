using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Scripting;

namespace FlatRedBall.Scripting
{
    public class GeneralAction: IScriptAction
    {
        public string Name
        {
            get;
            set;
        }

        public Func<bool> IsCompleteFunction;

        public Action ActionToPerform;
        

        public bool IsComplete() 
        {
            if (IsCompleteFunction != null)
            {
                return IsCompleteFunction();
            }
            else
            {
                return true;
            }
        }

        public bool Execute()
        {
            ActionToPerform();
            return true;
        }


        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }
            else
            {
                return base.ToString();
            }
        }
    }
}
