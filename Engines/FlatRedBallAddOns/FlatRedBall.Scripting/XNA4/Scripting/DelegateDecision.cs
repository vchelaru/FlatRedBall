using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Scripting
{
    public class DelegateDecision : IScriptDecision
    {
        
        public Func<bool> ConditionFunc;

        public bool ConditionsAreMet()
        {
            return ConditionFunc();
        }
    }
}
