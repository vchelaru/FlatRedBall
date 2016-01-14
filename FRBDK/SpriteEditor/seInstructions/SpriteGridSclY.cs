using System;
using FlatRedBall.Instructions;
using FlatRedBall.MSG;

namespace SpriteEditor.seInstructions
{
	/// <summary>
	/// Summary description for SpriteGridScaleY.
	/// </summary>
	public class SpriteGridScaleY : FrbInstruction
	{
		float ScaleY;
		public SpriteGridScaleY(SpriteGrid referenceGrid, float ScaleY, long TimeToExecute)
		{
			this.referenceObject = referenceGrid;			this.timeToExecute = TimeToExecute;
			ScaleY = ScaleY;
		}
		public override void Execute()
		{	
			((SpriteGrid)referenceObject).blueprint.ScaleY = ScaleY;
			((SpriteGrid)referenceObject).UpdateRotAndScl();
		}

	}
}
