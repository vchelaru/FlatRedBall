using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Instructions;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Graphics.Particle;
using System.Reflection;
using Color = System.Drawing.Color;
#if FRB_XNA
using Microsoft.Xna.Framework;
#else
using Vector3 = Microsoft.DirectX.Vector3;
#endif

namespace EditorObjects.Gui
{
    public class InstructionBlueprintPropertyGrid<TargetType> : PropertyGrid<InstructionBlueprint>
    {
        #region Fields
        private string mCurrentMember;
        private ComboBox mMemberBox;
        private UpDown mTimeUpdown;
        private bool mInitialized;

        private static Dictionary<Type, List<String>> mMemberFilter = new Dictionary<Type,List<string>>();
        #endregion

        #region Properties

        public UpDown TimeUpDown
        {
            get { return mTimeUpdown; }
            set { mTimeUpdown = value; }
        }

        public static Dictionary<Type, List<String>> MemberFilter
        {
            get { return mMemberFilter; }
        }

        #endregion

        #region Methods

        #region Events

        public void UpdateUpDownValue(Window callingWindow)
        {
            SelectedObject.MemberValue = (callingWindow as UpDown).CurrentValue;
        }

        public void UpdateComboBoxValue(Window callingWindow)
        {
            if (SelectedObject.MemberType.IsEnum)
            {
                SelectedObject.MemberValue = Enum.Parse(SelectedObject.MemberType, (callingWindow as ComboBox).Text, true);
            }

            else
            {
                SelectedObject.MemberValue = Boolean.Parse((callingWindow as ComboBox).Text);
            }
        }

        public void UpdateTextBoxValue(Window callingWindow)
        {
            if (SelectedObject.MemberType == typeof(long))
            {
                SelectedObject.MemberValue = long.Parse((callingWindow as TextBox).Text);
            }
            else
            {
                SelectedObject.MemberValue = (callingWindow as TextBox).Text;
            }
        }

        public void UpdateColorDisplayValue(Window callingWindow)
        {
            SelectedObject.MemberValue = (callingWindow as ColorDisplay).ColorValue;
        }

        public void UpdateVector3Display(Window callingWindow)
        {
            SelectedObject.MemberValue = (callingWindow as Vector3Display).Vector3Value;
        }

        #endregion

        #region Constructors
        public InstructionBlueprintPropertyGrid(Cursor cursor) :
            base(cursor)
        {
            ExcludeAllMembers();

            #region Member
            IncludeMember("MemberName");
            this.Name = "InstructionBlueprint";

            mMemberBox = new ComboBox(cursor);
            ReplaceMemberUIElement("MemberName", mMemberBox);
            mMemberBox.ScaleX = 12.0f;
            mMemberBox.SortingStyle = ListBoxBase.Sorting.AlphabeticalIncreasing;

            #region Populate with member names
            List<String> memberOptions = new List<String>();
            Type memberType;
            
            foreach(PropertyInfo propInfo in typeof(TargetType).GetProperties()){
                memberType = propInfo.PropertyType;
                if (memberType.IsEnum ||
                    memberType == typeof(string) ||
                    memberType == typeof(bool) ||
                    memberType == typeof(byte) ||
                    memberType == typeof(float) ||
                    memberType == typeof(double) ||
                    memberType == typeof(int) ||
                    memberType == typeof(long) ||
                    memberType == typeof(Color) ||
                    memberType == typeof(Vector3))
                {
                    memberOptions.Add(propInfo.Name);
                }

            }

            if (MemberFilter.ContainsKey(typeof(TargetType)))
            {
                List<String> filter = MemberFilter[typeof(TargetType)];

                if (filter != null)
                {
                    foreach (string s in filter)
                    {
                        memberOptions.Remove(s);
                    }
                }
            }

            SetOptionsForMember("MemberName", memberOptions);
            
            #endregion

            
            #endregion


            mCurrentMember = "<No Object>";
            mInitialized = false;
            AfterUpdateDisplayedProperties += new GuiMessage(AfterUpdate);

            IncludeMember("MemberValue");
            IncludeMember("Time");


            GetUIElementForMember("MemberValue").Visible = false;
            GetUIElementForMember("Time").Visible = false;
            UpdateDisplayedProperties();
        }
        #endregion

        #region Public Methods
        public void AfterUpdate(Window callingWindow)
        {
            if (mCurrentMember != null && 
                !String.IsNullOrEmpty(SelectedObject.MemberName) &&
                !mCurrentMember.Equals(SelectedObject.MemberName))
            {
                //Add properties to grid if this is the first change
                if (!mInitialized)
                {
                    GetUIElementForMember("MemberValue").Visible = true;
                    GetUIElementForMember("Time").Visible = true;

                    GetUIElementForMember("Time").ScaleX = 8;
                    mInitialized = true;

                    UpdateDisplayedProperties();
                }

                SelectedObject.MemberType = GetMemberType(typeof(TargetType), SelectedObject.MemberName); 
                mCurrentMember = SelectedObject.MemberName;

                Window replacement = CreateEditWindowForType(SelectedObject.MemberType);
                if (replacement != null)
                    ReplaceMemberUIElement("MemberValue", CreateEditWindowForType(SelectedObject.MemberType));
                else
                    ReplaceMemberUIElement("MemberValue", new Button(GuiManager.Cursor));

                if(SelectedObject.MemberValue != null && SelectedObject.MemberType != SelectedObject.MemberValue.GetType())
                    SelectedObject.MemberValue = null;
            }

            
          //  (GetUIElementForMember("Time") as UpDown).CurrentValue = (float)SelectedObject.MemberValue;
        }

        public void ExcludeMemberOption(String memberName)
        {

        }

        public void ExcludeMemberOptions(List<String> memberNames)
        {
            foreach (String s in memberNames)
            {
                ExcludeMemberOption(s);
            }
        }

        #endregion

        #region PrivateMethods
        private Window CreateEditWindowForType(Type memberType)
        {
            Window toReturn = null;

            #region ComboBox
            if (memberType.IsEnum ||
                memberType == typeof(bool)){

                ComboBox comboBox = new ComboBox(GuiManager.Cursor);
                comboBox.ScaleX = 8;
                toReturn = comboBox;
                comboBox.ItemClick += UpdateComboBoxValue;

                if(memberType == typeof(bool)){
                    comboBox.AddItem("true", true);
                    comboBox.AddItem("false", false);
                    
                }
                else
                {
                    string[] options = Enum.GetNames(memberType);
                    Array values = Enum.GetValues(memberType);
                    int i = 0;

                    foreach (object value in values)
                    {
                        comboBox.AddItem(options[i], value);

                        ++i;
                    }
                }
                }

            #endregion

            #region UpDown
            else if (
                memberType == typeof(byte) ||
                memberType == typeof(float) ||
                memberType == typeof(double) ||
                memberType == typeof(int))
            {
                UpDown upDown= new UpDown(GuiManager.Cursor);
                upDown.ValueChanged += new GuiMessage(UpdateUpDownValue);
                toReturn = upDown;
                upDown.ScaleX = 8;
            }
            #endregion

            #region Vector3
            else if (
                memberType == typeof(Vector3))
            {
                Vector3Display vecDisplay = new Vector3Display(GuiManager.Cursor);
                vecDisplay.LosingFocus += UpdateVector3Display;
                vecDisplay.ValueChanged += UpdateVector3Display;
                toReturn = vecDisplay;
            }
            #endregion

            #region ColorDisplay
            else if(
                memberType == typeof(System.Drawing.Color)){
                ColorDisplay cDisplay = new ColorDisplay(GuiManager.Cursor);

                cDisplay.ValueChanged += UpdateColorDisplayValue;
                cDisplay.LosingFocus += UpdateColorDisplayValue;

                toReturn = cDisplay;
                }
            #endregion

            #region Textbox
            else if(
                memberType == typeof(string) ||
                memberType == typeof(long)){
                TextBox textBox = new TextBox(GuiManager.Cursor);
                textBox.ScaleX = 8;

                if(memberType == typeof(long)){
                    textBox.Format = TextBox.FormatTypes.Integer;
                }

                textBox.LosingFocus += UpdateTextBoxValue;

                toReturn = textBox;
                }
            #endregion

            return toReturn;

        }

        private Type GetMemberType(Type objectType, string memberName)
        {
            PropertyInfo propertyInfo = objectType.GetProperty(memberName);

            //If this member is a property
            if (propertyInfo != null)
            {
                return propertyInfo.PropertyType;
            }

            else
            {
                return objectType.GetField(memberName).FieldType;
            }
        }


        #endregion

        #endregion

    }
}
