using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumPlugin.DataGeneration
{
    public static class BehaviorGenerator
    {
        public const string ButtonBehaviorName = "ButtonBehavior";

        public static BehaviorSave CreateButtonBehavior()
        {
            BehaviorSave toReturn = new BehaviorSave();
            toReturn.Name = ButtonBehaviorName;

            var category = new StateSaveCategory();
            category.Name = "ButtonCategory";

            category.States.Add(new StateSave { Name = "Enabled" });
            category.States.Add(new StateSave { Name = "Disabled" });
            category.States.Add(new StateSave { Name = "Highlighted" });
            category.States.Add(new StateSave { Name = "Pushed" });

            return toReturn;
        }
    }
}
