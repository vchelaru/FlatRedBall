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
        public const string ToggleBehaviorName = "ToggleBehavior";
        public const string TextBoxBehaviorName = "TextBoxBehavior";

        public static BehaviorSave CreateButtonBehavior()
        {
            BehaviorSave toReturn = new BehaviorSave();
            toReturn.Name = ButtonBehaviorName;

            var category = new StateSaveCategory();
            toReturn.Categories.Add(category);
            category.Name = "ButtonCategory";

            category.States.Add(new StateSave { Name = "Enabled" });
            category.States.Add(new StateSave { Name = "Disabled" });
            category.States.Add(new StateSave { Name = "Highlighted" });
            category.States.Add(new StateSave { Name = "Pushed" });

            return toReturn;
        }

        public static BehaviorSave CreateToggleBehavior()
        {
            BehaviorSave toReturn = new BehaviorSave();
            toReturn.Name = ToggleBehaviorName;

            var category = new StateSaveCategory();
            toReturn.Categories.Add(category);
            category.Name = "ToggleCategory";

            category.States.Add(new StateSave { Name = "EnabledOn" });
            category.States.Add(new StateSave { Name = "EnabledOff" });
            category.States.Add(new StateSave { Name = "DisabledOn" });
            category.States.Add(new StateSave { Name = "DisabledOff" });


            category.States.Add(new StateSave { Name = "HighlightedOn" });
            category.States.Add(new StateSave { Name = "HighlightedOff" });
            category.States.Add(new StateSave { Name = "PushedOn" });
            category.States.Add(new StateSave { Name = "PushedOff" });

            return toReturn;

        }

        public static BehaviorSave CreateTextBoxBehavior()
        {
            BehaviorSave toReturn = new BehaviorSave();
            toReturn.Name = TextBoxBehaviorName;

            var category = new StateSaveCategory();
            toReturn.Categories.Add(category);
            category.Name = "ToggleCategory";

            category.States.Add(new StateSave { Name = "Enabled" });
            category.States.Add(new StateSave { Name = "Disabled" });
            category.States.Add(new StateSave { Name = "Highlighted" });
            category.States.Add(new StateSave { Name = "Selected" });

            return toReturn;
        }
    }
}
