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

        static FormsControlInfo()
        {
            Button = new FormsControlInfo
            {
                BehaviorName = "ButtonBehavior",
                ComponentFile = "Button",
                ControlName = "Button",
            };

            CheckBox = new FormsControlInfo
            {
                BehaviorName = "CheckBoxBehavior",
                ComponentFile = "CheckBox",
                ControlName = "CheckBox",
            };

            ColoredFrame = new FormsControlInfo
            {
                // Vic asks - why is this a null behavior? 
                // Oh, maybe because FormsControlInfo defines
                // all types of controls, and this one doesn't
                // have a dedicated behavior type.
                BehaviorName = null,
                ComponentFile = "ColoredFrame",
                ControlName = null,


            };

            ComboBox = new FormsControlInfo
            {
                BehaviorName = "ComboBoxBehavior",
                ComponentFile = "ComboBox",
                ControlName = "ComboBox",
            };

            DialogBox = new FormsControlInfo
            {
                BehaviorName = "DialogBoxBehavior",
                ComponentFile = "DialogBox",
                ControlName = "FlatRedBall.Forms.Controls.Games.DialogBox",
            };

            Label = new FormsControlInfo
            {
                BehaviorName = "LabelBehavior",
                ComponentFile = "Label",
                ControlName = "Label",
            };

            ListBox = new FormsControlInfo
            {
                BehaviorName = "ListBoxBehavior",
                ComponentFile = "ListBox",
                ControlName = "ListBox",
            };

            ListBoxItem = new FormsControlInfo
            {
                BehaviorName = "ListBoxItemBehavior",
                ComponentFile = "ListBoxItem",
                ControlName = "ListBoxItem",
            };

            OnScreenKeyboard = new FormsControlInfo
            {
                BehaviorName = "OnScreenKeyboardBehavior",
                ComponentFile = "Keyboard",
                ControlName = "FlatRedBall.Forms.Controls.Games.OnScreenKeyboard",
            };

            PasswordBox = new FormsControlInfo
            {
                BehaviorName = "PasswordBoxBehavior",
                ComponentFile = "PasswordBox",
                ControlName = "PasswordBox",
            };

            RadioButton = new FormsControlInfo
            {
                BehaviorName = "RadioButtonBehavior",
                ControlName = "RadioButton",
                ComponentFile = "RadioButton",
            };

            ScrollBar = new FormsControlInfo
            {
                BehaviorName = "ScrollBarBehavior",
                ComponentFile = "ScrollBar",
                ControlName = "ScrollBar",
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
            };

            Slider = new FormsControlInfo
            {
                BehaviorName = "SliderBehavior",
                ComponentFile = "Slider",
                ControlName = "Slider",
            };

            TextBox = new FormsControlInfo
            {
                BehaviorName = "TextBoxBehavior",
                ComponentFile = "TextBox",
                ControlName = "TextBox",
            };

            Toast = new FormsControlInfo
            {
                BehaviorName = "ToastBehavior",
                ComponentFile = "Toast",
                ControlName = "FlatRedBall.Forms.Controls.Popups.Toast",
            };

            ToggleButton = new FormsControlInfo
            {
                BehaviorName = "ToggleBehavior",
                InterfaceName = "FlatRedBall.Gui.Controls.IToggle",
                ComponentFile = "ToggleButton",
                ControlName = "ToggleButton",
            };

            TreeView = new FormsControlInfo
            {
                BehaviorName = "TreeViewBehavior",
                ControlName = "TreeView",
                ComponentFile = "TreeView",
            };

            TreeViewItem = new FormsControlInfo
            {
                BehaviorName = "TreeViewItemBehavior",
                ComponentFile = "TreeViewItem",
                ControlName = "TreeViewItem",
            };

            TreeViewToggleButton = new FormsControlInfo
            {
                ComponentFile = "TreeViewToggleButton",
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


    }
}
