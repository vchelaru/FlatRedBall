using Gum.DataTypes;
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
        public const string ScrollBarBehaviorName = "ScrollBarBehavior";
        public const string ScrollViewerBehaviorName = "ScrollViewerBehavior";

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
            category.Name = "TextBoxCategory";

            category.States.Add(new StateSave { Name = "Enabled" });
            category.States.Add(new StateSave { Name = "Disabled" });
            category.States.Add(new StateSave { Name = "Highlighted" });
            category.States.Add(new StateSave { Name = "Selected" });

            // add the required instances:
            InstanceSave textInstance = new InstanceSave();
            textInstance.Name = "TextInstance";
            textInstance.BaseType = "Text";
            toReturn.RequiredInstances.Add(textInstance);

            InstanceSave caretInstance = new InstanceSave();
            caretInstance.Name = "CaretInstance";
            toReturn.RequiredInstances.Add(caretInstance);

            return toReturn;
        }

        public static BehaviorSave CreateScrollBarBehavior()
        {
            BehaviorSave toReturn = new BehaviorSave();
            toReturn.Name = ScrollBarBehaviorName;

            var category = new StateSaveCategory();
            toReturn.Categories.Add(category);
            category.Name = "ScrollBarCategory";

            InstanceSave upButtonInstance = new InstanceSave();
            upButtonInstance.Name = "UpButtonInstance";
            // todo - upButtonInstance needs to implement the Button behavior
            toReturn.RequiredInstances.Add(upButtonInstance);

            InstanceSave downButtonInstance = new InstanceSave();
            downButtonInstance.Name = "DownButtonInstance";
            // todo - downButtonInstance needs to implement the Button behavior
            toReturn.RequiredInstances.Add(downButtonInstance);

            InstanceSave thumbInstance = new InstanceSave();
            thumbInstance.Name = "ThumbInstance";
            // todo - thumbInstance needs to implement the Button behavior
            toReturn.RequiredInstances.Add(thumbInstance);

            return toReturn;
        }

        public static BehaviorSave CreateScrollViewerBehavior()
        {
            BehaviorSave toReturn = new BehaviorSave();
            toReturn.Name = ScrollViewerBehaviorName;

            // no categories needed yet

            InstanceSave verticalScrollBarInstance = new InstanceSave();
            verticalScrollBarInstance.Name = "VerticalScrollBarInstance";
            // todo - needs to implement the ScrollBar behavior
            toReturn.RequiredInstances.Add(verticalScrollBarInstance);

            InstanceSave innerPanelInstance = new InstanceSave();
            innerPanelInstance.Name = "InnerPanelInstance";
            // todo - needs to implement the ScrollBar behavior
            toReturn.RequiredInstances.Add(innerPanelInstance);

            InstanceSave clipContainerInstance = new InstanceSave();
            clipContainerInstance.Name = "ClipContainerInstance";
            // todo - needs to implement the ScrollBar behavior
            toReturn.RequiredInstances.Add(clipContainerInstance);

            return toReturn;
        }
    }
}
