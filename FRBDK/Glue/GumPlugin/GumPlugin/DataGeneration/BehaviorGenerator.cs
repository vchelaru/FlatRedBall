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

        public List<BehaviorInstanceSave> RequiredInstances = new List<BehaviorInstanceSave>();

        static FormsControlInfo()
        {
            Button = new FormsControlInfo
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

            CheckBox = new FormsControlInfo
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

            ColoredFrame = new FormsControlInfo
            {
                BehaviorName = null,
                ComponentFile = "ColoredFrame",
                ControlName = null,


            };

            ComboBox = new FormsControlInfo
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
                },

                RequiredInstances = new List<BehaviorInstanceSave>()
                {
                    new BehaviorInstanceSave()
                    {
                        Name = "ListBoxInstance",
                        Behaviors = new List<BehaviorReference>
                        {
                            new BehaviorReference { Name = "ListBoxBehavior" }
                        }
                    },

                    new BehaviorInstanceSave()
                    {
                        Name = "TextInstance",
                        BaseType = "Text"
                    }
                }
            };

            DialogBox = new FormsControlInfo
            {
                BehaviorName = "DialogBoxBehavior",
                ComponentFile = "DialogBox",
                ControlName = "FlatRedBall.Forms.Controls.Games.DialogBox"
            };

            Label = new FormsControlInfo
            {
                BehaviorName = "LabelBehavior",
                ComponentFile = "Label",
                ControlName = "Label",
                // no GumStateCategory
                RequiredInstances = new List<BehaviorInstanceSave>
                {
                    new BehaviorInstanceSave
                    {
                        Name = "TextInstance",
                        BaseType = "Text"

                    }
                }
            };

            ListBox = new FormsControlInfo
            {
                BehaviorName = "ListBoxBehavior",
                ComponentFile = "ListBox",
                ControlName = "ListBox",
                GumStateCategory = new List<GumStateCategory>
                {
                    new GumStateCategory
                    {
                        Name = "ListBoxCategory",
                        States = new []
                        {
                            "Enabled",
                            "Disabled",
                            "Focused"
                        }
                    }
                },
                // no category (yet?)
                //GumStateCategoryName = null,

                RequiredInstances = new List<BehaviorInstanceSave>
                {
                    new BehaviorInstanceSave()
                    {
                        Name = "VerticalScrollBarInstance",
                        Behaviors = new List<BehaviorReference>
                        {
                            new BehaviorReference { Name = "ScrollBarBehavior"}
                        }
                    },

                    new BehaviorInstanceSave()
                    {
                        Name = "InnerPanelInstance"
                    },

                    new BehaviorInstanceSave()
                    {
                        Name = "ClipContainerInstance"
                    }
                }
            };

            ListBoxItem = new FormsControlInfo
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

            OnScreenKeyboard = new FormsControlInfo
            {
                BehaviorName = "OnScreenKeyboardBehavior",
                ComponentFile = "Keyboard",
                ControlName = "FlatRedBall.Forms.Controls.Games.OnScreenKeyboard"
            };

            PasswordBox = new FormsControlInfo
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

            RadioButton = new FormsControlInfo
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
                },
                RequiredInstances = new List<BehaviorInstanceSave>
                {
                    new BehaviorInstanceSave
                    {
                        Name = "TextInstance",
                        BaseType = "Text"

                    },
                    // Vic asks - why do we have this? Was this a copy/paste issue from TextBox?
                    new BehaviorInstanceSave
                    {
                        Name = "CaretInstance"
                    }
                }
            };

            ScrollBar = new FormsControlInfo
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
                },

                RequiredInstances = new List<BehaviorInstanceSave>
                {
                    new BehaviorInstanceSave()
                    {
                        Name = "UpButtonInstance",
                        Behaviors = new List<BehaviorReference>
                        {
                            new BehaviorReference { Name = FormsControlInfo.Button.BehaviorName }
                        }
                    },

                    new BehaviorInstanceSave()
                    {
                        Name = "DownButtonInstance",
                        Behaviors = new List<BehaviorReference>
                        {
                            new BehaviorReference { Name = FormsControlInfo.Button.BehaviorName }
                        }
                    },

                    new BehaviorInstanceSave()
                    {
                        Name = "ThumbInstance",
                        Behaviors = new List<BehaviorReference>
                        {
                            new BehaviorReference { Name = FormsControlInfo.Button.BehaviorName }
                        }
                    },
                }
            };

            ScrollBarThumb = new FormsControlInfo
            {
                // only a gum component, no backing control:
                ComponentFile = "ScrollBarThumb"
            };

            ScrollViewer = new FormsControlInfo
            {
                BehaviorName = "ScrollViewerBehavior",
                ComponentFile = "ScrollViewer",
                ControlName = "ScrollViewer",
                // no categories needed (yet?)
                //GumStateCategoryName = null,

                RequiredInstances = new List<BehaviorInstanceSave>
                {
                    new BehaviorInstanceSave
                    {
                        Name = "VerticalScrollBarInstance",
                        Behaviors = new List<BehaviorReference>
                        {
                            new BehaviorReference { Name = FormsControlInfo.ScrollBar.BehaviorName }
                        }

                    },

                    new BehaviorInstanceSave
                    {
                        Name = "InnerPanelInstance"
                    },

                    new BehaviorInstanceSave
                    {
                        Name = "ClipContainerInstance"
                    }
                }
            };

            Slider = new FormsControlInfo
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
                },

                RequiredInstances = new List<BehaviorInstanceSave>
                {
                    new BehaviorInstanceSave
                    {
                        Name = "ThumbInstance",
                        Behaviors = new List<BehaviorReference>
                        {
                            new BehaviorReference { Name = FormsControlInfo.Button.BehaviorName }
                        }
                    }
                }
            };

            TextBox = new FormsControlInfo
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
                },

                RequiredInstances = new List<BehaviorInstanceSave>
                {
                    new BehaviorInstanceSave()
                    {
                        Name = "TextInstance",
                        BaseType = "Text"
                    },

                    new BehaviorInstanceSave()
                    {
                        Name = "CaretInstance"
                    }
                }

            };

            Toast = new FormsControlInfo
            {
                BehaviorName = "ToastBehavior",
                ComponentFile = "Toast",
                ControlName = "FlatRedBall.Forms.Controls.Popups.Toast",
                // no GumStateCategory
                RequiredInstances = new List<BehaviorInstanceSave>
                {
                    new BehaviorInstanceSave
                    {
                        Name = "TextInstance",
                        BaseType = "Text"

                    }
                }
            };

            ToggleButton = new FormsControlInfo
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

            TreeView = new FormsControlInfo
            {
                BehaviorName = "TreeViewBehavior",
                ControlName = "TreeView",
                ComponentFile = "TreeView",
                // no categories
                //GumStateCategoryName = null,

                RequiredInstances = new List<BehaviorInstanceSave>
                {
                    new BehaviorInstanceSave
                    {
                        Name = "VerticalScrollBarInstance",
                        Behaviors = new List<BehaviorReference>
                        {
                            new BehaviorReference { Name = FormsControlInfo.ScrollBar.BehaviorName }
                        }
                    },

                    new BehaviorInstanceSave
                    {
                        Name = "InnerPanelInstance"
                    },

                    new BehaviorInstanceSave
                    {
                        Name = "ClipContainerInstance"
                    }
                }
            };

            TreeViewItem = new FormsControlInfo
            {
                BehaviorName = "TreeViewItemBehavior",
                ComponentFile = "TreeViewItem",
                ControlName = "TreeViewItem",
                // no categories, contained objects have categories
                //GumStateCategoryName = null,

                RequiredInstances = new List<BehaviorInstanceSave>
                {
                    new BehaviorInstanceSave
                    {
                        Name = "InnerPanelInstance"
                    }
                }
            };

            TreeViewToggleButton = new FormsControlInfo
            {
                ComponentFile = "TreeViewToggleButton"
            };

            UserControl = new FormsControlInfo
            {
                BehaviorName = "UserControlBehavior",
                ComponentFile = "UserControl",
                ControlName = "UserControl",
                //GumStateCategoryName = null,
            };

            AllControls = new FormsControlInfo[]
            {
                Button,
                CheckBox,
                ColoredFrame,
                ComboBox,
                DialogBox,
                Label,
                ListBox,
                ListBoxItem,
                OnScreenKeyboard,
                PasswordBox,
                RadioButton,
                ScrollBar,
                ScrollBarThumb,
                ScrollViewer,
                Slider,
                TextBox,
                Toast,
                ToggleButton,
                TreeView,
                TreeViewItem,
                TreeViewToggleButton,
                UserControl
            };
        // Also look in the GueRuntimeTypeAssociationGenerator
        }

        public static FormsControlInfo Button;

        public static FormsControlInfo CheckBox;

        public static FormsControlInfo ColoredFrame;

        public static FormsControlInfo ComboBox;

        public static FormsControlInfo DialogBox;

        public static FormsControlInfo Label;

        public static FormsControlInfo ListBox;

        public static FormsControlInfo ListBoxItem;

        public static FormsControlInfo OnScreenKeyboard;

        public static FormsControlInfo PasswordBox;

        public static FormsControlInfo RadioButton;

        public static FormsControlInfo ScrollBar;

        public static FormsControlInfo ScrollBarThumb;

        public static FormsControlInfo ScrollViewer;

        public static FormsControlInfo Slider;

        public static FormsControlInfo TextBox;

        public static FormsControlInfo Toast;

        public static FormsControlInfo ToggleButton;

        public static FormsControlInfo TreeView;

        public static FormsControlInfo TreeViewItem;

        public static FormsControlInfo TreeViewToggleButton;

        public static FormsControlInfo UserControl;

        public static FormsControlInfo[] AllControls;

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

            toReturn.RequiredInstances.AddRange(controlInfo.RequiredInstances);

            return toReturn;
        }
    }
}
