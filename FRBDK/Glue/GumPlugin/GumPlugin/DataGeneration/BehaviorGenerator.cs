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
        public const string RadioButtonBehaviorName = "RadioButtonBehavior";
        public const string TextBoxBehaviorName = "TextBoxBehavior";
        public const string ScrollBarBehaviorName = "ScrollBarBehavior";
        public const string ScrollViewerBehaviorName = "ScrollViewerBehavior";
        public const string ListBoxItemBehaviorName = "ListBoxItemBehavior";
        public const string ListBoxBehaviorName = "ListBoxBehavior";
        public const string ComboBoxBehaviorName = "ComboBoxBehavior";
        public const string SliderBehaviorName = "SliderBehavior";
        public const string CheckBoxBehaviorName = "CheckBoxBehavior";
        // if adding here, search for the const string usage and the "Get" function to see
        // where to add additional code
        

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

        public static BehaviorSave CreateRadioButtonBehavior()
        {
            BehaviorSave toReturn = new BehaviorSave();
            toReturn.Name = RadioButtonBehaviorName;

            var category = new StateSaveCategory();
            toReturn.Categories.Add(category);
            category.Name = "RadioButtonCategory";

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

        public static BehaviorSave CreateSliderBehavior()
        {
            BehaviorSave toReturn = new BehaviorSave();
            toReturn.Name = SliderBehaviorName;

            var category = new StateSaveCategory();
            toReturn.Categories.Add(category);
            category.Name = "SliderCategory";
            
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
            toReturn.RequiredInstances.Add(innerPanelInstance);

            InstanceSave clipContainerInstance = new InstanceSave();
            clipContainerInstance.Name = "ClipContainerInstance";
            toReturn.RequiredInstances.Add(clipContainerInstance);

            return toReturn;
        }

        public static BehaviorSave CreateListBoxBehavior()
        {
            BehaviorSave toReturn = new BehaviorSave();
            toReturn.Name = ListBoxBehaviorName;

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
        
        public static BehaviorSave CreateListBoxItemBehavior()
        {
            BehaviorSave toReturn = new BehaviorSave();
            toReturn.Name = ListBoxItemBehaviorName;

            var category = new StateSaveCategory();
            toReturn.Categories.Add(category);
            category.Name = "ListBoxItemCategory";

            category.States.Add(new StateSave { Name = "Enabled" });
            category.States.Add(new StateSave { Name = "Highlighted" });
            category.States.Add(new StateSave { Name = "Selected" });

            return toReturn;
        }

        public static BehaviorSave CreateComboBoxBehavior()
        {
            BehaviorSave toReturn = new BehaviorSave();
            toReturn.Name = ComboBoxBehaviorName;

            var category = new StateSaveCategory();
            toReturn.Categories.Add(category);
            category.Name = "ComboBoxCategory";

            category.States.Add(new StateSave { Name = "Enabled" });
            category.States.Add(new StateSave { Name = "Disabled" });
            category.States.Add(new StateSave { Name = "Highlighted" });
            category.States.Add(new StateSave { Name = "Pushed" });

            InstanceSave listBoxInstance = new InstanceSave();
            listBoxInstance.Name = "ListBoxInstance";
            // todo - needs to implement the ListBox behavior
            toReturn.RequiredInstances.Add(listBoxInstance);

            InstanceSave textInstance = new InstanceSave();
            textInstance.Name = "TextInstance";
            textInstance.BaseType = "Text";
            toReturn.RequiredInstances.Add(textInstance);

            return toReturn;
        }

        public static BehaviorSave CreateCheckBoxBehavior()
        {
            BehaviorSave toReturn = new BehaviorSave();
            toReturn.Name = CheckBoxBehaviorName;

            var category = new StateSaveCategory();
            toReturn.Categories.Add(category);
            category.Name = "CheckBoxCategory";

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

    }
}
