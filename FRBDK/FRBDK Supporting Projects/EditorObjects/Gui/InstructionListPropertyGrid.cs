using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Instructions;
using FlatRedBall.Input;

namespace EditorObjects.Gui
{
    public class InstructionListPropertyGrid : PropertyGrid<InstructionList>
    {
        #region Fields

        ListDisplayWindow mListDisplayWindow;

        UpDown mSetAllInstructionTimeUpDown;

        Button mOverwriteInstructionListButton;

        #endregion

        #region Properties

        public ListDisplayWindow ListDisplayWindow
        {
            get
            {
                return mListDisplayWindow;
            }

        }

        public override InstructionList SelectedObject
        {
            get
            {
                return base.SelectedObject;
            }
            set
            {
                base.SelectedObject = value;

                Visible = (value != null);

                if (mListDisplayWindow != null)
                {
                    // This protects the extra UI from throwing an exception if this property is set
                    // before the UI is created.
                    mSetAllInstructionTimeUpDown.Enabled = (value != null);
                    mListDisplayWindow.ListShowing = value;
                }
            }
        }

        #endregion

        #region Events

        public event GuiMessage OverwriteInstructionList;

        #endregion

        #region Event Methods

        private void OverwriteInstructionListClick(Window callingWindow)
        {
            if (OverwriteInstructionList != null)
            {
                OverwriteInstructionList(this);
            }
        }

        private void SetAllInstructionTime(Window callingWindow)
        {
            double time = ((UpDown)callingWindow).CurrentValue;

            foreach (Instruction instruction in SelectedObject)
            {
                instruction.TimeToExecute = time;
            }

        }

        public bool ShowInstructionPropertyGridOnStrongSelect
        {
            get { return mListDisplayWindow.ShowPropertyGridOnStrongSelect; }
            set { mListDisplayWindow.ShowPropertyGridOnStrongSelect = value; }
        }

        #endregion

        #region Methods

        public InstructionListPropertyGrid(Cursor cursor)
            : base(GuiManager.Cursor)
        {
            ExcludeMember("Capacity");
            ExcludeMember("Item");

            CreateExtraWindows();

        }

        public override void UpdateDisplayedProperties()
        {
            base.UpdateDisplayedProperties();

            if (mListDisplayWindow != null)
            {
                mListDisplayWindow.UpdateToList();
            }

            if (SelectedObject != null && SelectedObject.Count != 0 && 
                InputManager.ReceivingInput != mSetAllInstructionTimeUpDown.textBox)
            {
                mSetAllInstructionTimeUpDown.CurrentValue = (float)SelectedObject[0].TimeToExecute;
            }
        }

        private void CreateExtraWindows()
        {
            #region Create the ListDisplayWindow to show all instructions
            mListDisplayWindow = new ListDisplayWindow(GuiManager.Cursor);

            mListDisplayWindow.ScaleX = 11;
            mListDisplayWindow.ScaleY = 9;
            mListDisplayWindow.DrawOuterWindow = false;

            this.AddWindow(mListDisplayWindow);
            #endregion

            #region Create the Up/Down to simultaneously set the time for all Instructions

            mSetAllInstructionTimeUpDown = new UpDown(GuiManager.Cursor);
            mSetAllInstructionTimeUpDown.MinValue = 0;
            mSetAllInstructionTimeUpDown.ScaleX = 7;
            mSetAllInstructionTimeUpDown.Enabled = false;
            this.AddWindow(mSetAllInstructionTimeUpDown);
            this.SetLabelForWindow(mSetAllInstructionTimeUpDown, "Time:");

            mSetAllInstructionTimeUpDown.ValueChanged += SetAllInstructionTime;

            #endregion

            #region Create the mRecordingToggleButton

            mOverwriteInstructionListButton = new Button(mCursor);
            mOverwriteInstructionListButton.Text = "Overwrite\nInstructionList";
            mOverwriteInstructionListButton.ScaleX = 7;
            mOverwriteInstructionListButton.ScaleY = 2f;

            mOverwriteInstructionListButton.Click += OverwriteInstructionListClick;
            AddWindow(mOverwriteInstructionListButton);
            #endregion

        }

        #endregion
    }
}
