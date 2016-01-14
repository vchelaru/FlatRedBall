using System;
using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Input;


namespace InstructionEditor.Gui
{
	/// <summary>
	/// Summary description for PropertyWindowMessages.
	/// </summary>
	public class PropertyWindowMessages
	{
		#region engine managers and data
		public static Camera camera;
		#endregion

		#region methods

		#region sprite properties GUI messages
		
		public static void SpriteXPosBoxGuiChange(Window callingWindow)
		{
			if(EditorData.EditorLogic.CurrentSprites.Count == 0)	return;

            EditorData.EditorLogic.CurrentSprites[0].X = ((UpDown)callingWindow).CurrentValue;


		}


		public static void SpriteYPosBoxGuiChange(Window callingWindow)
		{
			if(EditorData.EditorLogic.CurrentSprites.Count == 0)	return;
            EditorData.EditorLogic.CurrentSprites[0].Y = ((UpDown)callingWindow).CurrentValue;


		}



		public static void SetBlueprint(Window callingWindow)
		{
			if(EditorData.EditorLogic.CurrentSprites.Count == 0)	return;

			foreach(Sprite s in EditorData.EditorLogic.CurrentSprites)
			{
				string name = EditorData.EditorLogic.CurrentSprites[0].Name;


				EditorData.EditorLogic.CurrentSprites[0].Name = name;
			}

			// calling the SetFromRegSprite is good for setting all variables the same as the blueprint, but the name needs to be saved.
		}

		#endregion


		#endregion
	
	
	}
}
