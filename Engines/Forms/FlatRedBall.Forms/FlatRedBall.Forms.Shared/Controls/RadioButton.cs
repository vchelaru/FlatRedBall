using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using Gum.Wireframe;
using RenderingLibrary.Graphics;

namespace FlatRedBall.Forms.Controls
{
    public class RadioButton : ToggleButton
    {
        #region Fields/Properties

        private GraphicalUiElement textComponent;

        private RenderingLibrary.Graphics.Text coreTextObject;

        //<radio button parent, <group name, radio button list>>
        public static Dictionary<object, Dictionary<string, List<RadioButton>>> RadioButtonDictionary = new Dictionary<object, Dictionary<string, List<RadioButton>>>();

        private static readonly object FakeRoot = new object();    //will act as fake root to enable root level radio buttons to be added to the dictionary

        private string _groupName;

        private object GetParent()
        {
            object parent;
            if (Visual == null)
                parent = null;
            else if (Visual.Parent != null)
                parent = Visual.Parent;
            else
                parent = FakeRoot;

            return parent;
        }

        public string GroupName
        {
            get => _groupName;
            set
            {
                RemoveFromDictionary();
                _groupName = value;
                AddToDictionary();
            }
        }

        private void RemoveFromDictionary()
        {
            var parent = GetParent();

            if (parent == null) //parent will be null when visual is null. groupname dictionary updates are meaningless until we have set the scope for the radio button!
                return;

            if (RadioButtonDictionary.ContainsKey(parent)
                && RadioButtonDictionary[parent].ContainsKey(GroupName)
                && RadioButtonDictionary[parent][GroupName].Contains(this))
            {
                RadioButtonDictionary[parent][GroupName].Remove(this);
            }
        }

        //Only use this on screen clean-up!!
        public static void ClearDictionary()
        {
            foreach (var parent in RadioButtonDictionary)
            {
                foreach (var child in parent.Value)
                {
                    child.Value.Clear();
                }
                parent.Value.Clear();
            }
            RadioButtonDictionary.Clear();
        }

        private void AddToDictionary()
        {
            var parent = GetParent();

            if (parent == null) //parent will be null when visual is null. groupname dictionary updates are meaningless until we have set the scope for the radio button!
                return;

            if (RadioButtonDictionary.ContainsKey(parent) == false)
                RadioButtonDictionary.Add(parent, new Dictionary<string, List<RadioButton>>());

            if (RadioButtonDictionary[parent].ContainsKey(GroupName) == false)
                RadioButtonDictionary[parent].Add(GroupName, new List<RadioButton>());
                
            RadioButtonDictionary[parent][GroupName].Add(this);
        }
        


        public string Text
        {
            get
            {
#if DEBUG
                ReportMissingTextInstance();
#endif
                return coreTextObject.RawText;
            }
            set
            {
#if DEBUG
                ReportMissingTextInstance();
#endif
                // go through the component instead of the core text object to force a layout refresh if necessary
                textComponent.SetProperty("Text", value);
            }
        }

        public override bool IsEnabled
        {
            get => base.IsEnabled;
            set
            {
                base.IsEnabled = value;
                if (!IsEnabled)
                {
                    // todo - to add focus eventually
                    //HasFocus = false;
                }
                UpdateState();
            }
        }

        #endregion

        #region Events

        public event EventHandler Click;

        #endregion

        #region Initialize Methods

        public RadioButton(string groupName = "")
        {
            GroupName = groupName;
            IsChecked = false;
        }

        /*
        This method will assign the group of the radio box according to the parent of the assigned Visual. 
        This method assumes that the visual is already attached to its parent
        and that the parent will not change after this method has been called. If these assumptions cause problems in the future we may have to revisit this
         */
        protected override void ReactToVisualChanged()
        {
            // text component is optional:
            textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance");

            Visual.Click += this.HandleClick;
            Visual.Push += this.HandlePush;
            Visual.LosePush += this.HandleLosePush;
            Visual.RollOn += this.HandleRollOn;
            Visual.RollOff += this.HandleRollOff;

            GroupName = GroupName; //this will force the dictionary to be updated for the current <group name, visual> pair

            base.ReactToVisualChanged();
        }

        #endregion

        #region Event Handler Methods

        private void HandleClick(IWindow window)
        {
            UpdateGroup();  //will call update state

            Click?.Invoke(this, null);
        }

        public void HandlePush(IWindow window)
        {
            UpdateState();
        }

        public void HandleLosePush(IWindow window)
        {
            UpdateState();
        }

        public void HandleRollOn(IWindow window)
        {
            UpdateState();
        }

        public void HandleRollOff(IWindow window)
        {
            UpdateState();
        }

        #endregion

        #region UpdateTo Methods

        private void UpdateGroup()
        {
            foreach (var radio in RadioButtonDictionary[GetParent()][GroupName])
            {
                if (radio != this)
                {
                    radio.IsChecked = false;
                }
            }

            IsChecked = true;
        }

        protected override void UpdateState()
        {
            if (Visual == null) //don't try to update the UI when the UI is not set yet, mmmmkay?
                return;

            var cursor = GuiManager.Cursor;

            if (IsChecked)
            {
                if (IsEnabled == false)
                {
                    Visual.SetProperty("RadioButtonCategoryState", "DisabledOn");
                }
                //else if (HasFocus)
                //{
                //    Visual.SetProperty("TextBoxCategoryState", "Selected");
                //}
                else if (Visual.HasCursorOver(cursor))
                {
                    if (cursor.WindowPushed == Visual && cursor.PrimaryDown)
                    {
                        Visual.SetProperty("RadioButtonCategoryState", "PushedOn");
                    }
                    else
                    {
                        Visual.SetProperty("RadioButtonCategoryState", "HighlightedOn");
                    }
                }
                else
                {
                    Visual.SetProperty("RadioButtonCategoryState", "EnabledOn");
                }
            }
            else
            {
                if (IsEnabled == false)
                {
                    Visual.SetProperty("RadioButtonCategoryState", "DisabledOff");
                }
                //else if (HasFocus)
                //{
                //    Visual.SetProperty("TextBoxCategoryState", "Selected");
                //}
                else if (Visual.HasCursorOver(cursor))
                {
                    if (cursor.WindowPushed == Visual && cursor.PrimaryDown)
                    {
                        Visual.SetProperty("RadioButtonCategoryState", "PushedOff");
                    }
                    else
                    {
                        Visual.SetProperty("RadioButtonCategoryState", "HighlightedOff");
                    }
                }
                else
                {
                    Visual.SetProperty("RadioButtonCategoryState", "EnabledOff");
                }
            }
            
        }


        #endregion

        #region Utilities

#if DEBUG
        private void ReportMissingTextInstance()
        {
            if (textComponent == null)
            {
                throw new Exception(
                    "This button was created with a Gum component that does not have an instance called 'text'. A 'text' instance must be added to modify the radio button's Text property.");
            }
        }
#endif

        #endregion

    }
}
