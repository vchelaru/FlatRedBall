using System;
using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Instructions;


namespace InstructionEditor.Gui
{
	/// <summary>
	/// Summary description for TimeLineMessages.
	/// </summary>
	public class TimeLineMessages
	{
		#region engine managers and data
		public static Camera camera;


		#endregion

		#region timeLine GUI

		public static void CurrentTimeTextBoxChange(Window callingWindow)
		{
			double currentValue = (float)System.Convert.ToDouble(((TextBox)callingWindow).Text);

			if(GuiData.TimeLineWindow.timeLine.TimeUnitDisplayed == TimeLine.TimeUnit.Millisecond)
				currentValue *= 1000.0;

			if(currentValue < GuiData.TimeLineWindow.timeLine.MinimumValue)
			{
				currentValue = GuiData.TimeLineWindow.timeLine.MinimumValue;
				((TextBox)callingWindow).Text = currentValue.ToString();
			}
			else if(currentValue > GuiData.TimeLineWindow.timeLine.MaximumValue)
			{
				currentValue = GuiData.TimeLineWindow.timeLine.MaximumValue;
				((TextBox)callingWindow).Text = currentValue.ToString();
			}


            GuiData.TimeLineWindow.timeLine.CurrentValue = (float)System.Convert.ToDouble(((TextBox)callingWindow).Text);
            EditorData.SetTime(GuiData.TimeLineWindow.timeLine.CurrentValue);


		}

		
		public static void KeyframeMarkerClick(Window callingWindow)
		{
			// TODO : check the color of the marker, and depending on the color, find the appropriate 

			// even better, make markers keep track of an object.  Each marker will reference an object

            EditorData.EditorLogic.CurrentKeyframe = (InstructionList)(((MarkerTimeLine)callingWindow).MarkerClicked.ReferenceObject);

			//GuiData.propertyWindow.UpdateKeyframeOnSelect();


		}


		public static void InsertKeyframeClick(Window callingWindow)
		{
            if (EditorData.EditorLogic.CurrentSprites.Count == 0) return;


		}


		public static void TimeLineGUIValueChange(Window callingWindow)
		{
			switch(GuiData.TimeLineWindow.timeLine.TimeUnitDisplayed)
			{
				case TimeLine.TimeUnit.Millisecond:
                    GuiData.TimeLineWindow.currentTimeTextBox.Text = (((TimeLine)callingWindow).CurrentValue * 1000.0).ToString();

					break;
				case TimeLine.TimeUnit.Second:
                    GuiData.TimeLineWindow.currentTimeTextBox.Text = ((TimeLine)callingWindow).CurrentValue.ToString();
					break;
			}

            EditorData.SetTime(((TimeLine)callingWindow).CurrentValue);

			// do this only if we click on the window, not drag
			if(GuiManager.Cursor.PrimaryClick)
                EditorData.instructionPlayer.ChangeTime();
		}

	
		public static void TimeUnitChange(Window callingWindow)
		{
			if( ((ComboBox)callingWindow).Text == "Seconds")
                GuiData.TimeLineWindow.timeLine.TimeUnitDisplayed = TimeLine.TimeUnit.Second;
			else
                GuiData.TimeLineWindow.timeLine.TimeUnitDisplayed = TimeLine.TimeUnit.Millisecond;

            switch (GuiData.TimeLineWindow.timeLine.TimeUnitDisplayed)
			{
				case TimeLine.TimeUnit.Millisecond:
                    GuiData.TimeLineWindow.currentTimeTextBox.Text = GuiData.TimeLineWindow.timeLine.CurrentValue.ToString();

					break;
				case TimeLine.TimeUnit.Second:
                    GuiData.TimeLineWindow.currentTimeTextBox.Text = (GuiData.TimeLineWindow.timeLine.CurrentValue / 1000.0).ToString();
					break;
			}

		}


		public static void ZoomInClick(Window callingWindow)
		{

			/*
			 * We don't want the timeline to zoom in too much because the inaccuracies of float operations
			 * can create problems when really small.
			 * 
			 */
			GuiData.TimeLineWindow.timeLine.ValueWidth *= .5f;

			GuiData.TimeLineWindow.timeLine.SmallVerticalBarIncrement *= .5f;
			GuiData.TimeLineWindow.timeLine.VerticalBarIncrement *= .5f;

            GuiData.TimeLineWindow.timeLine.Start = GuiData.TimeLineWindow.timeLine.CurrentValue - GuiData.TimeLineWindow.timeLine.ValueWidth / 2.0f;

			GuiData.TimeLineWindow.timeLine.AutoCalculateVerticalLineSpacing();

            const float minimumWidth = 2.5f;

            if (GuiData.TimeLineWindow.timeLine.ValueWidth < minimumWidth)
			{
				GuiData.TimeLineWindow.zoomInTimeLineButton.Enabled = false;
			}
		}


		public static void ZoomOutClick(Window callingWindow)
		{
			GuiData.TimeLineWindow.timeLine.ValueWidth *= 2;
			GuiData.TimeLineWindow.timeLine.SmallVerticalBarIncrement *= 2;
			GuiData.TimeLineWindow.timeLine.VerticalBarIncrement *= 2;

            GuiData.TimeLineWindow.timeLine.Start = GuiData.TimeLineWindow.timeLine.CurrentValue - GuiData.TimeLineWindow.timeLine.ValueWidth / 2.0f;

			GuiData.TimeLineWindow.timeLine.AutoCalculateVerticalLineSpacing();

            GuiData.TimeLineWindow.zoomInTimeLineButton.Enabled = true;


		}
	
	
		#endregion
	}
}
