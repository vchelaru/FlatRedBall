using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;

namespace EditorObjects.Gui
{
    public class SaveUseWindow : Window
    {
        #region Fields

        ComboBox mComboBox;
        Button mSaveButton;

        #endregion

        #region Events

        public event GuiMessage SaveOKClick;
        public event GuiMessage ItemSelect;

        #endregion


        public object SelectedObject
        {
            get
            {
                return mComboBox.SelectedObject;
            }
        }

        public SaveUseWindow(Cursor cursor)
            : base(cursor)
        {
            mComboBox = new ComboBox(cursor);
            mSaveButton = new Button(cursor);

            AddWindow(mComboBox);
            AddWindow(mSaveButton);

            ScaleX = 9;
            ScaleY = 3.5f;

            mComboBox.Y = 2;
            mSaveButton.Y = 4.5f;

            mSaveButton.Text = "Save";

            mComboBox.ScaleX = 8.5f;
            mSaveButton.ScaleX = mComboBox.ScaleX;

            mComboBox.X = .5f + mComboBox.ScaleX;
            mSaveButton.X = .5f + mSaveButton.ScaleX;            

            mSaveButton.Click += new GuiMessage(SaveButtonClick);
            mComboBox.ItemClick += new GuiMessage(ComboBoxItemClick);

        }

        public void AddItem(string displayText, object objectToAdd)
        {
            mComboBox.AddItem(displayText, objectToAdd);
        }

        void SaveButtonClick(Window callingWindow)
        {
            TextInputWindow tiw = GuiManager.ShowTextInputWindow("Enter a name:", "Enter Name");

            tiw.Click += SaveOKClick;
        }
        
        void ComboBoxItemClick(Window callingWindow)
        {
            if (ItemSelect != null)
            {
                ItemSelect(this);
            }
        }



    }
}
