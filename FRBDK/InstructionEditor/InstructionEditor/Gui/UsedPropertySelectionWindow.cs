using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;

namespace InstructionEditor.Gui
{
    public class UsedPropertySelectionWindow : Window
    {
        #region Fields
        Button mOkButton;
        Button mCancelButton;

        ListBox mAllProperties;
        ListBox mPropertiesToSave;

        TextDisplay mAllPropertiesDisplay;
        TextDisplay mPropertiesToSaveDisplay;

        Button mUseAllButton;
        #endregion

        #region Properties

        public Button OkButton
        {
            get { return mOkButton; }
        }

        public override bool Visible
        {
            get
            {
                return base.Visible;
            }
            set
            {
                base.Visible = value;

                if (value)
                {
                    PopulateFromStringLists();
                }
            }
        }

        #endregion

        #region Delegate and Event Methods

        void AddSelectedProperty(Window callingWindow)
        {
            string selectedString = mAllProperties.GetHighlightedItem().Text;

            if (mPropertiesToSave.Contains(selectedString) == false)
            {
                mPropertiesToSave.AddItem(selectedString);
            }
        }

        void RemoveSelectedProperty(Window callingwindow)
        {
            mPropertiesToSave.RemoveItemByName(mPropertiesToSave.GetHighlightedItem().Text);
        }

        void SetChildrenWindowPositionsAndScales(Window callingWindow)
        {
            mAllProperties.ScaleX = (ScaleX - 1.5f) / 2.0f;
            mAllProperties.ScaleY = ScaleY - 3.9f;
            mAllProperties.X = mAllProperties.ScaleX + .7f;
            mAllProperties.Y = 1.7f + mAllProperties.ScaleY;

            mPropertiesToSave.ScaleX = mAllProperties.ScaleX;
            mPropertiesToSave.ScaleY = mAllProperties.ScaleY;
            mPropertiesToSave.X = (ScaleX * 2) - mPropertiesToSave.ScaleX - .7f;
            mPropertiesToSave.Y = mAllProperties.Y;

            mOkButton.Y = ScaleY * 2 - 1.5f;
            mCancelButton.Y = ScaleY * 2 - 1.5f;

            mCancelButton.X = ScaleX * 2 - .7f - mCancelButton.ScaleX;
            mOkButton.X = mCancelButton.X - mCancelButton.ScaleX - .5f - mOkButton.ScaleX;

            mAllPropertiesDisplay.X = .7f;
            mAllPropertiesDisplay.Y = 1;

            mPropertiesToSaveDisplay.X = mPropertiesToSave.X - mPropertiesToSave.ScaleX;
            mPropertiesToSaveDisplay.Y = 1;

            mUseAllButton.X = mAllProperties.X;
            mUseAllButton.ScaleX = mAllProperties.ScaleX;
            mUseAllButton.Y = this.ScaleY * 2 - 4.8f;
        }

        void UseAllClick(Window callingWindow)
        {
            for (int i = 0; i < mAllProperties.Count; i++)
            {
                string member = mAllProperties[i].Text;

                if (mPropertiesToSave.Contains(member) == false)
                {
                    mPropertiesToSave.AddItem(member);
                }
            }
        }

        void OkButtonClick(Window callingWindow)
        {
            EditorData.CurrentPositionedModelMembersWatching.Clear();
            EditorData.CurrentSpriteFrameMembersWatching.Clear();
            EditorData.CurrentSpriteMembersWatching.Clear();
            EditorData.CurrentTextMembersWatching.Clear();
            
            
            for(int i = 0; i < mPropertiesToSave.Count; i++)
            {
                if(EditorData.AllPositionedModelMembersWatching.Contains(mPropertiesToSave[i].Text))
                {
                    EditorData.CurrentPositionedModelMembersWatching.Add(mPropertiesToSave[i].Text);
                }

                if (EditorData.AllSpriteFrameMembersWatching.Contains(mPropertiesToSave[i].Text))
                {
                    EditorData.CurrentSpriteFrameMembersWatching.Add(mPropertiesToSave[i].Text);
                }

                if (EditorData.AllSpriteMembersWatching.Contains(mPropertiesToSave[i].Text))
                {
                    EditorData.CurrentSpriteMembersWatching.Add(mPropertiesToSave[i].Text);
                }

                if (EditorData.AllTextMembersWatching.Contains(mPropertiesToSave[i].Text))
                {
                    EditorData.CurrentTextMembersWatching.Add(mPropertiesToSave[i].Text);
                }
            }


            this.Visible = false;
        }

        void CancelButtonClick(Window callingWindow)
        {
            this.Visible = false;
        }

        #endregion

        #region Methods

        public UsedPropertySelectionWindow()
            : base(GuiManager.Cursor)
        {
            GuiManager.AddWindow(this);
            HasMoveBar = true;
            ScaleX = 18;
            ScaleY = 23;
            MinimumScaleX = 14;
            MinimumScaleY = 10;
            Resizable = true;
            Resizing += SetChildrenWindowPositionsAndScales;


            mOkButton = AddButton();
            mOkButton.Text = "Ok";
            mOkButton.ScaleX = 3;
            mOkButton.Click += OkButtonClick;

            mCancelButton = AddButton();
            mCancelButton.Text = "Cancel";
            mCancelButton.ScaleX = 3;
            mCancelButton.Click += CancelButtonClick;

            mAllProperties = AddListBox();
            mAllProperties.StrongSelect += AddSelectedProperty;

            mPropertiesToSave = AddListBox();
            mPropertiesToSave.StrongSelect += RemoveSelectedProperty;

            mAllPropertiesDisplay = AddTextDisplay();
            mAllPropertiesDisplay.Text = "All properties:";

            mPropertiesToSaveDisplay = AddTextDisplay();
            mPropertiesToSaveDisplay.Text = "Properties to save:";

            mUseAllButton = AddButton();
            mUseAllButton.Text = "Use All ->";
            mUseAllButton.ScaleX = 4;
            mUseAllButton.Click += UseAllClick;

            SetChildrenWindowPositionsAndScales(null);

            PopulateFromStringLists();

        }

        public void PopulateFromStringLists()
        {
            foreach (string member in EditorData.AllSpriteMembersWatching)
            {
                if (mAllProperties.Contains(member) == false)
                {
                    mAllProperties.AddItem(member);
                }
            }

            foreach (string member in EditorData.AllSpriteFrameMembersWatching)
            {
                if (mAllProperties.Contains(member) == false)
                {
                    mAllProperties.AddItem(member);
                }
            }

            foreach (string member in EditorData.AllPositionedModelMembersWatching)
            {
                if (mAllProperties.Contains(member) == false)
                {
                    mAllProperties.AddItem(member);
                }
            }

            foreach(string member in EditorData.AllTextMembersWatching)
            {
                if (mAllProperties.Contains(member) == false)
                {
                    mAllProperties.AddItem(member);
                }
            }

            mPropertiesToSave.Clear();


            foreach (string member in EditorData.CurrentSpriteMembersWatching)
            {
                if (mPropertiesToSave.Contains(member) == false)
                {
                    mPropertiesToSave.AddItem(member);
                }
            }

            foreach (string member in EditorData.CurrentSpriteFrameMembersWatching)
            {
                if (mPropertiesToSave.Contains(member) == false)
                {
                    mPropertiesToSave.AddItem(member);
                }
            }

            foreach (string member in EditorData.CurrentPositionedModelMembersWatching)
            {
                if (mPropertiesToSave.Contains(member) == false)
                {
                    mPropertiesToSave.AddItem(member);
                }
            }

            foreach (string member in EditorData.CurrentTextMembersWatching)
            {
                if (mPropertiesToSave.Contains(member) == false)
                {
                    mPropertiesToSave.AddItem(member);
                }
            }
        }

        #endregion
    }
}
