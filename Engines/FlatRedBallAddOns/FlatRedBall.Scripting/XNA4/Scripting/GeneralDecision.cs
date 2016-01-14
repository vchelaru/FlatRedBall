using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Scripting;

namespace FlatRedBall.Scripting
{
    public class GeneralDecision: IScriptDecision
    {
        public Func<bool> ConditionFunc;
#if DEBUG

        public string Name
        {
            get;
            set;
        }
#endif
        public bool ConditionsAreMet()
        {
            return ConditionFunc();
        }


        public override string ToString()
        {
#if DEBUG
            if (string.IsNullOrEmpty(Name) == false)
            {
                return Name;
    	    }
#endif
            return base.ToString();
        }


    }
}
