using System;
using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Instructions;

using FlatRedBall.Utilities;

using System.Collections.Generic;

namespace InstructionEditor.Gui
{
	/// <summary>
	/// Summary description for ToolsMessages.
	/// </summary>
	public class ToolsMessages
	{
		#region engine managers and data

		public static Camera camera;

		#endregion

		public static void CopyButtonClick(Window callingWindow)
		{
            if (EditorData.EditorLogic.CurrentSprites.Count == 0) return;

			#region explanation of how new group numbers are determined and assigned
			/*
			 * If a group is copied, then we want the new group to be separate from the old group.
			 * That means that if the old group is group 0, then the new group should be group 1.
			 * Also, if the currentSprites include Sprites from multiple groups, then we want to preserve groups.
			 * We don't know the order of the groups, so what we need to do is keep track of the old groups and new groups
			 * in an IntArray.  Then for every Sprite we copy, we first survey to see if its group is contained in the
			 * oldGroups IntArray.  If so, then we take the index of the group in the oldGroup and assign the same index
			 * of newGroup to the new Sprite.  If the oldGroup does not contain the sprite to copy's group, then we add
			 * an entry into both oldGroup and the new group; the old group gets the sprite to copy's group, and the new
			 * group gets the EditorData.groupNumber value.  Any time the groupNumber is added to the newGroup IntArray, the 
			 * EditorData.groupNumber needs to be incremented so that we have a fresh group number.
			 */
			#endregion

			List<int> oldGroups = new List<int>();
			List<int> newGroups = new List<int>();

            foreach (Sprite s in EditorData.EditorLogic.CurrentSprites)
			{
                //IESprite tempSprite = ((IESprite)s).Clone();
                //tempSprite.movementPath.sprite = tempSprite;

                //while( EditorData.ActiveSprites.FindByName(tempSprite.Name) != null)
                //{
                //    tempSprite.Name = FlatRedBall.Utilities.StringFunctions.IncrementNumberAtEnd(tempSprite.Name);
                //}

                //EditorData.AddSprite(tempSprite);

                //#region adjust the group number
                //if(tempSprite.groupNumber != -1)
                //{
                //    if(oldGroups.Contains(tempSprite.groupNumber))
                //    {
                //        tempSprite.groupNumber = newGroups[oldGroups.IndexOf(tempSprite.groupNumber)];
                //    }
                //    else
                //    {
                //        oldGroups.Add(tempSprite.groupNumber);
                //        tempSprite.groupNumber = EditorData.groupNumber;
                //        newGroups.Add(EditorData.groupNumber);
                //        EditorData.groupNumber++;
						
                //    }
                //}
                //#endregion
			}

		}


		public static void ShiftKeyframeTimesButtonClick(Window callingWindow)
		{
            if (EditorData.EditorLogic.CurrentSprites.Count == 0) return;

            TextInputWindow tiw = GuiManager.ShowTextInputWindow("Shift how many milliseconds?", EditorData.EditorLogic.CurrentSprites[0].Name);
			tiw.OkClick += new GuiMessage(ShiftKeyFrameTextInputOk);
			tiw.format = TextBox.FormatTypes.Decimal;

		}


		public static void ShiftKeyFrameTextInputOk(Window callingWindow)
		{
//            if (((IESprite)EditorData.EditorLogic.CurrentSprites[0]).positionInstructions[0].TimeToExecute +
//                System.Convert.ToDouble(((TextInputWindow)callingWindow).Text) < 0)
//            {
//                MultiButtonMessageBox mbmb = GuiManager.AddMultiButtonMessageBox();
//                mbmb.Text = "Shifting all keyframes by " + ((TextInputWindow)callingWindow).Text +
//                    " milliseconds moves the first keyframe before the scene starts.  What would you like to do?";
//                mbmb.AddButton("Cancel shift", new GuiMessage(CancelShift));
//                mbmb.AddButton("Manually change shift time", new GuiMessage(ShiftKeyframeTimesButtonClick));
//                mbmb.AddButton("Automatically change shift so keyframes occur in set.", new GuiMessage(AutomaticChangeShift));
//                mbmb.AddButton("Shift the set's start time to hold all keyframes.", null);
//            }
//            else
//            {
//                IESprite ies = null;
//                GuiData.TimeLineWindow.timeLine.ClearMarkers();

//                foreach(Sprite s in EditorData.EditorLogic.CurrentSprites)
//                {
//                    ies = s as IESprite;

//                    foreach(PositionInstruction instruction in ies.positionInstructions)
//                    {
//                        instruction.TimeToExecute += System.Convert.ToInt64(((TextInputWindow)callingWindow).Text);
//                        if(s == EditorData.EditorLogic.CurrentSprites[0] && !instruction.IsMidpoint())
//                        {
//                            GuiData.TimeLineWindow.timeLine.AddMarker( (double)instruction.TimeToExecute, instruction);
//                        }
//                    }
//                }
////				((IESprite)EditorData.EditorLogic.CurrentSprites[0]).movementPath.startTime = ((IESprite)EditorData.EditorLogic.CurrentSprites[0]).positionInstructions[0].TimeToExecute;
//            }
		}


		private static void AutomaticChangeShift(Window callingWindow)
		{
            //double time = -((IESprite)EditorData.EditorLogic.CurrentSprites[0]).positionInstructions[0].TimeToExecute;
            //GuiData.TimeLineWindow.timeLine.ClearMarkers();
            //foreach(PositionInstruction instruction in ((IESprite)EditorData.EditorLogic.CurrentSprites[0]).positionInstructions)
            //{
            //    instruction.TimeToExecute += time;
            //    if(!instruction.IsMidpoint())
            //    {
            //        GuiData.TimeLineWindow.timeLine.AddMarker( (double)instruction.TimeToExecute, instruction);
            //    }			
            //}

//			((IESprite)EditorData.EditorLogic.CurrentSprites[0]).movementPath.startTime += System.Convert.ToInt64(((TextInputWindow)callingWindow).text);
//			((IESprite)EditorData.EditorLogic.CurrentSprites[0]).movementPath.startTime = ((IESprite)EditorData.EditorLogic.CurrentSprites[0]).positionInstructions[0].TimeToExecute;
		}


		private static void CancelShift(Window callingWindow)
		{
			callingWindow.Parent.CloseWindow();
		}

	}
}
