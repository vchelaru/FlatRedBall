using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Gui.PropertyGrids
{
    #region XML Docs
    /// <summary>
    /// A StructReferencePropertyGrid used by the FlatRedBall Engine when editing strings in a ListDisplayWindow.
    /// </summary>
    #endregion
    public class StringPropertyGrid : StructReferencePropertyGrid<string>
    {
        #region Fields

        TextBox mTextBox;
        FileTextBox mFileTextBox;
        ComboBox mComboBox;

        bool mCloseOnEnter = true;

        #endregion

        #region Properties

        public bool CloseOnEnter
        {
            get { return mCloseOnEnter; }
            set { mCloseOnEnter = value; }
        }

        public override string SelectedObject
        {
            get
            {
                return base.SelectedObject;
            }
            set
            {
                base.SetSelectedObjectNoLoseFocus(value);

                if (mTextBox != null)
                {
                    mTextBox.Text = value;
                }
                else if (mFileTextBox != null)
                {
                    mFileTextBox.Text = value;
                }
                else if (mComboBox != null)
                {
                    mComboBox.Text = value;
                }
            }
        }

        #endregion

        #region Event Methods

        private void ChangeString(Window callingWindow)
        {
            if (mTextBox != null)
            {
                SelectedObject = mTextBox.Text;
            }
            else if (mComboBox != null)
            {
                SelectedObject = mComboBox.Text;
            }
            else
            {
                SelectedObject = mFileTextBox.Text;
            }

            UpdateObject(null);
        }

        private void ChangeStringAndClose(Window callingWindow)
        {
            ChangeString(callingWindow);

            CloseWindow();
        }

        #endregion

        #region Methods

        public StringPropertyGrid(Cursor cursor, ListDisplayWindow windowOfObject, int indexOfObject)
            : base(cursor, windowOfObject, indexOfObject)
        {
            ExcludeAllMembers();

            mTextBox = new TextBox(mCursor);
            mTextBox.ScaleX = 15;

            this.AddWindow(mTextBox);

            mTextBox.LosingFocus += ChangeString;
            mTextBox.TextChange += ChangeString;
            mTextBox.EnterPressed += ChangeStringAndClose;
        }

        public void SetOptions(IList<string> availableOptions)
        {
            SetOptions(availableOptions, ListBoxBase.Sorting.AlphabeticalIncreasing);
        }

        public void SetOptions(IList<string> availableOptions, FlatRedBall.Gui.ListBoxBase.Sorting sortingStyle)
        {
            if (mTextBox != null)
            {
                RemoveWindow(mTextBox);
                mTextBox = null;
            }

            if (mComboBox == null)
            {
                mComboBox = new ComboBox(mCursor);
                mComboBox.SortingStyle = sortingStyle;
                mComboBox.ScaleX = 8;
                this.AddWindow(mComboBox);
                mComboBox.ItemClick += ChangeString;
            }
            else
            {
                mComboBox.Clear();
            }
            foreach (string option in availableOptions)
            {
                mComboBox.AddItem(option);
            }

        }

        public void SetToFileString()
        {
            if (mTextBox != null)
            {
                RemoveWindow(mTextBox);
                mTextBox = null;
            }


            mFileTextBox = new FileTextBox(mCursor);
            mFileTextBox.ScaleX = 15;
            this.AddWindow(mFileTextBox);
            mFileTextBox.Text = SelectedObject;
            mFileTextBox.LosingFocus += ChangeString;
            mFileTextBox.FileSelect += ChangeString;
        }

        #endregion
    }
}
