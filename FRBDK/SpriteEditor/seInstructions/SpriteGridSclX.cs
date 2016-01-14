using System;
using FlatRedBall.Instructions;
using FlatRedBall.MSG;

namespace SpriteEditor.seInstructions
{
	/// <summary>
	/// Summary description for SpriteGridScaleX.
	/// </summary>
	public class SpriteGridScaleX : FrbInstruction
	{
		float ScaleX;
		public SpriteGridScaleX(SpriteGrid referenceGrid, float ScaleX, long TimeToExecute)
		{
			this.referenceObject = referenceGrid;			this.timeToExecute = TimeToExecute;
			ScaleX = ScaleX;
		}
		public override void Execute()
		{	
			((SpriteGrid)referenceObject).blueprint.ScaleX = ScaleX;
			((SpriteGrid)referenceObject).UpdateRotAndScl();
		}

	}
}
