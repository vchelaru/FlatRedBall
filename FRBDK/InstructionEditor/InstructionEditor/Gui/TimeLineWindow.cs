using System;
using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Input;
using FlatRedBall.Instructions;

namespace InstructionEditor.Gui
{
    #region InstructionMode Enum
    public enum InstructionMode
    {
        All,
        Current
    }
    #endregion

    /// <summary>
	/// Summary description for TimeLineWindow.
	/// </summary>
	public class TimeLineWindow : CollapseWindow
    {
        #region Fields

        public TextBox currentTimeTextBox;
		public MarkerTimeLine timeLine;
		public Button insertKeyframeButton;

		public Button zoomInTimeLineButton;
		public Button zoomOutTimeLineButton;

		public ComboBox timeUnit;

        private bool mIsPlaying = false;

        private ComboBox mAllOrCurrent;

        #endregion

        #region Properties

        public double CurrentValue
        {
            get 
            { 
                return timeLine.CurrentValue; 
            }
            set 
            { 
                timeLine.CurrentValue = value;
                currentTimeTextBox.Text = value.ToString();
            }
        }

        public InstructionMode InstructionMode
        {
            set
            {
                mAllOrCurrent.SelectedObject = value;
                mAllOrCurrent.Text = value.ToString();
            }

            get
            {
                return (InstructionMode)(mAllOrCurrent.SelectedObject);
            }
        }

        public bool IsPlaying
        {
            get 
            {
                return mIsPlaying;
            }
            set
            {
                mIsPlaying = value;
            }
        }

        #endregion

        #region Event Methods
        private void AllOrCurrentItemClick(Window callingWindow)
        {
            // IMPLEMENT HERE
        }
        #endregion

        #region Methods

        public TimeLineWindow(Cursor cursor) : 
            base(cursor)
		{
			GuiManager.AddWindow(this);
			SetPositionTL(55.2f, 80.1f);
			ScaleX = 55.2f;
			ScaleY = 3.3f;
			mName = "Time Line";

            #region Zoom in button

            zoomInTimeLineButton = AddButton();
			zoomInTimeLineButton.SetPositionTL(104.7474f, 4.5f);
			zoomInTimeLineButton.ScaleX = 1.1f;
			zoomInTimeLineButton.ScaleY = 1.1f;
			zoomInTimeLineButton.Text = "Zoom in time line";
			zoomInTimeLineButton.SetOverlayTextures(3, 2);
			zoomInTimeLineButton.Click += new GuiMessage(TimeLineMessages.ZoomInClick);

            #endregion

            #region Zoom out button

            zoomOutTimeLineButton = AddButton();
			zoomOutTimeLineButton.SetPositionTL(102.2f, 4.5f);
			zoomOutTimeLineButton.ScaleX = 1.1f;
			zoomOutTimeLineButton.ScaleY = 1.1f;
			zoomOutTimeLineButton.Text = "Zoom out time line";
			zoomOutTimeLineButton.SetOverlayTextures(2, 2);
			zoomOutTimeLineButton.Click += new GuiMessage(TimeLineMessages.ZoomOutClick);

            #endregion

            #region Current time TextBox
            currentTimeTextBox = AddTextBox();
			currentTimeTextBox.SetPositionTL(92, 4.3f);
			currentTimeTextBox.ScaleX = 6;
			currentTimeTextBox.ScaleY = 1.1f;
			currentTimeTextBox.Format = TextBox.FormatTypes.Decimal;
			currentTimeTextBox.LosingFocus += new GuiMessage(TimeLineMessages.CurrentTimeTextBoxChange);
			currentTimeTextBox.Text = "0";
            #endregion

            #region Insert Keyframe Button
            insertKeyframeButton = AddButton();
			insertKeyframeButton.SetPositionTL(99.5f, 4.6f);
			insertKeyframeButton.ScaleX = 1.3f;
			insertKeyframeButton.ScaleY = 1.3f;
			insertKeyframeButton.Text = "Insert Keyframe";
			insertKeyframeButton.SetOverlayTextures(4, 2);
			insertKeyframeButton.Click += new GuiMessage(TimeLineMessages.InsertKeyframeClick);
            insertKeyframeButton.Visible = false; // need to remove this.
            #endregion

            #region Time Line
            timeLine = new MarkerTimeLine(cursor);
			AddWindow(timeLine);
			timeLine.ScaleX = 42.0f;
			timeLine.ScaleY = 2;
			timeLine.SetPositionTL(43.0f, this.ScaleY * 2 - 2.4f);
            timeLine.GuiChange += new GuiMessage(TimeLineMessages.TimeLineGUIValueChange);
			timeLine.MarkerClick += new GuiMessage(TimeLineMessages.KeyframeMarkerClick);

			timeLine.Start = 0;
            timeLine.TimeUnitDisplayed = TimeLine.TimeUnit.Second;
			timeLine.MinimumValue = 0;
			timeLine.MaximumValue = 999;
			timeLine.ValueWidth = 20;
			timeLine.AutoCalculateVerticalLineSpacing();
            #endregion

            timeUnit = this.AddComboBox();
			timeUnit.ScaleX = 6f;
			timeUnit.AddItem("Milliseconds");
			timeUnit.AddItem("Seconds");
			timeUnit.Text = "Seconds";
			timeUnit.SetPositionTL(92f, 1.8f);
			timeUnit.ItemClick += new GuiMessage(TimeLineMessages.TimeUnitChange);

            #region All or Current Combo Box

            mAllOrCurrent = this.AddComboBox();
            mAllOrCurrent.ScaleX = 5;
            mAllOrCurrent.SetPositionTL(104, 1.8f);
            mAllOrCurrent.AddItem("All", InstructionMode.All);
            mAllOrCurrent.AddItem("Current", InstructionMode.Current);
            mAllOrCurrent.Text = "All";
            mAllOrCurrent.SelectedObject = InstructionMode.All;
            mAllOrCurrent.ItemClick += AllOrCurrentItemClick;

            #endregion
        }

        public void Update()
        {
            if (IsPlaying)
            {
                CurrentValue += TimeManager.SecondDifference;
            }

            UpdateToCurrentSet();
        }

        public void UpdateToCurrentSet()
        {
            timeLine.ClearMarkers();

            if (InstructionMode == InstructionMode.Current)
            {
                if (EditorData.EditorLogic.CurrentKeyframeList != null)
                {
                    foreach (InstructionList instructionList in EditorData.EditorLogic.CurrentKeyframeList)
                    {
                        if (instructionList.Count != 0)
                        {
                            timeLine.AddMarker(instructionList[0].TimeToExecute, instructionList);
                        }
                    }
                }
            }
            else
            {
                if (EditorData.EditorLogic.CurrentAnimationSequence != null)
                {
                    foreach (TimedKeyframeList tkl in EditorData.EditorLogic.CurrentAnimationSequence)
                    {
                        timeLine.AddMarker(tkl.TimeToExecute, tkl);
                    }
                }

            }
        }

        #endregion

    }
}
