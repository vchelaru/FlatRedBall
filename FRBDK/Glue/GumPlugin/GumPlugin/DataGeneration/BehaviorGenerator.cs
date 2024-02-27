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
    public class FormsControlInfo
    {
        public string BehaviorName;
        public string ControlName;

        static FormsControlInfo()
        {
            List<FormsControlInfo> tempList = new List<FormsControlInfo>();

            // ******************* STOP! ************************
            // Also look in the GueRuntimeTypeAssociationGenerator.GetIfIsCompleteFulfillment and add an entry
            // there if adding anything here.
            // **************************************************
            Add("ButtonBehavior", "Button");

            Add("CheckBoxBehavior", "CheckBox");
            Add("ComboBoxBehavior", "ComboBox");

            Add("DialogBoxBehavior", "FlatRedBall.Forms.Controls.Games.DialogBox");

            Add("InputDeviceSelector", "FlatRedBall.Forms.Controls.Games.InputDeviceSelector");
            Add("InputDeviceSelectionItem", "FlatRedBall.Forms.Controls.Games.InputDeviceSelectionItem");


            Add("LabelBehavior", "Label");

            Add("ListBoxBehavior", "ListBox");

            Add("ListBoxItemBehavior", "ListBoxItem");

            Add("OnScreenKeyboardBehavior", "FlatRedBall.Forms.Controls.Games.OnScreenKeyboard");

            Add("PasswordBoxBehavior", "PasswordBox");

            Add("PlayerJoinViewBehavior", "FlatRedBall.Forms.Controls.Games.PlayerJoinView");
            Add("PlayerJoinViewItemBehavior", "FlatRedBall.Forms.Controls.Games.PlayerJoinViewItem");

            Add("RadioButtonBehavior", "RadioButton");

            Add("ScrollBarBehavior", "ScrollBar");

            Add("ScrollViewerBehavior", "ScrollViewer");

            Add("SettingsViewBehavior", "FlatRedBall.Forms.Controls.Games.SettingsView");

            Add("SliderBehavior", "Slider");

            Add("TextBoxBehavior", "TextBox");

            Add("ToastBehavior", "FlatRedBall.Forms.Controls.Popups.Toast");

            Add("ToggleBehavior", "ToggleButton");

            Add("TreeViewBehavior", "TreeView");

            Add("TreeViewItemBehavior", "TreeViewItem");

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

            AllControls = tempList.ToArray();
        }
        public static FormsControlInfo[] AllControls;

    }
}
