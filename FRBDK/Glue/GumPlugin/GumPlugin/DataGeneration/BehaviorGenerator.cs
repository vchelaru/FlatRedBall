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
        public const string CheckBoxBehaviorName = "CheckBoxBehavior";
        public const string RadioButtonBehaviorName = "RadioButtonBehavior";
        public const string TextBoxBehaviorName = "TextBoxBehavior";
        public const string ScrollBarBehaviorName = "ScrollBarBehavior";
        public const string ScrollViewerBehaviorName = "ScrollViewerBehavior";
        public const string ListBoxItemBehaviorName = "ListBoxItemBehavior";
        public const string ListBoxBehaviorName = "ListBoxBehavior";
        public const string ComboBoxBehaviorName = "ComboBoxBehavior";
        public const string SliderBehaviorName = "SliderBehavior";
        public const string ToggleBehaviorName = "ToggleBehavior";
        public const string TreeViewBehaviorName = "TreeViewBehavior";
        public const string TreeViewItemBehaviorName = "TreeViewItemBehavior";
        public const string UserControlBehaviorName = "UserControlBehavior";
        // if adding here, search for the const string usage and the "Get" function to see
        // where to add additional code.
        // Also look in the GueRuntimeTypeAssociationGenerator


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
            BehaviorInstanceSave textInstance = new BehaviorInstanceSave();
            textInstance.Name = "TextInstance";
            textInstance.BaseType = "Text";
            toReturn.RequiredInstances.Add(textInstance);

            BehaviorInstanceSave caretInstance = new BehaviorInstanceSave();
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

            BehaviorInstanceSave upButtonInstance = new BehaviorInstanceSave();
            upButtonInstance.Name = "UpButtonInstance";
            upButtonInstance.Behaviors.Add(new BehaviorReference { Name = ButtonBehaviorName });
            toReturn.RequiredInstances.Add(upButtonInstance);

            BehaviorInstanceSave downButtonInstance = new BehaviorInstanceSave();
            downButtonInstance.Name = "DownButtonInstance";
            downButtonInstance.Behaviors.Add(new BehaviorReference { Name = ButtonBehaviorName });
            toReturn.RequiredInstances.Add(downButtonInstance);

            BehaviorInstanceSave thumbInstance = new BehaviorInstanceSave();
            thumbInstance.Name = "ThumbInstance";
            thumbInstance.Behaviors.Add(new BehaviorReference { Name = ButtonBehaviorName });
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

            BehaviorInstanceSave thumbInstance = new BehaviorInstanceSave();
            thumbInstance.Name = "ThumbInstance";
            thumbInstance.Behaviors.Add(new BehaviorReference { Name = ButtonBehaviorName });
            toReturn.RequiredInstances.Add(thumbInstance);

            return toReturn;
        }

        public static BehaviorSave CreateScrollViewerBehavior()
        {
            BehaviorSave toReturn = new BehaviorSave();
            toReturn.Name = ScrollViewerBehaviorName;

            // no categories needed yet

            BehaviorInstanceSave verticalScrollBarInstance = new BehaviorInstanceSave();
            verticalScrollBarInstance.Name = "VerticalScrollBarInstance";
            verticalScrollBarInstance.Behaviors.Add(new BehaviorReference { Name = ScrollBarBehaviorName });
            toReturn.RequiredInstances.Add(verticalScrollBarInstance);

            BehaviorInstanceSave innerPanelInstance = new BehaviorInstanceSave();
            innerPanelInstance.Name = "InnerPanelInstance";
            toReturn.RequiredInstances.Add(innerPanelInstance);

            BehaviorInstanceSave clipContainerInstance = new BehaviorInstanceSave();
            clipContainerInstance.Name = "ClipContainerInstance";
            toReturn.RequiredInstances.Add(clipContainerInstance);

            return toReturn;
        }

        public static BehaviorSave CreateListBoxBehavior()
        {
            BehaviorSave toReturn = new BehaviorSave();
            toReturn.Name = ListBoxBehaviorName;

            // no categories needed yet

            BehaviorInstanceSave verticalScrollBarInstance = new BehaviorInstanceSave();
            verticalScrollBarInstance.Name = "VerticalScrollBarInstance";
            verticalScrollBarInstance.Behaviors.Add(new BehaviorReference { Name = ScrollBarBehaviorName });
            toReturn.RequiredInstances.Add(verticalScrollBarInstance);

            BehaviorInstanceSave innerPanelInstance = new BehaviorInstanceSave();
            innerPanelInstance.Name = "InnerPanelInstance";
            toReturn.RequiredInstances.Add(innerPanelInstance);

            BehaviorInstanceSave clipContainerInstance = new BehaviorInstanceSave();
            clipContainerInstance.Name = "ClipContainerInstance";
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

        public static BehaviorSave CreateTreeViewItemBehavior()
        {
            BehaviorSave toReturn = new BehaviorSave();
            toReturn.Name = TreeViewItemBehaviorName;

            // no categories, the contained objects have categories

            BehaviorInstanceSave innerPanelInstance = new BehaviorInstanceSave();
            innerPanelInstance.Name = "InnerPanelInstance";
            toReturn.RequiredInstances.Add(innerPanelInstance);

            return toReturn;

        }

        public static BehaviorSave CreateTreeViewBehavior()
        {
            BehaviorSave toReturn = new BehaviorSave();
            toReturn.Name = TreeViewBehaviorName;

            // no categories needed yet

            BehaviorInstanceSave verticalScrollBarInstance = new BehaviorInstanceSave();
            verticalScrollBarInstance.Name = "VerticalScrollBarInstance";
            verticalScrollBarInstance.Behaviors.Add(new BehaviorReference { Name = ScrollBarBehaviorName });
            toReturn.RequiredInstances.Add(verticalScrollBarInstance);

            BehaviorInstanceSave innerPanelInstance = new BehaviorInstanceSave();
            innerPanelInstance.Name = "InnerPanelInstance";
            toReturn.RequiredInstances.Add(innerPanelInstance);

            BehaviorInstanceSave clipContainerInstance = new BehaviorInstanceSave();
            clipContainerInstance.Name = "ClipContainerInstance";
            toReturn.RequiredInstances.Add(clipContainerInstance);

            return toReturn;
        }

        public static BehaviorSave CreateUserControlBehavior()
        {
            BehaviorSave toReturn = new BehaviorSave();
            toReturn.Name = UserControlBehaviorName;

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

            BehaviorInstanceSave listBoxInstance = new BehaviorInstanceSave();
            listBoxInstance.Name = "ListBoxInstance";
            listBoxInstance.Behaviors.Add(new BehaviorReference { Name = ListBoxBehaviorName });

            toReturn.RequiredInstances.Add(listBoxInstance);

            BehaviorInstanceSave textInstance = new BehaviorInstanceSave();
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
