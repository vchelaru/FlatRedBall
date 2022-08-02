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
        public string ControlName;

        static FormsControlInfo()
        {
            List<FormsControlInfo> tempList = new List<FormsControlInfo>();
            Add("ButtonBehavior", "Button");

            Add("CheckBoxBehavior", "CheckBox");
            Add("ComboBoxBehavior", "ComboBox");

            Add("DialogBoxBehavior", "FlatRedBall.Forms.Controls.Games.DialogBox");

            Add("LabelBehavior", "Label");

            Add("ListBoxBehavior", "ListBox");

            Add("")
            ListBoxItem = new FormsControlInfo
            {
                BehaviorName = "ListBoxItemBehavior",
                ControlName = "ListBoxItem",
            };

            OnScreenKeyboard = new FormsControlInfo
            {
                BehaviorName = "OnScreenKeyboardBehavior",
                ControlName = "FlatRedBall.Forms.Controls.Games.OnScreenKeyboard",
            };

            PasswordBox = new FormsControlInfo
            {
                BehaviorName = "PasswordBoxBehavior",
                ControlName = "PasswordBox",
            };

            RadioButton = new FormsControlInfo
            {
                BehaviorName = "RadioButtonBehavior",
                ControlName = "RadioButton",
            };

            ScrollBar = new FormsControlInfo
            {
                BehaviorName = "ScrollBarBehavior",
                ControlName = "ScrollBar",
            };

            ScrollViewer = new FormsControlInfo
            {
                BehaviorName = "ScrollViewerBehavior",
                ControlName = "ScrollViewer",
            };

            Slider = new FormsControlInfo
            {
                BehaviorName = "SliderBehavior",
                ControlName = "Slider",
            };

            TextBox = new FormsControlInfo
            {
                BehaviorName = "TextBoxBehavior",
                ControlName = "TextBox",
            };

            Toast = new FormsControlInfo
            {
                BehaviorName = "ToastBehavior",
                ControlName = "FlatRedBall.Forms.Controls.Popups.Toast",
            };

            ToggleButton = new FormsControlInfo
            {
                BehaviorName = "ToggleBehavior",
                ControlName = "ToggleButton",
            };

            TreeView = new FormsControlInfo
            {
                BehaviorName = "TreeViewBehavior",
                ControlName = "TreeView",
            };

            TreeViewItem = new FormsControlInfo
            {
                BehaviorName = "TreeViewItemBehavior",
                ControlName = "TreeViewItem",
            };


            Add("UserControlBehavior", "UserControl");

            void Add(string behaviorName, string controlName)
            {
                var formsControlInfo = new FormsControlInfo
                {
                    BehaviorName = behaviorName,
                    ControlName = controlName
                };
                tempList.Add(formsControlInfo);
            }

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
