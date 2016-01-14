using System;
using FlatRedBall.Instructions;
using FlatRedBall.MSG;

namespace SpriteEditor.seInstructions
{
	/// <summary>
	/// Summary description for SpriteGridRotX.
	/// </summary>
	public class SpriteGridRotX : FrbInstruction
	{
		float RotationX;
		public SpriteGridRotX(SpriteGrid referenceGrid, float rotx, long TimeToExecute)
		{
			this.referenceObject = referenceGrid;			this.timeToExecute = TimeToExecute;
			RotationX = rotx;
		}
		public override void Execute()
		{	
			((SpriteGrid)referenceObject).blueprint.RotationX = RotationX;
			((SpriteGrid)referenceObject).UpdateRotAndScl();
		}

	}
}
