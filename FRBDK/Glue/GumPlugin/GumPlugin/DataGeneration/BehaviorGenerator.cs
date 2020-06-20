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
    public class GumStateCategory
    {
        public string Name;
        public string[] States;
    }

    public class FormsControlInfo
    {
        public string BehaviorName;
        public string InterfaceName;
        public string ControlName;
        public string ComponentFile;

        public List<GumStateCategory> GumStateCategory = new List<GumStateCategory>();


        public static FormsControlInfo Button = new FormsControlInfo
        {
            BehaviorName = "ButtonBehavior",
            ComponentFile = "Button",
            ControlName = "Button",
            GumStateCategory = new List<GumStateCategory>
            {
                new GumStateCategory
                {
                    Name = "ButtonCategory",
                    States = new[]
                    {
                        "Enabled",
                        "Disabled",
                        "Highlighted",
                        "Pushed",
                        "HighlightedFocused",
                        "Focused"
                    }
                }
            }
            
        };
        public static FormsControlInfo CheckBox = new FormsControlInfo
        {
            BehaviorName = "CheckBoxBehavior",
            ComponentFile = "CheckBox",
            ControlName = "CheckBox",
            GumStateCategory = new List<GumStateCategory>
            {
                new GumStateCategory
                {
                    Name = "CheckBoxCategory",
                    States = new[]
                    {
                        "EnabledOn",
                        "EnabledOff",
                        "DisabledOn",
                        "DisabledOff",

                        "HighlightedOn",
                        "HighlightedOff",
                        "PushedOn",
                        "PushedOff",
                    }
                }
            }
        };

        public static FormsControlInfo ColoredFrame = new FormsControlInfo
        {
            BehaviorName = null,
            ComponentFile = "ColoredFrame",
            ControlName = null,
            

        };

        public static FormsControlInfo ComboBox = new FormsControlInfo
        {
            BehaviorName = "ComboBoxBehavior",
            ComponentFile = "ComboBox",
            ControlName = "ComboBox",
            GumStateCategory = new List<GumStateCategory>
            {
                new GumStateCategory
                {
                    Name = "ComboBoxCategory",
                    States = new[]
                    {
                        "Enabled",
                        "Disabled",
                        "Highlighted",
                        "Pushed"
                    }
                }
            }
        };

        public static FormsControlInfo ListBox = new FormsControlInfo
        {
            BehaviorName = "ListBoxBehavior",
            ComponentFile = "ListBox",
            ControlName = "ListBox",
            // no category (yet?)
            //GumStateCategoryName = null,
        };

        public static FormsControlInfo ListBoxItem = new FormsControlInfo
        {
            BehaviorName = "ListBoxItemBehavior",
            ComponentFile = "ListBoxItem",
            ControlName = "ListBoxItem",
            GumStateCategory = new List<GumStateCategory>
            {
                new GumStateCategory
                {
                    Name = "ListBoxItemCategory",
                    States = new[]
                    {
                        "Enabled",
                        "Highlighted",
                        "Selected",
                    }
                }
            }
        };

        public static FormsControlInfo PasswordBox = new FormsControlInfo
        {
            BehaviorName = "PasswordBoxBehavior",
            ComponentFile = "PasswordBox",
            ControlName = "PasswordBox",
            GumStateCategory = new List<GumStateCategory>
            {
                new GumStateCategory
                {
                    Name = "PasswordBoxCategory",

                    States = new[]
                    {
                        "Enabled",
                        "Disabled",
                        "Highlighted",
                        "Selected"
                    }
                }
            }
        };

        public static FormsControlInfo RadioButton = new FormsControlInfo
        {
            BehaviorName = "RadioButtonBehavior",
            ControlName = "RadioButton",
            ComponentFile = "RadioButton",
            GumStateCategory = new List<GumStateCategory>
            {
                new GumStateCategory
                {
                    Name = "RadioButtonCategory",
                    States = new[]
                    {
                        "EnabledOn",
                        "EnabledOff",
                        "DisabledOn",
                        "DisabledOff",

                        "HighlightedOn",
                        "HighlightedOff",
                        "PushedOn",
                        "PushedOff"
                    }
                }
            }
        };


        public static FormsControlInfo ScrollBar = new FormsControlInfo
        {
            BehaviorName = "ScrollBarBehavior",
            ComponentFile = "ScrollBar",
            ControlName = "ScrollBar",
            GumStateCategory = new List<GumStateCategory>
            {
                new GumStateCategory
                {
                    Name = "ScrollBarCategory",
                }
            }
        };

        public static FormsControlInfo ScrollBarThumb = new FormsControlInfo
        {
            // only a gum component, no backing control:
            ComponentFile = "ScrollBarThumb"
        };

        public static FormsControlInfo ScrollViewer = new FormsControlInfo
        {
            BehaviorName = "ScrollViewerBehavior",
            ComponentFile = "ScrollViewer",
            ControlName = "ScrollViewer",
            // no categories needed (yet?)
            //GumStateCategoryName = null,
        };

        public static FormsControlInfo Slider = new FormsControlInfo
        {
            BehaviorName = "SliderBehavior",
            ComponentFile = "Slider",
            ControlName = "Slider",
            GumStateCategory = new List<GumStateCategory>
            {
                new GumStateCategory
                {
                    Name = "SliderCategory",
                }
            }
        };

        public static FormsControlInfo TextBox = new FormsControlInfo
        {
            BehaviorName = "TextBoxBehavior",
            ComponentFile = "TextBox",
            ControlName = "TextBox",
            GumStateCategory = new List<GumStateCategory>
            {
                new GumStateCategory
                {
                    Name = "TextBoxCategory",

                    States = new[]
                    {
                        "Enabled",
                        "Disabled",
                        "Highlighted",
                        "Selected"
                    }
                },
                new GumStateCategory
                {
                    Name = "LineModeCategory",

                    States = new []
                    {
                        "Single",
                        "Multi"
                    }

                }
            }
        };

        public static FormsControlInfo ToggleButton = new FormsControlInfo
        {
            BehaviorName = "ToggleBehavior",
            InterfaceName = "FlatRedBall.Gui.Controls.IToggle",
            ComponentFile = "ToggleButton",
            ControlName = "ToggleButton",
            GumStateCategory = new List<GumStateCategory>
            {
                new GumStateCategory
                {
                    Name = "ToggleCategory",
                    States = new[]
                    {
                        "EnabledOn",
                        "EnabledOff",
                        "DisabledOn",
                        "DisabledOff",

                        "HighlightedOn",
                        "HighlightedOff",
                        "PushedOn",
                        "PushedOff",
                    }
                }
            }
        };
        public static FormsControlInfo TreeView = new FormsControlInfo
        {
            BehaviorName = "TreeViewBehavior",
            ControlName = "TreeView",
            ComponentFile = "TreeView",
            // no categories
            //GumStateCategoryName = null,

        };

        public static FormsControlInfo TreeViewItem = new FormsControlInfo
        {
            BehaviorName = "TreeViewItemBehavior",
            ComponentFile = "TreeViewItem",
            ControlName = "TreeViewItem",
            // no categories, contained objects have categories
            //GumStateCategoryName = null,
        };

        public static FormsControlInfo TreeViewToggleButton = new FormsControlInfo
        {
            ComponentFile = "TreeViewToggleButton"
        };


        public static FormsControlInfo UserControl = new FormsControlInfo
        {
            BehaviorName = "UserControlBehavior",
            ComponentFile = "UserControl",
            ControlName = "UserControl",
            //GumStateCategoryName = null,
        };


        public static FormsControlInfo[] AllControls = new FormsControlInfo[]
        {
            Button,
            CheckBox,
            ColoredFrame,
            ComboBox,
            ListBox,
            ListBoxItem,
            PasswordBox,
            RadioButton,
            ScrollBar,
            ScrollBarThumb,
            ScrollViewer,
            Slider,
            TextBox,
            ToggleButton,
            TreeView,
            TreeViewItem,
            TreeViewToggleButton,
            UserControl
        };
        // Also look in the GueRuntimeTypeAssociationGenerator
    }

    public static class BehaviorGenerator
    {

        public static BehaviorSave CreateBehaviorSaveFrom(FormsControlInfo controlInfo)
        {
            BehaviorSave toReturn = new BehaviorSave();
            toReturn.Name = controlInfo.BehaviorName;

            foreach(var gumStateCategory in controlInfo.GumStateCategory)
            {
                var category = new StateSaveCategory();
                toReturn.Categories.Add(category);
                category.Name = gumStateCategory.Name;

                if (gumStateCategory.States != null)
                {
                    foreach (var stateName in gumStateCategory.States)
                    {
                        category.States.Add(new StateSave { Name = stateName });
                    }
                }
            }

            return toReturn;
        }

        public static BehaviorSave CreateRadioButtonBehavior()
        {
            var toReturn = CreateBehaviorSaveFrom(FormsControlInfo.RadioButton);

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
            var formsControl = FormsControlInfo.ScrollBar;
            BehaviorSave toReturn = CreateBehaviorSaveFrom(formsControl);

            BehaviorInstanceSave upButtonInstance = new BehaviorInstanceSave();
            upButtonInstance.Name = "UpButtonInstance";
            upButtonInstance.Behaviors.Add(new BehaviorReference { Name = FormsControlInfo.Button.BehaviorName });
            toReturn.RequiredInstances.Add(upButtonInstance);

            BehaviorInstanceSave downButtonInstance = new BehaviorInstanceSave();
            downButtonInstance.Name = "DownButtonInstance";
            downButtonInstance.Behaviors.Add(new BehaviorReference { Name = FormsControlInfo.Button.BehaviorName });
            toReturn.RequiredInstances.Add(downButtonInstance);

            BehaviorInstanceSave thumbInstance = new BehaviorInstanceSave();
            thumbInstance.Name = "ThumbInstance";
            thumbInstance.Behaviors.Add(new BehaviorReference { Name = FormsControlInfo.Button.BehaviorName });
            toReturn.RequiredInstances.Add(thumbInstance);

            return toReturn;
        }


        public static BehaviorSave CreateTextBoxBehavior()
        {
            var formsControl = FormsControlInfo.TextBox;

            BehaviorSave toReturn = CreateBehaviorSaveFrom(formsControl);

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

        public static BehaviorSave CreateSliderBehavior()
        {
            var formsControl = FormsControlInfo.Slider;
            BehaviorSave toReturn = CreateBehaviorSaveFrom(formsControl);

            BehaviorInstanceSave thumbInstance = new BehaviorInstanceSave();
            thumbInstance.Name = "ThumbInstance";
            thumbInstance.Behaviors.Add(new BehaviorReference { Name = FormsControlInfo.Button.BehaviorName });
            toReturn.RequiredInstances.Add(thumbInstance);

            return toReturn;
        }

        public static BehaviorSave CreateScrollViewerBehavior()
        {
            var formsControl = FormsControlInfo.ScrollViewer;
            BehaviorSave toReturn = CreateBehaviorSaveFrom(formsControl);

            BehaviorInstanceSave verticalScrollBarInstance = new BehaviorInstanceSave();
            verticalScrollBarInstance.Name = "VerticalScrollBarInstance";
            verticalScrollBarInstance.Behaviors.Add(new BehaviorReference { Name = FormsControlInfo.ScrollBar.BehaviorName });
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
            var formsControl = FormsControlInfo.ListBox;
            BehaviorSave toReturn = CreateBehaviorSaveFrom(formsControl);

            BehaviorInstanceSave verticalScrollBarInstance = new BehaviorInstanceSave();
            verticalScrollBarInstance.Name = "VerticalScrollBarInstance";
            verticalScrollBarInstance.Behaviors.Add(new BehaviorReference { Name = FormsControlInfo.ScrollBar.BehaviorName });
            toReturn.RequiredInstances.Add(verticalScrollBarInstance);

            BehaviorInstanceSave innerPanelInstance = new BehaviorInstanceSave();
            innerPanelInstance.Name = "InnerPanelInstance";
            toReturn.RequiredInstances.Add(innerPanelInstance);

            BehaviorInstanceSave clipContainerInstance = new BehaviorInstanceSave();
            clipContainerInstance.Name = "ClipContainerInstance";
            toReturn.RequiredInstances.Add(clipContainerInstance);

            return toReturn;
        }

        public static BehaviorSave CreateTreeViewItemBehavior()
        {
            var formsControl = FormsControlInfo.TreeViewItem;
            BehaviorSave toReturn = CreateBehaviorSaveFrom(formsControl);

            BehaviorInstanceSave innerPanelInstance = new BehaviorInstanceSave();
            innerPanelInstance.Name = "InnerPanelInstance";
            toReturn.RequiredInstances.Add(innerPanelInstance);

            return toReturn;

        }

        public static BehaviorSave CreateTreeViewBehavior()
        {
            var formsControl = FormsControlInfo.TreeView;
            BehaviorSave toReturn = CreateBehaviorSaveFrom(formsControl);

            BehaviorInstanceSave verticalScrollBarInstance = new BehaviorInstanceSave();
            verticalScrollBarInstance.Name = "VerticalScrollBarInstance";
            verticalScrollBarInstance.Behaviors.Add(new BehaviorReference { Name = FormsControlInfo.ScrollBar.BehaviorName });
            toReturn.RequiredInstances.Add(verticalScrollBarInstance);

            BehaviorInstanceSave innerPanelInstance = new BehaviorInstanceSave();
            innerPanelInstance.Name = "InnerPanelInstance";
            toReturn.RequiredInstances.Add(innerPanelInstance);

            BehaviorInstanceSave clipContainerInstance = new BehaviorInstanceSave();
            clipContainerInstance.Name = "ClipContainerInstance";
            toReturn.RequiredInstances.Add(clipContainerInstance);

            return toReturn;
        }


        public static BehaviorSave CreateComboBoxBehavior()
        {
            var formsControl = FormsControlInfo.ComboBox;
            BehaviorSave toReturn = CreateBehaviorSaveFrom(formsControl);

            BehaviorInstanceSave listBoxInstance = new BehaviorInstanceSave();
            listBoxInstance.Name = "ListBoxInstance";
            listBoxInstance.Behaviors.Add(new BehaviorReference { Name = FormsControlInfo.ListBox.BehaviorName });

            toReturn.RequiredInstances.Add(listBoxInstance);

            BehaviorInstanceSave textInstance = new BehaviorInstanceSave();
            textInstance.Name = "TextInstance";
            textInstance.BaseType = "Text";
            toReturn.RequiredInstances.Add(textInstance);

            return toReturn;
        }

    }
}
