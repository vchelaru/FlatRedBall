using System;
using FlatRedBall;
using FlatRedBall.Gui;
using InstructionEditor.Gui;

namespace InstructionEditor
{
	/// <summary>
	/// Summary description for InstructionPlayer.
	/// </summary>
	public class InstructionPlayer
	{
		#region members

		public string currentState = "";

		long activityStartTime;

        double mTimeManagerStartTime;

		#endregion

		#region methods
				
		public void Activity()
		{
            //if(currentState == "playing")
            //{
            //    // This was here before the unification - not sure why.
            //    //SpriteManager.ExecuteInstructions();

            //    float secondsPassed = (float)(TimeManager.CurrentTime - activityStartTime);
            //    GuiData.TimeLineWindow.timeLine.CurrentValue = activityStartTime + secondsPassed*1000;

            //    switch (GuiData.TimeLineWindow.timeLine.TimeUnitDisplayed)
            //    {
            //        case TimeLine.TimeUnit.Millisecond:
            //            GuiData.TimeLineWindow.currentTimeTextBox.Text = GuiData.TimeLineWindow.timeLine.CurrentValue.ToString();

            //            break;
            //        case TimeLine.TimeUnit.Second:
            //            GuiData.TimeLineWindow.currentTimeTextBox.Text = (GuiData.TimeLineWindow.timeLine.CurrentValue / 1000.0).ToString();
            //            break;
            //    }



            //    if (GuiData.TimeLineWindow.timeLine.CurrentValue > GuiData.TimeLineWindow.timeLine.Start + GuiData.TimeLineWindow.timeLine.ValueWidth * .85f)
            //        GuiData.TimeLineWindow.timeLine.Start = GuiData.TimeLineWindow.timeLine.CurrentValue - GuiData.TimeLineWindow.timeLine.ValueWidth * .15f;
            //}

		}


		public void ChangeTime()
		{
			if(currentState == "playing")
			{
				Stop();
				Play();
			}
		}

		
		public void Play()
		{
            activityStartTime = ((long)GuiData.TimeLineWindow.timeLine.CurrentValue);
            mTimeManagerStartTime = TimeManager.CurrentTime;

			currentState = "playing";
			
            //foreach(IESprite sprite in EditorData.ActiveSprites)
            //{
            //    sprite.StartPlaying((long)(GuiData.TimeLineWindow.timeLine.CurrentValue), EditorData.EditorLogic.CurrentSprites.Contains(sprite));
            //}

//			EditorData.inSetSpriteManager.Camera.StartPlaying( (long)(GuiData.timeLine.currentValue), sprMan);

		}


		public void Stop()
		{
			currentState = "";

            //foreach(IESprite sprite in EditorData.ActiveSprites)
            //{
            //    sprite.StopPlaying();
            //}

            //throw new NotImplementedException();
			///sprMan.ClearSpriteAdditionInstructions();
		}

		
		#endregion

	}
}
