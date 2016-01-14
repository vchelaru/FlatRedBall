using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Elements;

namespace FlatRedBall.Glue.SaveClasses
{
    public interface IBehaviorContainer
    {


        List<BehaviorSave> Behaviors
        {
            get;
            set;
        }

        List<CustomVariable> CustomVariables
        {
            get;
            set;
        }

		List<string> FulfilledRequirements
		{
			get;
		}

        string BaseObject
        {
            get;
            set;
        }

		CustomVariable AddCustomVariable(string propertyType, string propertyName);

        BehaviorSave GetBehavior(string behaviorName);

        CustomVariable GetCustomVariable(string customVariableName);

        CustomVariable GetCustomVariableRecursively(string customVariableName);

		string GetFulfillerName(BehaviorRequirement behaviorRequirement);

		bool ContainsBehavior(string behaviorName);

		bool ContainsCustomVariable(string variableName);

        List<CustomVariable> GetCustomVariablesToBeSetByDerived();
    }


}
