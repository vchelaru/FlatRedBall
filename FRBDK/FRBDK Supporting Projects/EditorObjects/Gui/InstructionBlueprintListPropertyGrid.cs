using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Instructions;
using FlatRedBall.Input;
using FlatRedBall;

namespace EditorObjects.Gui
{
    public class InstructionBlueprintListPropertyGrid : PropertyGrid<InstructionBlueprintList>
    {

        #region Fields

        ListDisplayWindow mListDisplayWindow;

        UpDown mSetSelectedInstructionTimeUpDown;

        #endregion


        #region Properties

        public ListDisplayWindow ListDisplayWindow
        {
            get
            {
                return mListDisplayWindow;
            }

        }

        public override InstructionBlueprintList SelectedObject
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
                    mSetSelectedInstructionTimeUpDown.Enabled = (value != null);
                    mListDisplayWindow.ListShowing = value;
                    mListDisplayWindow.AllowShiftClick = true;
                    mListDisplayWindow.AllowCtrlClick = true;
                    mListDisplayWindow.AllowReordering = false;
                    
                }
            }
        }


        #endregion

        #region Events


        private void SetSelectedInstructionTime(Window callingWindow)
        {
            double time = ((UpDown)callingWindow).CurrentValue;
            List<object> selected = mListDisplayWindow.ListBox.GetHighlightedObject();
            foreach (object instruction in selected)
            {
                ((InstructionBlueprint)instruction).Time = time;
            }

        }

        private void UpdateUpDownOnClick(Window callingWindow)
        {
            InstructionBlueprint template = (mListDisplayWindow.GetFirstHighlightedObject() as InstructionBlueprint);
            if(template != null)
                mSetSelectedInstructionTimeUpDown.CurrentValue = (float)template.Time;
        }

        private void UpdateUpDownOnTimeChange(Window callingWindow)
        {
            if (FloatingChildren.Count > 0)
            {
                mSetSelectedInstructionTimeUpDown.CurrentValue = (float)((FloatingChildren[0] as InstructionBlueprintPropertyGrid<Sprite>).SelectedObject as InstructionBlueprint).Time;
            }
        }

        public bool ShowInstructionBlueprintPropertyGridOnStrongSelect
        {
            get { return mListDisplayWindow.ShowPropertyGridOnStrongSelect; }
            set { mListDisplayWindow.ShowPropertyGridOnStrongSelect = value; }
        }


        #endregion


        #region Methods

        public InstructionBlueprintListPropertyGrid(Cursor cursor) :
            base(cursor)
        {
           // ExcludeMember("Name");

            CreateExtraWindows();
            ExcludeAllMembers();

        }

        public override void UpdateDisplayedProperties()
        {
           base.UpdateDisplayedProperties();

           if (mListDisplayWindow != null)
           {
               mListDisplayWindow.UpdateToList();

               if (SelectedObject != null)
               {
                   (SelectedObject as InstructionBlueprintList).InsertionSortAscendingTimeToExecute();
               }
           }

           if (SelectedObject != null && SelectedObject.Count > 0 && InputManager.ReceivingInput != mSetSelectedInstructionTimeUpDown.textBox)
           {
               if (mListDisplayWindow.FloatingChildren.Count == 0 && mListDisplayWindow.GetFirstHighlightedItem() != null)
               {
                   mSetSelectedInstructionTimeUpDown.CurrentValue = (float)(mListDisplayWindow.GetFirstHighlightedObject() as InstructionBlueprint).Time;
               }
               else if (mListDisplayWindow.FloatingChildren.Count == 0)
               {
                   mSetSelectedInstructionTimeUpDown.CurrentValue = (float)(SelectedObject as InstructionBlueprintList)[0].Time;
               }
               else
               {
                   mSetSelectedInstructionTimeUpDown.CurrentValue =
                       (float)((mListDisplayWindow.FloatingChildren[0] as InstructionBlueprintPropertyGrid<Sprite>).SelectedObject as InstructionBlueprint).Time;
               }
           }
        }

        public void CreateExtraWindows()
        {

            #region Create the Up/Down to simultaneously set the time for all selected Instructions

            mSetSelectedInstructionTimeUpDown = new UpDown(GuiManager.Cursor);
            mSetSelectedInstructionTimeUpDown.MinValue = 0;
            mSetSelectedInstructionTimeUpDown.ScaleX = 7;
            mSetSelectedInstructionTimeUpDown.Enabled = false;
            this.AddWindow(mSetSelectedInstructionTimeUpDown);
            this.SetLabelForWindow(mSetSelectedInstructionTimeUpDown, "Time:");

            mSetSelectedInstructionTimeUpDown.ValueChanged += SetSelectedInstructionTime;

            #endregion

            #region Create the ListDisplayWindow to show all instructions
            mListDisplayWindow = new ListDisplayWindow(GuiManager.Cursor);

            mListDisplayWindow.ScaleX = 11;
            mListDisplayWindow.ScaleY = 9;
            mListDisplayWindow.DrawOuterWindow = false;
            mListDisplayWindow.ListBox.Click += UpdateUpDownOnClick;

            this.AddWindow(mListDisplayWindow);
            #endregion

        }

        #endregion

    }
}
