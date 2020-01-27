using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPluginsCore.StateDataPlugin.Managers
{
    public class VariableInclusionManager
    {
        public static bool ShouldIncludeVariable(CustomVariable customVariable, StateSaveCategory category)
        {
            if(customVariable.GetIsVariableState())
            {

            }
            return customVariable.IsShared == false;
        }
    }
}
